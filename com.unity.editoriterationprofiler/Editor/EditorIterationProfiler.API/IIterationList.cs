using System;
using System.Collections.Generic;
using UnityEditor.EditorIterationProfiler.Formatting;

namespace UnityEditor.EditorIterationProfiler
{
    public interface IIterationList
    {
        List<IterationEventRoot> IterationEventRoots { get; }
        IterationEventRoot LastIterationEventRoot { get; }
        List<IterationEventKind> IterationEventKinds { get; }
        Action<IIterationList> Updated { get; set; }
        void NewIteration(IterationEventKind kind);
        void NotifyUpdated();
    }
}
