namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public interface IDataReporter
    {
        string GetFormatString(in IIterationList iterationList);
        void Report(in IIterationList iterationList, string path = null);
    }

    public interface IFileDataReporter : IDataReporter
    {
        string Extension { get; }
        string Name { get; }
        new void Report(in IIterationList iterationList, string path);
    }
}
