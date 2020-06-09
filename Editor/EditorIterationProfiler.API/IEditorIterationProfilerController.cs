namespace UnityEditor.EditorIterationProfiler.API
{
    public interface IEditorIterationProfilerController
    {
        IIterationList IterationList { get; }
        IDataReporterProvider DataReporterProvider { get; }

        EditorIterationProfilerSettings Settings { get; }
    }
}
