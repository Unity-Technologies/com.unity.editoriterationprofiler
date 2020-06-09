using System.Text;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    sealed class CSVReporter : FileReporter
    {
        string m_Extension = "csv";
        public override string Extension => m_Extension;
        
        string m_Name = "CSV";
        public override string Name => m_Name;

        protected override StringBuilder GetPrefixStringBuilder(in IIterationList iterationList = null)
        {
            var sb = new StringBuilder();

           sb.AppendLine($"{EditorIterationProfilerIntegration.Instance.Settings}");
           sb.AppendLine();

            return sb;
        }

        protected override void RecursiveIterationList(in IIterationList iterationList, ref StringBuilder sb, params object[] parameters)
        {
           sb.AppendLine($"Iteration ID,NodeType,Name,Details,Duration (ms)");

            for (int i = 0; i < iterationList.IterationEventRoots.Count; ++i)
            {
                RecursiveIterationEventRoot(iterationList.IterationEventRoots[i], ref sb, 1);
            }
        }

        protected override void RecursiveEventData(in EventData ed, in IterationEventRoot parent, ref StringBuilder sb, params object[] parameters)
        {
            if (ed.Children.Count == 0)
            {
               sb.AppendLine($"{parent.IterationIndex + 1} ({parent.IterationEventKind}), Leaf, {ed.Identifier}, {ed.Details}, {ed.Duration:0.000}");
            }
            else
            {
               sb.AppendLine($"{parent.IterationIndex + 1} ({parent.IterationEventKind}), Parent, {ed.Identifier}, {ed.Details}, {ed.Duration:0.000}");
            }

            RecursiveEventDataWalker(in ed, in parent, ref sb, parameters);
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
