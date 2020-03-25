using System.Text;
using UnityEditor.EditorIterationProfiler.Formatting;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler
{
    sealed class HTMLReporter : FileReporter
    {
        string m_Extension = "html";
        public override string Extension => m_Extension;

        string m_Name = "HTML";
        public override string Name => m_Name;

        double m_ParentTotalDuration;

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

        void RecursiveIterationList(in IIterationList iterationList, ref StringBuilder sb)
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

        void RecursiveIterationEventRoot(in IterationEventRoot iterationEventRoot, ref StringBuilder sb)
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

            var indentation1 = IndentationProvider.Get(1);

            sb.AppendLine($"<div class=\"TreeViewItem TreeViewItemCollapsed\">");
            sb.AppendLine(TimeDisplay(1, totalDuration));
            sb.AppendLine(PercentageDisplay(1, 100));
            sb.AppendLine($"{indentation1}<div class=\"NameDisplay\" onclick=\"treeViewToggle(event);\">Iteration {iterationEventRoot.IterationIndex + 1} ({iterationEventRoot.IterationEventKind})</div>");

            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0)
                {
                    RecursiveEventData(ed, iterationEventRoot, ref sb, 1, 100);

                    sb.AppendLine($"{indentation1}</div>");
                }
            }

            sb.AppendLine($"</div>");
        }

        void RecursiveEventData(in EventData ed, in IterationEventRoot parent, ref StringBuilder sb, int depth, double parentPercentage)
        {
            var indentation1 = IndentationProvider.Get(depth);
            var indentation2 = IndentationProvider.Get(depth + 1);
            double percentage = ed.Duration / m_ParentTotalDuration * 100;
            if (ed.Children.Count > 0)
            {
                sb.AppendLine($"{indentation1}<div class=\"TreeViewItem TreeViewItemCollapsed\">");
                sb.AppendLine(TimeDisplay(depth + 1, ed.Duration));
                sb.AppendLine(PercentageDisplay(depth + 1, percentage));

                if (!string.IsNullOrEmpty(ed.Details))
                {
                    sb.AppendLine($"{indentation2}<div class=\"NameDisplay\" onclick=\"treeViewToggle(event);\">{ed.Identifier} ({ed.Details})</div>");
                }
                else
                {
                    sb.AppendLine($"{indentation2}<div class=\"NameDisplay\" onclick=\"treeViewToggle(event);\">{ed.Identifier}</div>");
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

            RecursiveEventDataWalker(in ed, in parent, ref sb, depth, percentage);

            if (depth != 1)
            {
                sb.AppendLine($"{indentation1}</div>");
            }
        }

        void RecursiveEventDataWalker(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, int depth, double percentage)
        {
            if (ed.Children == null || ed.Children.Count == 0)
            {
                return;
            }

            foreach (var child in ed.Children)
            {
                RecursiveEventData(child, iterationEventRoot, ref sb, depth + 1, percentage);
            }
        }

        string TimeDisplay(int depth, double time)
        {
            return $"{IndentationProvider.Get(depth)}<div class=\"TimeDisplay\">{ToTimeString(time)}</div>";
        }

        string PercentageDisplay(int depth, double percentage)
        {
            return $"{IndentationProvider.Get(depth)}<div class=\"PercentageDisplay\">{ToPercentageString(percentage)}</div>";
        }

        string SimpleLabel(int depth, string s, string style = "")
        {
            if (!string.IsNullOrEmpty(style))
            {
                return $"{IndentationProvider.Get(depth)}<div style=\"{style}\">{s}</div>";
            }

            return $"{IndentationProvider.Get(depth)}<div>{s}</div>";
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
