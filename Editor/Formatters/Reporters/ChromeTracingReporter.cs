using System;
using System.Text;
using static System.FormattableString;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    sealed class ChromeTracingReporter : FileReporter
    {
        string m_Extension = "json";
        public override string Extension => m_Extension;

        string m_Name = "JSON for Chrometrace";
        public override string Name => m_Name;

        double m_ParentTotalDuration;

        protected override StringBuilder GetPrefixStringBuilder(in IIterationList iterationList = null)
        {
            var sb = new StringBuilder(32);

            sb.AppendLine("{");

            sb.AppendLine("\"otherData\": {");
            sb.AppendLine($"\"Readme\": \"Data serialized for use with Chrome Tracing. Load this file into chrome://tracing/ \",");
            sb.AppendLine($"\"Data\": \"{EditorIterationProfilerIntegration.Instance.Settings.ToString().Replace(Environment.NewLine, "; ")}\"");
            sb.AppendLine("},");

            sb.AppendLine("\"traceEvents\": [");

            return sb;
        }

        protected override StringBuilder GetMainStringBuilder(in IIterationList iterationList)
        {
            var sb = new StringBuilder();

            if (iterationList.IterationEventRoots.Count > 0)
            {
                RecursiveIterationList(iterationList, ref sb);

                // Remove the last comma
                for (int i = sb.Length - 1; i > 0; --i)
                {
                    if (sb[i] == ',')
                    {
                        sb.Remove(i, 1);
                        break;
                    }
                }
            }

            return sb;
        }

        protected override StringBuilder GetPostfixStringBuilder(in IIterationList iterationList = null)
        {
            var sb = new StringBuilder(4);

            sb.AppendLine("]");
            sb.AppendLine("}");

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

            m_ParentTotalDuration = totalDuration;

            sb.AppendLine(MetadataEvent("process_name", (iterationEventRoot.IterationIndex + 1).ToString(), "1", $"Iteration {iterationEventRoot.IterationIndex + 1} ({iterationEventRoot.IterationEventKind.ToString()}) ({totalDuration:0.000}ms) "));

            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0)
                {
                    RecursiveEventData(ed, iterationEventRoot, ref sb, ed.StartTime, parameters);
                }
            }
        }

        void RecursiveEventData(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, double parentStartTime, params object[] parameters)
        {
            if (ed.Duration > float.Epsilon)
            {
                sb.AppendLine(DurationEvent(ed.Identifier, (iterationEventRoot.IterationIndex + 1).ToString(), "1", iterationEventRoot.IterationEventKind.ToString(), ed.StartTime - parentStartTime, ed.Duration));
            }

            RecursiveEventDataWalker(in ed, in iterationEventRoot, ref sb, parentStartTime, parameters);
        }

        void RecursiveEventDataWalker(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, double parentStartTime, params object[] parameters)
        {
            if (ed.Children == null || ed.Children.Count == 0)
            {
                return;
            }

            foreach (var child in ed.Children)
            {
                var newParameters = new object[parameters.Length];
                Array.Copy(parameters, newParameters, parameters.Length);
                newParameters[0] = (int)newParameters[0] + 1;

                RecursiveEventData(child, iterationEventRoot, ref sb, parentStartTime, newParameters);
            }
        }

        string DurationEvent(string identifier, string pid, string tid, string category, double startTime, double duration)
        {
            return Invariant($"{{ \"pid\": {pid}, \"tid\": {tid}, \"ph\": \"X\", \"name\": \"{identifier}\", \"cat\": \"{category}\", \"ts\": {startTime * 1000.0:0.000}, \"dur\": {duration * 1000}, \"args\": {{ \"Duration (ms)\": {duration:0.000}, \"Start Time (ms)\": {startTime:0.000}, \"Percentage of total\": {(duration / m_ParentTotalDuration * 100.0):0.000} }} }},");
        }

        static string MetadataEvent(string identifier, string pid, string tid, string name)
        {
            return $"{{ \"pid\": {pid}, \"tid\": {tid}, \"ph\": \"M\", \"name\": \"{identifier}\", \"args\": {{ \"name\": \"{name}\" }} }},";
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
