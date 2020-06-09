using System;
using System.Text;
using System.Text.RegularExpressions;
using static System.Double;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public class Aggregator
    {
        public string Name { get; private set; }
        public double Time { get; private set; }
        public double TotalTime { get; private set; }
        public Regex Pattern { get; private set; }
        public int Depth { get; private set; }
        public int Calls { get; private set; }
        public bool IsRoot => !string.IsNullOrEmpty(Name) && m_Sb.Length != 0;
        public double Percentage => (Math.Abs(TotalTime) < Epsilon || Math.Abs(Time) < Epsilon) ? 0 : Time / TotalTime * 100;

        StringBuilder m_Sb;

        public Aggregator(int depth, string name, Regex pattern)
        {
            m_Sb = new StringBuilder();

            Time = 0;
            TotalTime = 0;
            Calls = 0;
            Name = name;
            Depth = depth;
            Pattern = pattern;
        }

        public void Aggregate(string s, bool isCall = false)
        {
            m_Sb.Append(s);

            if (isCall)
            {
                ++Calls;
            }
        }

        public void Aggregate(double time)
        {
            ++Calls;
            Time += time;
        }

        public void Aggregate(string s, double time)
        {
            Aggregate(s);
            Aggregate(time);
        }

        public void Reset()
        {
            Calls = 0;
            m_Sb.Clear();
            Time = 0;
            TotalTime = 0;
        }

        public void Reset(double totalTime)
        {
            Calls = 0;
            m_Sb.Clear();
            Time = 0;
            TotalTime = totalTime;
        }

        public override string ToString()
        {
            return m_Sb.ToString();
        }
    }

    public class HTMLAggregator : Aggregator
    {
        public string Style { get; private set;}

        public HTMLAggregator(int depth, string name, Regex pattern, string style) : base(depth, name, pattern)
        {
            Style = style;
        }
    }
}
