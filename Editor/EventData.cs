using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler
{
    [Flags]
    public enum EventDataFlags
    {
        None = 0,
        UserCode = 1 << 0,
        Flatten = 1 << 1,
    }

    [Serializable]
    public class EventData
    {
        [field: NonSerialized]
        public List<EventData> Children { get; set; } = new List<EventData>();

        public int Index { get; }
        public int ParentIndex { get; set; }
        public string Identifier { get; set; }
        public double StartTime { get; set; }
        public double FinishTime { get; set; }
        public EventDataFlags Flags { get; set; }
        public IterationEventKind Kind { get; }

        [SerializeField]
        string m_Metadata;

        public string Details => m_Metadata;

        public string DisplayName
        {
            get
            {
                if (Kind == IterationEventKind.None)
                {
                    return $"{Identifier}";
                }

                string kindString = Kind.ToString();

                if (kindString == Identifier)
                {
                    return kindString;
                }

                return $"{kindString}: {Identifier}";
            }
        }

        public double Duration => FinishTime - StartTime;

        static double CurrentTime
        {
            get
            {
                long timeStamp = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
                return timeStamp;
            }
        }

        public EventData(IterationEventKind kind, string identifier, string metadata, int index)
        {
            Kind = kind;
            Flags = EventDataFlags.None;
            Identifier = identifier;
            m_Metadata = metadata;
            Index = index;
            ParentIndex = -1;
            StartTime = -1.0f;
            FinishTime = -1.0f;
        }

        public EventData(string identifier, string metadata, int index, double startTime, double finishTime, EventDataFlags flags)
        {
            Kind = IterationEventKind.None;
            Identifier = identifier;
            m_Metadata = metadata;
            Index = index;
            ParentIndex = -1;
            StartTime = startTime;
            FinishTime = finishTime;
            Flags = flags;
        }

        public void SetStartTime()
        {
            StartTime = CurrentTime;
            FinishTime = StartTime;
        }

        public void Finish()
        {
            if (StartTime > 0.0f)
            {
                FinishTime = CurrentTime;
            }
        }

        public void SetStartFinishTimeFromChildren()
        {
            if (Children.Count == 0)
            {
                return;
            }

            StartTime = Children.First().StartTime;
            FinishTime = Children.First().FinishTime;

            foreach (var child in Children)
            {
                StartTime = Math.Min(child.StartTime, StartTime);
                FinishTime = Math.Max(child.FinishTime, FinishTime);
            }
        }

        public void PostProcess(bool flatten = false)
        {
            if (Children.Count == 0)
            {
                return;
            }

            if (flatten && Children.Count == 1)
            {
                Flags |= EventDataFlags.Flatten;
            }

            foreach (var child in Children)
            {
                child.PostProcess(Children.Count == 1);
            }
        }
    }
}
