using System;
using System.IO;
using System.Reflection;
using UnityEditor.EditorIterationProfiler.Formatting;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using ExportStatus = UnityEditor.EditorIterationProfiler.EditorIterationProfilerAnalytics.ExportStatus;
using ExportType = UnityEditor.EditorIterationProfiler.EditorIterationProfilerAnalytics.ExportType;

namespace UnityEditor.EditorIterationProfiler
{

    public class EditorIterationProfilerWindow : EditorWindow
    {
        internal static class Styles
        {
            public static readonly GUIContent k_EnableProfiler = EditorGUIUtility.TrTextContent("Enable");
            public static readonly GUIContent k_ClearProfilerData = EditorGUIUtility.TrTextContent("Clear");
            public static readonly GUIContent k_DeepProfile = EditorGUIUtility.TrTextContent("Deep Profile", "Show all managed calls");
            public static readonly GUIContent k_UserCode = EditorGUIUtility.TrTextContent("User Code", "Only show user code");
            public static readonly GUIContent k_Flatten = EditorGUIUtility.TrTextContent("Flatten", "Flatten markers");
            public static readonly GUIContent k_CollapseAll = EditorGUIUtility.TrTextContent("Collapse All", "Collapse all foldouts");
            public static readonly GUIContent k_LogConsole = EditorGUIUtility.TrTextContent("Print to Console", "Prints plaintext into the log and console");
            public static readonly GUIContent k_Export = EditorGUIUtility.TrTextContent("Export...", "Exports the data captured by the Editor Iteration Profiler");
            public static readonly GUIContent k_ExportProfiler = EditorGUIUtility.TrTextContent("Export Profiler Data...", "Exports the selected frame or the last frame in the Profiler");

            public const string k_EditorIterationProfilerPrefix = "EIP_";
            public const string k_EnableProfilerPref = k_EditorIterationProfilerPrefix + "ProfilingEnabled";
            public const string k_EnableDeepProfile = k_EditorIterationProfilerPrefix + "DeepProfilingEnabled";
            public const string k_UserCodePref = k_EditorIterationProfilerPrefix + "UserCodeOnly";
            public const string k_FlattenPref = k_EditorIterationProfilerPrefix + "Flatten";

            public const string k_EventHeaderWidthSizePref = k_EditorIterationProfilerPrefix + "EventHeaderWidthSizePref";
            public const string k_DetailsHeaderWidthSizePref = k_EditorIterationProfilerPrefix + "DetailsWidthSizePref";
            public const string k_DurationHeaderWidthSizePref = k_EditorIterationProfilerPrefix + "HeaderWidthSizePref";

            public const float k_SingleLineHeight =
#if UNITY_2019_3_OR_NEWER
                18;
#else
                16;
#endif
        }

        [NonSerialized]
        bool m_Initialized;
        [SerializeField]
        MultiColumnHeaderState m_MultiColumnHeaderState;
        [SerializeField]
        TreeViewState m_TreeViewState;
        EditorIterationProfilerTreeView m_TreeView;
        SearchField m_SearchField;

        [MenuItem("Window/Analysis/Editor Iteration Profiler/Show Window", priority = 8)]
        static void ShowProfilerWindow()
        {
            var window = GetWindow<EditorIterationProfilerWindow>("Editor Iteration Profiler");
            window.minSize = new Vector2(500, 300);
        }

        void OnEnable()
        {
            if (!m_Initialized)
            {
                InitializeButtonStateFromPrefs();

                if (m_TreeViewState == null)
                {
                    m_TreeViewState = new TreeViewState();
                }

                bool firstInit = m_MultiColumnHeaderState == null;
                var headerState = EditorIterationProfilerTreeView.CreateDefaultMultiColumnHeaderState();

                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                }

                m_MultiColumnHeaderState = headerState;
                InitializeHeaderStateFromPrefs(headerState);

                var multiColumnHeader = new MultiColumnHeader(headerState);
                if (firstInit)
                {
                    multiColumnHeader.ResizeToFit();
                }

                m_TreeView = new EditorIterationProfilerTreeView(m_TreeViewState, multiColumnHeader);

                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                m_Initialized = true;
            }
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            UnityProfiling.EditorProfilingEnabled = GUILayout.Toggle(UnityProfiling.EditorProfilingEnabled, Styles.k_EnableProfiler, EditorStyles.toolbarButton);

