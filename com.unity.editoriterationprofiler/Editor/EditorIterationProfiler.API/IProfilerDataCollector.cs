namespace UnityEditor.EditorIterationProfiler.API
{
    public interface IProfilerDataCollector : IEventSubscriber
    {
        void Collect(IterationEventKind iterationEventKind, IterationEventRoot iterationEventRoot, EventData rootEvent);
        void Clear();
    }
}
