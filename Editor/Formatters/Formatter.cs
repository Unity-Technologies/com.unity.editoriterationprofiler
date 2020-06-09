using System;
using System.Text;
using static System.FormattableString;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public abstract class Formatter
    {
        public virtual string GetFormatString(in IIterationList iterationList)
        {
            var finalStringBuilder = GetPrefixStringBuilder(iterationList).Append(GetMainStringBuilder(iterationList).Append(GetPostfixStringBuilder(iterationList)));

            return finalStringBuilder.ToString();
        }

        protected virtual StringBuilder GetPrefixStringBuilder(in IIterationList iterationList = null)
        {
            return new StringBuilder();
        }

        protected virtual StringBuilder GetMainStringBuilder(in IIterationList iterationList)
        {
            var sb = new StringBuilder();

            if (iterationList.IterationEventRoots.Count > 0)
            {
                RecursiveIterationList(iterationList, ref sb);
            }

            return sb;
        }

        protected virtual StringBuilder GetPostfixStringBuilder(in IIterationList iterationList = null)
        {
            return new StringBuilder();
        }

        protected virtual void RecursiveIterationList(in IIterationList iterationList, ref StringBuilder sb, params object[] parameters)
        {
            if (iterationList.IterationEventRoots.Count == 0)
            {
                return;
            }

            for (int i = 0; i < iterationList.IterationEventRoots.Count; ++i)
            {
                RecursiveIterationEventRoot(iterationList.IterationEventRoots[i], ref sb, 1, parameters);
            }
        }

        protected virtual void RecursiveIterationEventRoot(in IterationEventRoot iterationEventRoot, ref StringBuilder sb, params object[] parameters)
        {
            foreach (var ed in iterationEventRoot.Events)
            {
                if (ed.ParentIndex < 0)
                {
                    RecursiveEventData(ed, iterationEventRoot, ref sb, parameters);
                }
            }
        }

        protected virtual void RecursiveEventData(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, params object[] parameters)
        {
            RecursiveEventDataWalker(in ed, in iterationEventRoot, ref sb, parameters);
        }

        protected virtual void RecursiveEventDataWalker(in EventData ed, in IterationEventRoot iterationEventRoot, ref StringBuilder sb, params object[] parameters)
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

                RecursiveEventData(child, iterationEventRoot, ref sb, newParameters);
            }
        }

        public static string ToTimeString(double time)
        {
            return Invariant($"{time:0.000} ms");
        }

        public static string ToPercentageString(double percentage)
        {
            return Invariant($"{percentage:0.00}%");
        }
    }
}
