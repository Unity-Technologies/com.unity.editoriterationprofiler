using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.EditorIterationProfiler.Formatting;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler
{
    public class HTMLPerfReporter : FileReporter
    {
        string m_Extension = "html";
        public override string Extension => m_Extension;

        string m_Name = "HTML Performance Report";
        public override string Name => m_Name;

        // [0-100] percentage. Determines when the children of the current node will stop being logged. If the Parent is under this percentage, only the direct children of it will be printed. After that it stops.
        const double k_PrunePercentage = 1;

        // [0-100] percentage. Determines what is considered as a divergence in times. This is used to 'flatten' the structure. 1 will basically turn it 'off'.
        const double k_DivergenceFactor = 75;

        IList<HTMLAggregator> m_Aggregators = new List<HTMLAggregator>();

        double m_ParentTotalDuration;

        public HTMLPerfReporter()
        {
            m_Aggregators.Add(new HTMLAggregator(1, "Garbage Collector", new Regex(@"GC\.C"), "color:#F00"));
            m_Aggregators.Add(new HTMLAggregator(1, "Mono.JIT", new Regex("Mono.JIT"), "color:#FA1"));
            m_Aggregators.Add(new HTMLAggregator(1, "[MISSING MARKER TIME]", new Regex("MISSING MARKER TIME"), "color:#B54"));
            m_Aggregators.Add(new HTMLAggregator(1, "EIP Time", new Regex(@"EditorIterationProfiler"), "color:#3A7"));
            m_Aggregators.Add(new HTMLAggregator(1, "Object Stuff", new Regex(@"Object\."), "color:#1C7"));
            m_Aggregators.Add(new HTMLAggregator(1, "OnEnable & Awake Stuff", new Regex(@"\.Awake|\.OnEnable"), "color:#17C"));
            m_Aggregators.Add(new HTMLAggregator(1, "UnityEditor Stuff", new Regex(@"UnityEditor\."), "color:#52F"));
            m_Aggregators.Add(new HTMLAggregator(1, "UnityEngine Stuff", new Regex(@"UnityEngine\."), "color:#81B"));
            m_Aggregators.Add(new HTMLAggregator(1, "GUI Stuff", new Regex(@"GUI"), "color:#C0F"));
        }

        public override string GetFormatString(in IIterationList iterationList)
        {
            var finalStringBuilder = GetPrefixStringBuilder(iterationList).Append(GetMainStringBuilder(iterationList).Append(GetPostfixStringBuilder(iterationList)));

            return finalStringBuilder.ToString();
        }

        protected override StringBuilder GetPrefixStringBuilder(in IIterationList iterationList = null)
        {
            var sb = new StringBuilder();

            var fileGUID = AssetDatabase.FindAssets("HTMLReporterPrefix").FirstOrDefault();
            var filePath = AssetDatabase.GUIDToAssetPath(fileGUID);
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
            sb.Append(file.text);

            sb.AppendLine($"<div class=\"Details Wordwrap\">{EditorIterationProfilerIntegration.Instance.Settings}</div>");

            sb.AppendLine($"<br>");
            sb.AppendLine();

            sb.AppendLine($"<body>");
            sb.AppendLine($"<div class=\"ToggleButton\" onclick=\"treeViewToggleAll(true);\">Expand all</div>");
            sb.AppendLine($"<div class=\"ToggleButton\" onclick=\"treeViewToggleAll(false);\">Collapse all</div>");

            return sb;
        }

        protected override StringBuilder GetMainStringBuilder(in IIterationList iterationList)
        {
            var sb = new StringBuilder();

            if (iterationList.IterationEventRoots.Count > 0)
            {
                RecursiveIterationList(iterationList, ref sb);
            }
            else
            {
                sb.AppendLine($"<div>ERROR: No data to export!</div>");
            }

            return sb;
        }

        protected void RecursiveIterationList(in IIterationList iterationList, ref StringBuilder sb)
        {
            if (iterationList.IterationEventRoots.Count == 0)
            {
                return;
            }

            for (int i = 0; i < iterationList.IterationEventRoots.Count; ++i)
            {
                RecursiveIterationEventRoot(iterationList.IterationEventRoots[i], ref sb);
            }
        }

        protected void RecursiveIterationEventRoot(in IterationEventRoot iterationEventRoot, ref StringBuilder sb)
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

            foreach (var agg in m_Aggregators)
            {
                agg.Reset(totalDuration);
            }


            var foundRoot = new List<bool>(m_Aggregators.Count);

            for (int i = 0; i < foundRoot.Capacity; ++i)
            {
                foundRoot.Add(false);
            }


            var indentation1 = IndentationProvider.Get(1);

            sb.AppendLine($"<div class=\"TreeViewItem TreeViewItemCollapsed\">");
            sb.AppendLine(TimeDisplay(1, totalDuration));
            sb.AppendLine(PercentageDisplay(1, 100));
            sb.AppendLine($"{indentation1}<div class=\"NameDisplay\" onclick=\"treeViewToggle(event);\">Iteration {iterationEventRoot.IterationIndex + 1} ({iterationEventRoot.IterationEventKind})</div>");

            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0)
                {
                    RecursiveEventData(ed, iterationEventRoot, ref sb, 1, 100, foundRoot);

                    sb.AppendLine($"{indentation1}</div>");
                }
            }

            sb.AppendLine(AggregateDataToString());

            sb.AppendLine($"</div>");
        }

        protected void RecursiveEventData(in EventData ed, in IterationEventRoot parent, ref StringBuilder sb, int depth, double parentPercentage, List<bool> foundRoot)
        {
            var indentation1 = IndentationProvider.Get(depth);
            var indentation2 = IndentationProvider.Get(depth + 1);
            double percentage = ed.Duration / m_ParentTotalDuration * 100;

            var currentFoundRoot = new List<bool>(foundRoot);

            AggregateEventData(ed, percentage, ref currentFoundRoot);

            if (parentPercentage * k_DivergenceFactor / 100 < percentage && ed.Children.Count != 0 && depth > 1 && percentage > 1)
            {
                RecursiveEventDataWalker(in ed, in parent, ref sb, depth, percentage, currentFoundRoot);
                return;
            }

            bool nodeWasWritten = false;

            if (percentage >= k_PrunePercentage)
            {
                if (ed.Children.Count > 0)
                {
                    sb.AppendLine($"{indentation1}<div class=\"TreeViewItem TreeViewItemCollapsed\">");
                    sb.AppendLine(TimeDisplay(depth + 1, ed.Duration));
                    sb.AppendLine(PercentageDisplay(depth + 1, percentage));
                    sb.AppendLine($"{indentation2}<div class=\"NameDisplay\" onclick=\"treeViewToggle(event);\">{ed.Identifier}</div>");

                    double childrenTime = 0;
                    foreach (var child in ed.Children)
                    {
                        childrenTime += child.Duration;
                    }

                    var ag = FindAggregator("MISSING MARKER TIME");
                    if (ag != null)
                    {
                        if(ed.Duration > childrenTime)
                        {
                            ag.Aggregate(ed.Duration - childrenTime);
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"{indentation1}<div class=\"TreeViewItem TreeViewItemLeaf\">");
                    sb.AppendLine(TimeDisplay(depth + 1, ed.Duration));
                    sb.AppendLine(PercentageDisplay(depth + 1, percentage));

                    if (!string.IsNullOrEmpty(ed.Details))
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"[{ed.Identifier}] ({ed.Details})"));
                    }
                    else
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"[{ed.Identifier}]"));
                    }
                }

                nodeWasWritten = true;
            }
            else if (parentPercentage >= k_PrunePercentage && percentage < k_PrunePercentage)
            {
                sb.AppendLine($"{indentation1}<div class=\"TreeViewItem TreeViewItemLeaf\">");
                sb.AppendLine(TimeDisplay(depth + 1, ed.Duration));
                sb.AppendLine(PercentageDisplay(depth + 1, percentage));

                if (!string.IsNullOrEmpty(ed.Details))
                {
                    if (ed.Children.Count == 0)
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"[{ed.Identifier}] ({ed.Details})"));
                    }
                    else
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"{{{ed.Identifier}}} ({ed.Details})"));
                    }
                }
                else
                {
                    if (ed.Children.Count == 0)
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"[{ed.Identifier}]"));
                    }
                    else
                    {
                        sb.AppendLine(SimpleLabel(depth + 1, $"{{{ed.Identifier}}}"));
                    }
                }

                nodeWasWritten = true;
            }

            RecursiveEventDataWalker(in ed, in parent, ref sb, depth, percentage, currentFoundRoot);

            if (nodeWasWritten && depth != 1)
            {
                sb.AppendLine($"{indentation1}</div>");
            }
        }

        protected void RecursiveEventDataWalker(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, int depth, double percentage, List<bool> foundRoot)
        {
            if (ed.Children == null || ed.Children.Count == 0)
            {
                return;
            }

            foreach (var child in ed.Children)
            {
                RecursiveEventData(child, iterationEventRoot, ref sb, depth + 1, percentage, foundRoot);
            }
        }

        protected static string TimeDisplay(int depth, double time)
        {
            return $"{IndentationProvider.Get(depth)}<div class=\"TimeDisplay\">{ToTimeString(time)}</div>";
        }

        protected static string PercentageDisplay(int depth, double percentage)
        {
            return $"{IndentationProvider.Get(depth)}<div class=\"PercentageDisplay\">{ToPercentageString(percentage)}</div>";
        }

        protected static string SimpleLabel(int depth, string s, string style = "")
        {
            if (!string.IsNullOrEmpty(style))
            {
                return $"{IndentationProvider.Get(depth)}<div style=\"{style}\">{s}</div>";
            }

            return $"{IndentationProvider.Get(depth)}<div>{s}</div>";
        }

        protected void AggregateEventData(EventData ed, double percentage, ref List<bool> foundRoot)
        {
            for (int i = 0; i < m_Aggregators.Count; ++i)
            {
                var agg = m_Aggregators[i];

                if (agg.Pattern.IsMatch(ed.Identifier) && !foundRoot[i])
                {
                    agg.Aggregate(TimeDisplay(agg.Depth + 1, ed.Duration) + Environment.NewLine, ed.Duration);
                    agg.Aggregate(PercentageDisplay(agg.Depth + 1, percentage) + Environment.NewLine);
                    agg.Aggregate(SimpleLabel(agg.Depth + 1, ed.Identifier + (string.IsNullOrEmpty(ed.Details) ? "" : $" ({ed.Details})"), agg.Style) + Environment.NewLine);
                    foundRoot[i] = true;
                }
            }
        }

        protected void AggregateData(string name, double time)
        {
            foreach (var agg in m_Aggregators)
            {
                if (agg.Pattern.IsMatch(name))
                {
                    agg.Aggregate(time);
                }
            }
        }

        protected string AggregateDataToString()
        {
            var sb = new StringBuilder();

            foreach (var agg in m_Aggregators)
            {
                sb.Append(AggregatorLabel(agg));
            }

            return sb.ToString();
        }

        protected HTMLAggregator FindAggregator(string name)
        {
            return m_Aggregators.FirstOrDefault(x => x.Pattern.IsMatch(name));
        }

        protected static StringBuilder AggregatorLabel(HTMLAggregator ag)
        {
            var sb = new StringBuilder();
            string indentation = IndentationProvider.Get(ag.Depth);

            if (ag.IsRoot)
            {
                sb.AppendLine($"{indentation}<div class=\"TreeViewItem TreeViewItemCollapsed\">");
                sb.AppendLine($"{indentation}<div class=\"TimeDisplay\">{ToTimeString(ag.Time)}</div>");
                sb.AppendLine($"{indentation}<div class=\"PercentageDisplay\">{ToPercentageString(ag.Percentage)}</div>");
                sb.AppendLine($"{indentation}<div style=\"{ag.Style}\" onclick=\"treeViewToggle(event);\" class=\"NameDisplay\">{ag.Name} (Found Instances: {ag.Calls})</div>");
                sb.AppendLine($"{indentation}<div class=\"TreeViewItem TreeViewItemLeaf\">");
                sb.AppendLine(ag.ToString());
                sb.AppendLine($"{indentation}</div>");
            }
            else
            {
                sb.AppendLine($"{indentation}<div class=\"TreeViewItem TreeViewItemLeaf\">");
                sb.AppendLine($"{indentation}<div class=\"TimeDisplay\">{ToTimeString(ag.Time)}</div>");
                sb.AppendLine($"{indentation}<div class=\"PercentageDisplay\">{ToPercentageString(ag.Percentage)}</div>");
                sb.AppendLine($"{indentation}<div style=\"{ag.Style}\">{ag.Name} ({ag.Calls})</div>");
            }

            sb.AppendLine($"{indentation}</div>");

            return sb;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
