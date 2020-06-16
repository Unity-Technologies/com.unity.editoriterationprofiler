using System;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UnityEditor.EditorIterationProfiler
{
    static class UnityProfiling
    {
        public static bool EditorProfilingEnabled
        {
            get => ProfilerDriver.enabled && ProfilerDriver.profileEditor;

            set
            {
                ProfilerDriver.enabled = value;
                ProfilerDriver.profileEditor = value;
            }
        }

        public static void SetProfileDeepScripts(bool deep)
        {
            if (ProfilerDriver.deepProfiling == deep)
                return;

            bool doApply = true;

            // When enabling / disabling deep script profiling we need to reload scripts. In play mode this might be intrusive, so we ask the user first.
            if (EditorApplication.isPlaying)
            {
                if (deep)
                {
                    doApply = EditorUtility.DisplayDialog("Enable deep script profiling", "Enabling deep profiling requires reloading scripts.", "Reload", "Cancel");
                }
                else
                {
                    doApply = EditorUtility.DisplayDialog("Disable deep script profiling", "Disabling deep profiling requires reloading all scripts", "Reload", "Cancel");
                }
            }

            if (doApply)
            {
                EditorIterationProfilerAnalytics.SendInteractionEvent(EditorProfilingEnabled, EditorApplication.isPlaying, ProfilerDriver.deepProfiling, EditorIterationProfilerIntegration.Instance.Settings.Flatten, EditorIterationProfilerIntegration.Instance.Settings.UserCode);

                ProfilerDriver.deepProfiling = deep;
                EditorPrefs.SetBool(EditorIterationProfilerWindow.Styles.k_EnableDeepProfile, deep);
                EditorIterationProfilerIntegration.Instance.Settings.DeepProfile = deep;
                EditorUtility.RequestScriptReload();
            }
        }

        public static int InvalidSampleId => HierarchyFrameDataView.invalidSampleId;

        public static event Action<int, int> NewProfilerFrameRecorded
        {
            add => ProfilerDriver.NewProfilerFrameRecorded += value;
            remove => ProfilerDriver.NewProfilerFrameRecorded -= value;
        }

        public static HierarchyFrameDataView GetFrame(int frameIndex, int threadIndex)
        {
            var frame = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false);

            return frame;
        }

        public static HierarchyFrameDataView GetFrame(int frameIndex, string threadName)
        {
            var iter = new ProfilerFrameDataIterator();
            iter.SetRoot(frameIndex, 0);

            int threadCount = iter.GetThreadCount(frameIndex);

            for (var i = 0; i < threadCount; ++i)
            {
                iter.SetRoot(frameIndex, i);
                string currentThreadName = iter.GetThreadName();

                if (currentThreadName.Equals(threadName, StringComparison.OrdinalIgnoreCase))
                {
                    iter.Dispose();
                    return GetFrame(frameIndex, i);
                }
            }

            iter.Dispose();
            throw new ArgumentException($"Could not find thread named '{threadName}'");
        }
    }
}
