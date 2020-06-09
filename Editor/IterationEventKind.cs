using System;

namespace UnityEditor.EditorIterationProfiler
{
    public enum IterationEventKind
    {
        None = 0,
        AssetImport,
        AssetPostProcess,
        ScriptCompilation,
        AssemblyCompilation,
        EnterPlayMode,
        ExitPlayMode,
        AssemblyReload,
        AssemblyCompilationStart,
        AssemblyCompilationFinish
    }
}