            UnityProfiling.SetProfileDeepScripts(GUILayout.Toggle(ProfilerDriver.deepProfiling, Styles.k_DeepProfile, EditorStyles.toolbarButton));

            var flatten = GUILayout.Toggle(m_TreeView.Flatten, Styles.k_Flatten, EditorStyles.toolbarButton);

            var userCodeOnly = GUILayout.Toggle(m_TreeView.UserCodeOnly, Styles.k_UserCode, EditorStyles.toolbarButton);

            GUILayout.Space(5);

            if (GUILayout.Button(Styles.k_ClearProfilerData, EditorStyles.toolbarButton))
            {
                EditorIterationProfilerIntegration.Clear();
            }

            if (GUILayout.Button(Styles.k_CollapseAll, EditorStyles.toolbarButton))
            {
                m_TreeView.CollapseAll();
            }

            if (GUILayout.Button(Styles.k_LogConsole, EditorStyles.toolbarButton))
            {
                var reporter = EditorIterationProfilerIntegration.Instance.DataReporterProvider.TryGetDataReporter("EditorLogReporter");
                reporter.Report(EditorIterationProfilerIntegration.Instance.IterationList);
            }

            if (EditorGUILayout.DropdownButton(Styles.k_Export, FocusType.Passive, EditorStyles.toolbarPopup))
            {
                var menu = new GenericMenu();

                var exporters = EditorIterationProfilerIntegration.Instance.DataReporterProvider.GetAllReporters<IFileDataReporter>();

                foreach (var exporter in exporters)
                {
                    menu.AddItem(new GUIContent(exporter.Name), false, () => ReportExtension(exporter, EditorIterationProfilerIntegration.Instance.IterationList, ExportType.Captured));
                }

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Export all available formats"), false, () => ReportAllExtensions(EditorIterationProfilerIntegration.Instance.IterationList));

                var dropDownRect = EditorGUILayout.GetControlRect();
                dropDownRect.x += 500;
                dropDownRect.y += 18;

                menu.DropDown(dropDownRect);
            }

            if (EditorGUILayout.DropdownButton(Styles.k_ExportProfiler, FocusType.Passive, EditorStyles.toolbarPopup))
            {
                var menu = new GenericMenu();

                var exporters = EditorIterationProfilerIntegration.Instance.DataReporterProvider.GetAllReporters<IFileDataReporter>();

                var assembly = typeof(Editor).Assembly;
                var windowType = assembly.GetType("UnityEditor.ProfilerWindow");
                var profilerWindow = GetWindow(windowType);
                var currentFrameInfo = windowType.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(currentFrameInfo);

                int currentFrameIndex = (int)currentFrameInfo.GetValue(profilerWindow);

                foreach (var exporter in exporters)
                {
                    menu.AddItem(new GUIContent("Selected Frame/" + exporter.Name), false, () => ReportSelectedFrame(exporter, currentFrameIndex));
                }

                menu.AddSeparator("");

                foreach (var exporter in exporters)
                {
                    menu.AddItem(new GUIContent("Multiple Frames/" + exporter.Name), false, () =>
                    {
                        var window = GetWindow<ProfilerMultiFrameSelector>();
                        window.Initialize(exporter);
                    });
                }

                var dropDownRect = EditorGUILayout.GetControlRect();
                dropDownRect.x += 600;
                dropDownRect.y += 18;

                menu.DropDown(dropDownRect);
            }

            GUILayout.Space(5);

            GUILayout.FlexibleSpace();

            if (m_TreeView.UserCodeOnly != userCodeOnly)
            {
                EditorIterationProfilerAnalytics.SendInteractionEvent(UnityProfiling.EditorProfilingEnabled, EditorApplication.isPlaying, ProfilerDriver.deepProfiling, EditorIterationProfilerIntegration.Instance.Settings.Flatten, EditorIterationProfilerIntegration.Instance.Settings.UserCode);

                m_TreeView.UserCodeOnly = userCodeOnly;
                EditorPrefs.SetBool(Styles.k_UserCodePref, m_TreeView.UserCodeOnly);
                m_TreeView.Reload();
            }

            if (m_TreeView.Flatten != flatten)
            {
                EditorIterationProfilerAnalytics.SendInteractionEvent(UnityProfiling.EditorProfilingEnabled, EditorApplication.isPlaying, ProfilerDriver.deepProfiling, EditorIterationProfilerIntegration.Instance.Settings.Flatten, EditorIterationProfilerIntegration.Instance.Settings.UserCode);

                m_TreeView.Flatten = flatten;
                EditorPrefs.SetBool(Styles.k_FlattenPref, m_TreeView.Flatten);
                m_TreeView.Reload();
            }

