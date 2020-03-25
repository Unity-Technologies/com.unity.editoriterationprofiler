using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler
{
    [Serializable]
    public class IterationEventRoot
    {
        [SerializeField]
        List<EventData> m_Events = new List<EventData>();
        public List<EventData> Events => m_Events;

        [SerializeField]
        int m_IterationIndex;
        public int IterationIndex
        {
            get => m_IterationIndex;
            set => m_IterationIndex = value;
        }

        [SerializeField]
        IterationEventKind m_IterationEventKind;
        public IterationEventKind IterationEventKind => m_IterationEventKind;

        [SerializeField]
        Dictionary<string, EventData> m_EventDictionary = new Dictionary<string, EventData>();

        public IterationEventKind LastIterationEventKind
        {
            get
            {
                if (Events.Count == 0)
                {
                    return IterationEventKind.None;
                }

                return Events.Last().Kind;
            }
        }

        public IterationEventRoot(int index)
        {
            IterationIndex = index;
            m_IterationEventKind = IterationEventKind.None;
        }

        public IterationEventRoot(int index, IterationEventKind eventKind)
        {
            IterationIndex = index;
            m_IterationEventKind = eventKind;
        }

        public void Reload()
        {
            foreach (var eventData in Events)
            {
                eventData.Children = new List<EventData>();
            }

            foreach (var eventData in Events)
            {
                int parentIndex = eventData.ParentIndex;

                if (parentIndex >= 0)
                {
                    Events[parentIndex].Children.Add(eventData);
                }
            }
        }

        public EventData StartEvent(IterationEventKind kind, string identifier, string metadata)
        {
            int index = Events.Count;
            var eventData = new EventData(kind, identifier, metadata, index);

            Events.Add(eventData);
            m_EventDictionary[identifier] = eventData;

            return eventData;
        }

        public EventData StartEvent(IterationEventKind kind)
        {
            return StartEvent(kind, kind.ToString(), null);
        }

        public void FinishEvent(string identifier)
        {
            var eventData = FindLastEvent(identifier);
            eventData?.Finish();
        }

        public void FinishEvent(IterationEventKind kind)
        {
            string identifier = kind.ToString();
            FinishEvent(identifier);
        }

        public EventData AddChildEvent(string identifier, string metadata, double startTime, double finishTime, EventData parentEvent, EventDataFlags flags = EventDataFlags.None)
        {
            int index = Events.Count;

            var eventData = new EventData(identifier, metadata, index, startTime, finishTime, flags);
            Events.Add(eventData);

            SetParent(eventData, parentEvent);

            return eventData;
        }

        public static void SetParent(EventData child, EventData parent)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child), "Argument is null");
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "Argument is null");
            }

            child.ParentIndex = parent.Index;
            parent.Children.Add(child);
        }

        public void SetParent(EventData child, IterationEventKind kind)
        {
            var parent = FindLastEvent(kind);
            SetParent(child, parent);
        }

        public EventData FindLastEvent(IterationEventKind kind)
        {
            string identifier = kind.ToString();
            return FindLastEvent(identifier);
        }

        public EventData FindLastEvent(string identifier)
        {

            if (m_EventDictionary != null && m_EventDictionary.TryGetValue(identifier, out var eventData))
            {
                return eventData;
            }

            // If an assembly reload happens, then eventDictionary is
            // empty and we search for the last event with matching
            // identifier.
            for (int i = Events.Count - 1; i >= 0; --i)
            {
                if (Events[i].Identifier == identifier)
                {
                    return Events[i];
                }
            }

            return null;
        }
    }
}
