using System.Collections.Generic;
using UnityEditor.EditorIterationProfiler.API;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.EditorIterationProfiler
{
    public class EditorIterationProfilerController : ScriptableObject, IEditorIterationProfilerController
    {
        [SerializeField]
        IterationList m_IterationList;
        public IIterationList IterationList => m_IterationList;

        [SerializeField]
        ProfilerDataCollector m_ProfilerDataCollector;
        public IProfilerDataCollector ProfilerDataCollector => m_ProfilerDataCollector;

        [SerializeField]
        DataReporterProvider m_DataReporterProvider;
        public IDataReporterProvider DataReporterProvider => m_DataReporterProvider;

        [SerializeField]
        EditorIterationProfilerSettings m_Settings;
        public EditorIterationProfilerSettings Settings => m_Settings;

        [SerializeField]
        DataCollector m_DataCollector;

        public EditorIterationProfilerController()
        {
            m_IterationList = new IterationList();

            m_ProfilerDataCollector = new ProfilerDataCollector(m_IterationList);

            m_DataCollector = new DataCollector(m_ProfilerDataCollector, m_IterationList);

            m_DataReporterProvider = new DataReporterProvider();

            m_Settings = new EditorIterationProfilerSettings();
        }

        public EditorIterationProfilerController(IIterationList iterationList, IProfilerDataCollector profilerDataCollector)
        {
            m_IterationList = iterationList as IterationList;

            m_ProfilerDataCollector = profilerDataCollector as ProfilerDataCollector;

            m_DataCollector = new DataCollector(m_ProfilerDataCollector, m_IterationList);

            m_DataReporterProvider = new DataReporterProvider();

            m_Settings = new EditorIterationProfilerSettings();
        }

        void OnEnable()
        {
            m_DataCollector.Subscribe();
            m_ProfilerDataCollector.Subscribe();
        }

        void OnDisable()
        {
            m_DataCollector.Unsubscribe();
            m_ProfilerDataCollector.Unsubscribe();
        }

        internal void Initialize()
        {
            m_IterationList.Reload();
        }

        public void Clear()
        {
            m_IterationList.Clear();
            m_ProfilerDataCollector.Clear();
        }
    }
}
