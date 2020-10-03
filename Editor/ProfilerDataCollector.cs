using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.EditorIterationProfiler.API;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.EditorIterationProfiler
{
    [Serializable]
    public class ProfilerDataCollector : IProfilerDataCollector
    {
        IIterationList m_IterationList;

        //static Dictionary<string, string> s_FlattenMarkers = new Dictionary<string, string>();
        static Dictionary<IterationEventKind, string[]> s_KeyMarkers = new Dictionary<IterationEventKind, string[]>();
        static Dictionary<string, EventDataFlags> s_MarkerFlags = new Dictionary<string, EventDataFlags>();
        static Dictionary<string, string[]> s_MarkerThread = new Dictionary<string, string[]>();

        [SerializeField]
        List<FrameSearchData> m_FrameSearchDataList = new List<FrameSearchData>();

        public ProfilerDataCollector(IIterationList iterationList)
        {
            Initialize(iterationList);

            // Find frame where asset import kicks off compilation.
            AddKeyMarkers(IterationEventKind.AssetImport, "CompilationPipeline.CompileScripts");

            //AddKeyMarkers(IterationEventKind.AssemblyCompilationStart, "CompilationPipeline.CompileAssemblyStart");
            //AddKeyMarkers(IterationEventKind.AssemblyCompilationFinish, "CompilationPipeline.CompileAssemblyFinish");

            AddKeyMarkers(IterationEventKind.AssemblyReload, "ReloadAssemblies");

            AddKeyMarkers(IterationEventKind.EnterPlayMode, "EnterPlayMode");

            AddKeyMarkers(IterationEventKind.ExitPlayMode, "ExitPlayMode");

            //AddFlattenMarker("ReloadAssembly", "ReloadAssemblies");
            //AddFlattenMarker("BeginReloadAssembly", "ReloadAssemblies");
            //AddFlattenMarker("EndReloadAssembly", "ReloadAssemblies");

            string[] userCodeMarkers =
            {
                "AssemblyReloadEvents.OnBeforeAssemblyReload()",
                "AssemblyReloadEvents.OnAfterAssemblyReload()",
                "DisabledScriptedObjects",
                "BackupScriptedObjects",
                "RestoreManagedReferences",
                "ProcessInitializeOnLoadAttributes",
                "ProcessInitializeOnLoadMethodAttributes",
                "AwakeScriptedObjects",
                "UnloadDomain"
            };

            foreach (string marker in userCodeMarkers)
            {
                AddMarkerFlags(marker, EventDataFlags.UserCode);
            }

            AddThreadMarker("UnloadDomain", "Domain unloader", "Finalizer");
        }

        void Initialize(IIterationList iterationList)
        {
            m_IterationList = iterationList;
        }

        public void Clear()
        {
            m_FrameSearchDataList.Clear();
        }

        public void Subscribe()
        {
            UnityProfiling.NewProfilerFrameRecorded += ProfilerNewFrame;
        }

        public void Unsubscribe()
        {
            UnityProfiling.NewProfilerFrameRecorded -= ProfilerNewFrame;
        }

        static void AddKeyMarkers(IterationEventKind iterationEventKind, params string[] markers)
        {
            s_KeyMarkers[iterationEventKind] = markers;
        }

        static string[] GetKeyMarkers(IterationEventKind iterationEventKind)
        {
            if (s_KeyMarkers.TryGetValue(iterationEventKind, out string[] marker))
            {
                return marker;
            }

            return null;
        }

        //static void AddFlattenMarker(string marker, string parentMarker)
        //{
        //    s_FlattenMarkers[marker] = parentMarker;
        //}

        static void AddMarkerFlags(string marker, EventDataFlags flags)
        {
            s_MarkerFlags[marker] = flags;
        }

        static void AddThreadMarker(string marker, params string[] threadNames)
        {
            s_MarkerThread[marker] = threadNames;
        }

        public void Collect(IterationEventKind iterationEventKind, IterationEventRoot iterationEventRoot, EventData rootEvent)
        {
            var frameSearchData = new FrameSearchData
            {
                IterationEventKind = iterationEventKind,
                EventListIndex = iterationEventRoot.IterationIndex,
                EventDataIndex = rootEvent.Index,
                MaxNumFrames = 600
            };

            m_FrameSearchDataList.Add(frameSearchData);

#if DEVELOPMENT_BUILD
            Debug.Log($"Starting to look for {iterationEventKind}");
#endif
        }

        void ProfilerNewFrame(int connectionId, int newFrameIndex)
        {
            // Remove frames which exceeded the search limit.
            for (var i = m_FrameSearchDataList.Count - 1; i >= 0; --i)
            {
                m_FrameSearchDataList[i].MaxNumFrames -= 1;

                if (m_FrameSearchDataList[i].MaxNumFrames <= 0)
                {
#if DEVELOPMENT_BUILD
                    Debug.Log($"Stopping search for event {m_FrameSearchDataList[i].IterationEventKind}");
#endif
                    m_FrameSearchDataList.RemoveAt(i);
                }
            }

            if (m_FrameSearchDataList.Count == 0)
            {
                return;
            }

            var frameData = UnityProfiling.GetFrame(newFrameIndex, 0);
            if (!frameData.valid)
            {
#if DEVELOPMENT_BUILD
                Debug.LogErrorFormat($"Unable to retrieve profiler data for frame {newFrameIndex}");
#endif
                return;
            }

            var markerData = new MarkerData(m_FrameSearchDataList.Count);
            for (var i = 0; i < markerData.Length; ++i)
            {
                var frameSearchData = m_FrameSearchDataList[i];
                frameSearchData.MaxNumFrames -= 1;
                markerData.Markers[i] = GetKeyMarkers(frameSearchData.IterationEventKind);
            }

            var framesNotFound = new List<FrameSearchData>();

            for (int i = 0; i < m_FrameSearchDataList.Count; ++i)
            {
                if (markerData.FrameIndices[i] == 0)
                {
                    framesNotFound.Add(m_FrameSearchDataList[i]);
                }
            }

#if DEVELOPMENT_BUILD
            Debug.Log($"Searching for events {string.Join(",", framesNotFound.Select(f => f.IterationEventKind.ToString()).ToArray())} in frame {newFrameIndex}");
#endif

            if (!FindMarkersInFrameData(frameData, frameData.GetRootItemID(), markerData, ProfilerDriver.deepProfiling ? 12 : 8))
            {
#if DEVELOPMENT_BUILD
                Debug.LogWarning($"Didn't find all markers in {newFrameIndex}");
#endif
                return;
            }

            var iterationListUpdated = false;

            for (int i = markerData.Length - 1; i >= 0; --i)
            {
                var frameSearchData = m_FrameSearchDataList[i];

                if (markerData.SampleIds[i] == UnityProfiling.InvalidSampleId)
                {
                    if (frameSearchData.MaxNumFrames <= 0)
                    {
#if DEVELOPMENT_BUILD
                        Debug.Log($"Stopping search for event {frameSearchData.IterationEventKind}");
#endif
                        m_FrameSearchDataList.RemoveAt(i);
                    }

                    continue;
                }

#if DEVELOPMENT_BUILD
                Debug.Log($"Reading profiling data for event {frameSearchData.IterationEventKind} from frame {markerData.FrameIndices[i]}");
#endif

                var eventDataList = m_IterationList.IterationEventRoots[frameSearchData.EventListIndex];
                var eventData = eventDataList.Events[frameSearchData.EventDataIndex];

                ReadProfilingData(eventDataList, eventData, markerData.FrameData[i], markerData.FrameData[i].GetRootItemID(), EventDataFlags.None, false);

                eventData.SetStartFinishTimeFromChildren();
                eventData.PostProcess();

                iterationListUpdated = true;

                m_FrameSearchDataList.RemoveAt(i);
            }

            if (iterationListUpdated)
            {
                m_IterationList.NotifyUpdated();
            }
        }

        internal static void ReadProfilingData(IterationEventRoot iterationEventRoot, EventData parentEventData, HierarchyFrameDataView frameData, int sampleId, EventDataFlags flags, bool parentHasSingleChild)
        {
            double duration = frameData.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnTotalTime);

            if (duration < 0.01)
            {
                return;
            }

            string markerName = frameData.GetItemName(sampleId);
            double startTime = frameData.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnStartTime);
            double finishTime = startTime + duration;

            int metadataCount = frameData.GetItemMetadataCount(sampleId);
            var sb = new StringBuilder();

            sb.Append("GC Alloc: " + frameData.GetItemColumnData(sampleId, 4) + "; ");
            sb.Append("Calls: " + frameData.GetItemColumnData(sampleId, 3) + "; ");


            if (metadataCount > 0)
            {
                sb.Append("Metadata: ");
                for (var i = 0; i < metadataCount; ++i)
                {
                    sb.Append(frameData.GetItemMetadata(sampleId, i));
                    sb.Append("; ");
                }
            }

            var markerMetadata = sb.Replace(',', ';').Remove(sb.Length - 2, 1).ToString().Trim();
            if (s_MarkerFlags.TryGetValue(markerName, out var eventMarkerFlags))
            {
                flags |= eventMarkerFlags;
            }

            var childEvent = iterationEventRoot.AddChildEvent(markerName, markerMetadata, startTime, finishTime, parentEventData, flags);

            if (s_MarkerThread.TryGetValue(markerName, out string[] threadNames))
            {
                foreach (string threadName in threadNames)
                {
                    var threadFrameData = UnityProfiling.GetFrame(frameData.frameIndex, threadName);
                    var threadEvent = iterationEventRoot.AddChildEvent($"Thread: {threadName}", markerMetadata, parentEventData.StartTime, parentEventData.FinishTime, childEvent, flags);

                    var threadChildIds = new List<int>();
                    threadFrameData.GetItemChildren(threadFrameData.GetRootItemID(), threadChildIds);

                    foreach (int childId in threadChildIds)
                    {
                        ReadProfilingData(iterationEventRoot, threadEvent, threadFrameData, childId, flags, threadChildIds.Count == 1);
                    }

                    threadEvent.SetStartFinishTimeFromChildren();
                }
            }

            var childIds = new List<int>();
            frameData.GetItemChildren(sampleId, childIds);

            foreach (int childId in childIds)
            {
                ReadProfilingData(iterationEventRoot, childEvent, frameData, childId, flags, childIds.Count == 1);
            }
        }

        static bool FindMarkersInFrameData(HierarchyFrameDataView frameData, int parentId, MarkerData markerData, int maxDepth)
        {
            if (maxDepth == 0)
            {
                return false;
            }

            var childrenIds = new List<int>();
            frameData.GetItemChildren(parentId, childrenIds);

            string[][] markers = markerData.Markers;
            int[] sampleIds = markerData.SampleIds;
            int[] frameIndices = markerData.FrameIndices;

            foreach (int childId in childrenIds)
            {
                string name = frameData.GetItemName(childId);

                for (var i = 0; i < markers.Length; ++i)
                {
                    if (markers[i] == null)
                    {
                        continue;
                    }

                    string[] localKeyMarkers = markers[i];

                    for (var j = 0; j < localKeyMarkers.Length; ++j)
                    {
                        if (name == localKeyMarkers[j])
                        {
                            sampleIds[i] = childId;
                            frameIndices[i] = frameData.frameIndex;
                            markerData.FrameData[i] = frameData;
                        }
                    }
                }

                if (markerData.FoundAllSampleIds)
                {
                    return true;
                }

                if (FindMarkersInFrameData(frameData, childId, markerData, maxDepth - 1))
                {
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        public class FrameSearchData
        {
            public int EventDataIndex;
            public int EventListIndex;
            public IterationEventKind IterationEventKind;
            public int MaxNumFrames;
        }

        struct MarkerData
        {
            public string[][] Markers;
            public int[] SampleIds;
            public int[] FrameIndices;
            public HierarchyFrameDataView[] FrameData;

            public MarkerData(int length)
            {
                Markers = new string[length][];
                SampleIds = new int[length];
                FrameIndices = new int[length];
                FrameData = new HierarchyFrameDataView[length];

                for (var i = 0; i < length; ++i)
                {
                    SampleIds[i] = UnityProfiling.InvalidSampleId;
                }
            }

            public bool FoundAllSampleIds
            {
                get
                {
                    for (var i = 0; i < Length; ++i)
                    {
                        if (SampleIds[i] == UnityProfiling.InvalidSampleId)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            public int Length => Markers.Length;
        }
    }
}
