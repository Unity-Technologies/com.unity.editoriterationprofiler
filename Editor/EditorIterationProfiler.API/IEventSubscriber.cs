namespace UnityEditor.EditorIterationProfiler
{
    public interface IEventSubscriber
    {
        void Subscribe();
        void Unsubscribe();
    }
}