            DrawSearchBar();
            GUILayout.EndHorizontal();
        }

        public static void ReportMultipleFrames(IFileDataReporter reporter, int beginRange, int endRange, string guid)
        {
            var exportType = ExportType.Multi;
            if (beginRange == -1)
            {
                ReportSelectedFrame(reporter, beginRange, exportType, guid);
                return;
            }

            EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Started.ToString(), guid);

            var path = EditorUtility.SaveFolderPanel($"Export frames to folder", "", reporter.Extension);

            for (int i = beginRange - 1; i < endRange; ++i)
            {
                var list = new IterationList();

                var hfdv = UnityProfiling.GetFrame(i, 0);
                if (!hfdv.valid)
                {
                    hfdv = UnityProfiling.GetFrame(-1, 0);

                    if (!hfdv.valid)
                    {
                        EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Error.ToString(), guid);
                        Debug.LogError($"There was an issue getting the frame {i}");
                        return;
                    }
                }

                list.NewIteration(IterationEventKind.None);

                Assert.IsNotNull(list.LastIterationEventRoot);
                list.LastIterationEventRoot.StartEvent(IterationEventKind.None);

                ProfilerDataCollector.ReadProfilingData(list.LastIterationEventRoot, list.LastIterationEventRoot.FindLastEvent(IterationEventKind.None), hfdv, hfdv.GetRootItemID(), EventDataFlags.None, false);

                var lastEvent = list.LastIterationEventRoot.FindLastEvent(IterationEventKind.None);

                Assert.IsNotNull(lastEvent);

                lastEvent.SetStartFinishTimeFromChildren();
                lastEvent.Identifier = $"Data (Frame {i + 1})";

                var filename = "EditorIterationData_" + $"Frame{i + 1}_" + $"{DateTime.Now.ToString("MMddyyyy_HHmmss")}" + $".{reporter.Extension}";

