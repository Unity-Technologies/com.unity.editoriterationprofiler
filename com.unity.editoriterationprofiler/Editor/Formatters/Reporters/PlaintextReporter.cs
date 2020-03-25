namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public sealed class PlaintextReporter : FileReporter
    {
        string m_Extension = "txt";
        public override string Extension => m_Extension;

        string m_Name = "Plaintext";
        public override string Name => m_Name;

        IDataReporter m_El = new EditorLogReporter();

        public override void Report(in IIterationList iterationList, string path)
        {
            ReportToFile(m_El.GetFormatString(iterationList), path);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
