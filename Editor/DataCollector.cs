using System;
using UnityEditor.EditorIterationProfiler.API;
using UnityEngine.Assertions;

namespace UnityEditor.EditorIterationProfiler
{
    [Serializable]
    class DataCollector : IEventSubscriber
    {
        IIterationList m_IterationList;
        IProfilerDataCollector m_ProfilerDataCollector;

        public DataCollector(IProfilerDataCollector profilerCollector, IIterationList iterationList)
        {
            Initialize(profilerCollector, iterationList);
        }

        void Initialize(IProfilerDataCollector profilerCollector, IIterationList iterationList)
        {
            m_IterationList = iterationList;
            m_ProfilerDataCollector = profilerCollector;
        }

        public void Subscribe()
        {
            UnityEditorEvents.Subscribe();
            UnityEditorEvents.EditorEvent += EditorEvent;
        }

        public void Unsubscribe()
        {
            UnityEditorEvents.Unsubscribe();
            UnityEditorEvents.EditorEvent -= EditorEvent;
        }

        void EditorEvent(UnityEditorEvents.Event evt, string data)
        {
            Assert.IsNotNull(m_IterationList);

            if (!UnityProfiling.EditorProfilingEnabled)
            {
                return;
            }

            //Debug.Log($"EditorEvent: {evt} {data}");

            switch (evt)
            {
                case UnityEditorEvents.Event.ScriptCompilationStarted:
                {
                    m_IterationList.NewIteration(IterationEventKind.ScriptCompilation);

                    var assetImportEvent = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.AssetImport);
                    m_ProfilerDataCollector.Collect(IterationEventKind.AssetImport, m_IterationList.LastIterationEventRoot, assetImportEvent);

                    var scriptCompilationEvent = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.ScriptCompilation);

                    scriptCompilationEvent.SetStartTime();
                    break;
                }

                case UnityEditorEvents.Event.ScriptCompilationFinished:
                {
                    m_IterationList.LastIterationEventRoot.FinishEvent(IterationEventKind.ScriptCompilation);
                    break;
                }

                case UnityEditorEvents.Event.AssemblyCompilationStarted:
                {
                    var eventData = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.AssemblyCompilation, data, null);
                    m_IterationList.LastIterationEventRoot.SetParent(eventData, IterationEventKind.ScriptCompilation);

                    //m_ProfilerDataCollector.Collect(IterationEventKind.AssemblyCompilationStart, m_IterationList.LastIterationEventRoot, eventData);

                    eventData.SetStartTime();
                    break;
                }

                case UnityEditorEvents.Event.AssemblyCompilationFinished:
                {
                    //var ev = m_IterationList.LastIterationEventRoot.FindLastEvent(IterationEventKind.ScriptCompilation);
                    //m_ProfilerDataCollector.Collect(IterationEventKind.AssemblyCompilationFinish, m_IterationList.LastIterationEventRoot, ev);
                    m_IterationList.LastIterationEventRoot.FinishEvent(data);
                    break;
                }

                case UnityEditorEvents.Event.AssemblyReloadStarted:
                {
                    if (m_IterationList.LastIterationEventRoot != null)
                    {
                        var enterPlayModeEvent = m_IterationList.LastIterationEventRoot.FindLastEvent(IterationEventKind.EnterPlayMode);

                        // If this assembly reload is part of a enter play mode iteration,
                        // then do no add the event, as it happens in the same frame and
                        // profiling data will contain the domain reload event data.
                        if (enterPlayModeEvent != null)
                        {
                            return;
                        }

                        var eventData = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.AssemblyReload);
                        m_ProfilerDataCollector.Collect(IterationEventKind.AssemblyReload, m_IterationList.LastIterationEventRoot, eventData);
                    }
                    break;
                }

                case UnityEditorEvents.Event.AssemblyReloadFinished:
                {
                    break;
                }

                case UnityEditorEvents.Event.EnteringPlayMode:
                {
                    m_IterationList.NewIteration(IterationEventKind.EnterPlayMode);
                    var eventData = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.EnterPlayMode);
                    m_ProfilerDataCollector.Collect(IterationEventKind.EnterPlayMode, m_IterationList.LastIterationEventRoot, eventData);
                    break;
                }

                case UnityEditorEvents.Event.EnteredPlayMode:
                {
                    break;
                }

                case UnityEditorEvents.Event.ExitingPlayMode:
                {
                    m_IterationList.NewIteration(IterationEventKind.ExitPlayMode);
                    var eventData = m_IterationList.LastIterationEventRoot.StartEvent(IterationEventKind.ExitPlayMode);
                    m_ProfilerDataCollector.Collect(IterationEventKind.ExitPlayMode, m_IterationList.LastIterationEventRoot, eventData);
                    break;
                }

                case UnityEditorEvents.Event.ExitedPlayMode:
                {
                    break;
                }
            }
        }
    }
}
