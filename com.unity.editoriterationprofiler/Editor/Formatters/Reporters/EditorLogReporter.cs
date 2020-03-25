using System.Text;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public class EditorLogReporter : Formatter, IDataReporter
    {
        protected override StringBuilder GetPrefixStringBuilder(in IIterationList iterationList = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{EditorIterationProfilerIntegration.Instance.Settings}");
            sb.AppendLine();

            return sb;
        }

        protected override void RecursiveIterationEventRoot(in IterationEventRoot iterationEventRoot, ref StringBuilder sb, params object[] parameters)
        {
            double totalDuration = 0;
            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0 && ed.Duration > 0)
                {
                    totalDuration += ed.Duration;
                }
            }

            sb.AppendLine($"Iteration {iterationEventRoot.IterationIndex + 1} ({iterationEventRoot.IterationEventKind}) [{totalDuration:0.000} ms]");

            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0)
                {
                    RecursiveEventData(ed, iterationEventRoot, ref sb, parameters);
                }
            }
        }

        protected override void RecursiveEventData(in EventData ed, in IterationEventRoot parent, ref StringBuilder sb, params object[] parameters)
        {
            var indentation = new string('\t', (int)parameters[0]);

            sb.AppendLine($"{indentation}{ed.Identifier} ({ed.Duration:0.000} ms; {ed.Details})");

            RecursiveEventDataWalker(in ed, in parent, ref sb, parameters);
        }

        public void Report(in IIterationList iterationList, string path = null)
        {
            var message = GetFormatString(iterationList);

            Debug.Log(message);
        }
    }
}