                if (path.Length != 0)
                {
                    reporter.Report(list, Path.Combine(path, filename));
                }
            }

            if (path.Length != 0)
            {
                EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Finished.ToString(), guid);
            }
        }

        static void ReportSelectedFrame(IFileDataReporter reporter, int currentFrameIndex, ExportType exportType = ExportType.Selected, string guid = "")
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
                EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Started.ToString(), guid);
            }

            var list = new IterationList();

            var hfdv = UnityProfiling.GetFrame(currentFrameIndex, 0);
            if (!hfdv.valid)
            {
                hfdv = UnityProfiling.GetFrame(-1, 0);

                if (!hfdv.valid)
                {
                    EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Error.ToString(), guid);
                    Debug.LogError($"There was an issue getting the frame {currentFrameIndex}");
                    return;
                }
            }

            list.NewIteration(IterationEventKind.None);

            Assert.IsNotNull(list.LastIterationEventRoot);
            list.LastIterationEventRoot.StartEvent(IterationEventKind.None);

            ProfilerDataCollector.ReadProfilingData(list.LastIterationEventRoot, list.LastIterationEventRoot.FindLastEvent(IterationEventKind.None), hfdv, hfdv.GetRootItemID(), EventDataFlags.None, false);

            var lastEvent = list.LastIterationEventRoot.FindLastEvent(IterationEventKind.None);

            Assert.IsNotNull(lastEvent);

            lastEvent.SetStartFinishTimeFromChildren();
            lastEvent.Identifier = $"Data (Frame {currentFrameIndex + 1})";

            ReportExtension(reporter, list, exportType, guid);
        }

        static void ReportAllExtensions(IIterationList iterationList)
        {
            var guid = Guid.NewGuid().ToString();

            var path = EditorUtility.SaveFolderPanel($"Export frames to folder", "", "");
            var extensions = EditorIterationProfilerIntegration.Instance.DataReporterProvider.GetAllReporters<IFileDataReporter>();

            foreach (var extension in extensions)
            {
                ReportExtension(extension, iterationList, ExportType.CapturedAll, guid, path);
            }
        }

        static void ReportExtension(IFileDataReporter fileReporterType, IIterationList iterationList, ExportType exportType, string guid = "", string folderPath = "")
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            if(ExportType.Selected != exportType)
            {
                EditorIterationProfilerAnalytics.SendExportEvent(fileReporterType.Name, exportType.ToString(), ExportStatus.Started.ToString(), guid);
            }

            var reporter = EditorIterationProfilerIntegration.Instance.DataReporterProvider.TryGetReporter<IFileDataReporter>(fileReporterType.GetType());
            if (reporter == null)
            {
                return;
            }

            var filename = "EditorIterationData_" + $"{reporter.Name.Replace(" ", "")}_" + $"{DateTime.Now.ToString("MMddyyyy_HHmmss")}" + $".{reporter.Extension}";

            string path;
            if (string.IsNullOrEmpty(folderPath))
            {
                path = EditorUtility.SaveFilePanel($"Export {reporter.Extension}", "", filename, reporter.Extension);
            }
            else
            {
                path = Path.Combine(folderPath, filename);
            }

            if (path.Length != 0)
            {
                reporter.Report(iterationList, path);

                EditorIterationProfilerAnalytics.SendExportEvent(reporter.Name, exportType.ToString(), ExportStatus.Finished.ToString(), guid);
            }
        }

        void DrawSearchBar()
        {
            var rect = GUILayoutUtility.GetRect(50f, 300f, Styles.k_SingleLineHeight, Styles.k_SingleLineHeight, EditorStyles.toolbarSearchField);

            var searchLength = m_TreeView.searchString?.Length ?? 0;
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(rect, m_TreeView.searchString);
            if (searchLength > 0 && !m_TreeView.hasSearch)
            {
                m_TreeView.CollapseAll();
                try
                {
                    m_TreeView.FrameItem(m_TreeView.state.lastClickedID);
                }
                catch (ArgumentException)
                {
                    m_TreeView.state.lastClickedID = -1;
                }
            }
        }

        void DrawTreeView()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            m_TreeView.OnGUI(rect);
        }

        void OnLostFocus()
        {
            EditorIterationProfilerAnalytics.SendInteractionEvent(UnityProfiling.EditorProfilingEnabled, EditorApplication.isPlaying, ProfilerDriver.deepProfiling, EditorIterationProfilerIntegration.Instance.Settings.Flatten, EditorIterationProfilerIntegration.Instance.Settings.UserCode);
        }

        void OnGUI()
        {
            DrawToolbar();

            DrawTreeView();

            UpdatePrefs();
        }

        static void InitializeButtonStateFromPrefs()
        {
            if (!EditorPrefs.HasKey(Styles.k_EnableProfilerPref))
            {
                EditorPrefs.SetBool(Styles.k_EnableProfilerPref, false);
            }

            if (!EditorPrefs.HasKey(Styles.k_UserCodePref))
            {
                EditorPrefs.SetBool(Styles.k_UserCodePref, false);
            }

            if (!EditorPrefs.HasKey(Styles.k_FlattenPref))
            {
                EditorPrefs.SetBool(Styles.k_FlattenPref, false);
            }

            if (!EditorPrefs.HasKey(Styles.k_EnableDeepProfile))
            {
                EditorPrefs.SetBool(Styles.k_EnableDeepProfile, false);
            }

            UnityProfiling.EditorProfilingEnabled = EditorPrefs.GetBool(Styles.k_EnableProfilerPref);
            UnityProfiling.SetProfileDeepScripts(EditorPrefs.GetBool(Styles.k_EnableDeepProfile));
        }

        static void InitializeHeaderStateFromPrefs(MultiColumnHeaderState state)
        {
            int columnIndex = 0;
            if (!EditorPrefs.HasKey(Styles.k_EventHeaderWidthSizePref))
            {
                EditorPrefs.SetFloat(Styles.k_EventHeaderWidthSizePref, state.columns[columnIndex++].minWidth);
            }

            if (!EditorPrefs.HasKey(Styles.k_DetailsHeaderWidthSizePref))
            {
                EditorPrefs.SetFloat(Styles.k_DetailsHeaderWidthSizePref, state.columns[columnIndex++].minWidth);
            }

            if (!EditorPrefs.HasKey(Styles.k_DurationHeaderWidthSizePref))
            {
                EditorPrefs.SetFloat(Styles.k_DurationHeaderWidthSizePref, state.columns[columnIndex].minWidth);
            }

            columnIndex = 0;
            state.columns[columnIndex++].width = EditorPrefs.GetFloat(Styles.k_EventHeaderWidthSizePref);
            state.columns[columnIndex++].width = EditorPrefs.GetFloat(Styles.k_DetailsHeaderWidthSizePref);
            state.columns[columnIndex].width = EditorPrefs.GetFloat(Styles.k_DurationHeaderWidthSizePref);
        }

        void UpdatePrefs()
        {
            EditorPrefs.SetFloat(Styles.k_EventHeaderWidthSizePref, m_MultiColumnHeaderState.columns[0].width);
            EditorPrefs.SetFloat(Styles.k_DetailsHeaderWidthSizePref, m_MultiColumnHeaderState.columns[1].width);
            EditorPrefs.SetFloat(Styles.k_DurationHeaderWidthSizePref, m_MultiColumnHeaderState.columns[2].width);
            EditorPrefs.SetBool(Styles.k_EnableProfilerPref, UnityProfiling.EditorProfilingEnabled);
        }

        [MenuItem("Window/Analysis/Editor Iteration Profiler/Clear Editor Preferences", priority = 20)]
        static void DeleteAllEIPPrefKeys()
        {
            if (EditorUtility.DisplayDialog("Delete EIP Editor Preferences", "Delete EIP Editor Preferences?", "Yes", "No"))
            {
                EditorPrefs.DeleteKey(Styles.k_DurationHeaderWidthSizePref);
                EditorPrefs.DeleteKey(Styles.k_DetailsHeaderWidthSizePref);
                EditorPrefs.DeleteKey(Styles.k_EventHeaderWidthSizePref);
                EditorPrefs.DeleteKey(Styles.k_UserCodePref);
                EditorPrefs.DeleteKey(Styles.k_EnableDeepProfile);
                EditorPrefs.DeleteKey(Styles.k_EnableProfilerPref);

                Debug.Log("EIP Editor Preferences deleted");
            }
        }
    }

    class ProfilerMultiFrameSelector : EditorWindow
    {
        public static readonly GUIContent k_RangeBeginning = EditorGUIUtility.TrTextContent("Range Beginning", "Range beginning, value is inclusive.");
        public static readonly GUIContent k_RangeEnd = EditorGUIUtility.TrTextContent("Range End", "Range end, value is inclusive.");
        public static readonly GUIContent k_WindowTitle = EditorGUIUtility.TrTextContent("Multi-Frame Exporter");

        IFileDataReporter m_Exporter;

        int m_FirstRange = -1;
        int m_SecondRange = -1;

        string m_EventGuid;

        static void OpenWindow()
        {
            GetWindow<ProfilerMultiFrameSelector>();
        }

        public void Initialize(IFileDataReporter exporter)
        {
            m_Exporter = exporter;

            m_EventGuid = Guid.NewGuid().ToString();
            EditorIterationProfilerAnalytics.SendExportEvent(exporter.Name, ExportType.MultiWindow.ToString(), ExportStatus.Started.ToString(), m_EventGuid);
        }

        public void OnDestroy()
        {
            EditorIterationProfilerAnalytics.SendExportEvent(m_Exporter.Name, ExportType.MultiWindow.ToString(), ExportStatus.Finished.ToString(), m_EventGuid);
        }

        void OnEnable()
        {
            maxSize = new Vector2(300, 125);
            minSize = new Vector2(300, 125);

            titleContent = k_WindowTitle;
            ShowUtility();
        }

        void OnGUI()
        {
            m_FirstRange = EditorGUILayout.IntField(k_RangeBeginning, m_FirstRange);
            m_SecondRange = EditorGUILayout.IntField(k_RangeEnd, m_SecondRange);
            EditorGUILayout.HelpBox("Integers only. The limits are INCLUSIVE. If frame does not exist it won't be exported. -1 is the newest frame, however you can't do -1 and 15, for example; only -1 and -1.", MessageType.Info);

            if (m_FirstRange > m_SecondRange)
            {
                int x = m_FirstRange;
                m_FirstRange = m_SecondRange;
                m_SecondRange = x;
            }

            bool areValid = (m_FirstRange == m_SecondRange && m_FirstRange == -1 || m_FirstRange >= 0 && m_SecondRange >= 0);
            EditorGUI.BeginDisabledGroup(!areValid);
            if (GUILayout.Button("Export"))
            {
                Debug.Log($"Exporting Frames [{m_FirstRange},{m_SecondRange}] ({Mathf.Abs(m_SecondRange) - Mathf.Abs(m_FirstRange) + 1} frames)");
                EditorIterationProfilerWindow.ReportMultipleFrames(m_Exporter, m_FirstRange, m_SecondRange, m_EventGuid);
                Close();
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
