using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler
{
    [Serializable]
    class IterationList : IIterationList
    {
        [SerializeField]
        List<IterationEventRoot> m_IterationIterationEventRoots;
        public List<IterationEventRoot> IterationEventRoots => m_IterationIterationEventRoots;
        public IterationEventRoot LastIterationEventRoot => m_IterationIterationEventRoots.LastOrDefault();

        [SerializeField]
        List<IterationEventKind> m_IterationEventKinds;
        public List<IterationEventKind> IterationEventKinds => m_IterationEventKinds;
        
        [SerializeField]
        Action<IIterationList> m_Updated;
        public Action<IIterationList> Updated { get; set; }
        
        public IterationList()
        {
            m_IterationIterationEventRoots = new List<IterationEventRoot>();
            m_IterationEventKinds = new List<IterationEventKind>();
        }

        public void NewIteration(IterationEventKind kind)
        {
            int index = IterationEventRoots.Count;
            IterationEventRoots.Add(new IterationEventRoot(index, kind));
            IterationEventKinds.Add(kind);
        }

        public void Clear()
        {
            IterationEventRoots.Clear();
            IterationEventKinds.Clear();

            NotifyUpdated();
        }

        public void Reload()
        {
            foreach (var eventDataList in IterationEventRoots)
            {
                eventDataList.Reload();
            }

            NotifyUpdated();
        }

        public void NotifyUpdated()
        {
            Updated?.Invoke(this);
        }
    }
}
