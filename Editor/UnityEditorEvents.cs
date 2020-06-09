using System;
using System.IO;
using UnityEditor.Compilation;

namespace UnityEditor.EditorIterationProfiler
{
    static class UnityEditorEvents
    {
        public enum Event
        {
            None = 0,
            ScriptCompilationStarted,
            ScriptCompilationFinished,
            AssemblyCompilationStarted,
            AssemblyCompilationFinished,
            AssemblyReloadStarted,
            AssemblyReloadFinished,
            EnteringPlayMode,
            EnteredPlayMode,
            ExitingPlayMode,
            ExitedPlayMode
        }

        public static event Action<Event, string> EditorEvent;

        public static void Subscribe()
        {
            CompilationPipeline.compilationStarted += ScriptCompilationStarted;
            CompilationPipeline.compilationFinished += ScriptCompilationFinished;
            CompilationPipeline.assemblyCompilationStarted += AssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinished;

            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadStarted;
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadFinished;

            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        public static void Unsubscribe()
        {
            CompilationPipeline.compilationStarted -= ScriptCompilationStarted;
            CompilationPipeline.compilationFinished -= ScriptCompilationFinished;
            CompilationPipeline.assemblyCompilationStarted -= AssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= AssemblyCompilationFinished;

            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadStarted;
            AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadFinished;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        static void ScriptCompilationStarted(object obj)
        {
            EditorEvent?.Invoke(Event.ScriptCompilationStarted, null);
        }

        static void ScriptCompilationFinished(object obj)
        {
            EditorEvent?.Invoke(Event.ScriptCompilationFinished, null);
        }

        static void AssemblyCompilationStarted(string assembly)
        {
            EditorEvent?.Invoke(Event.AssemblyCompilationStarted, Path.GetFileName(assembly));
        }

        static void AssemblyCompilationFinished(string assembly, CompilerMessage[] messages)
        {
            EditorEvent?.Invoke(Event.AssemblyCompilationFinished, Path.GetFileName(assembly));
        }

        static void AssemblyReloadStarted()
        {
            EditorEvent?.Invoke(Event.AssemblyReloadStarted, null);
        }

        static void AssemblyReloadFinished()
        {
            EditorEvent?.Invoke(Event.AssemblyReloadFinished, null);
        }

        static void PlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                {
                    EditorEvent?.Invoke(Event.EnteringPlayMode, null);
                }
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                {
                    EditorEvent?.Invoke(Event.EnteredPlayMode, null);
                }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                {
                    EditorEvent?.Invoke(Event.ExitingPlayMode, null);
                }
                    break;

                case PlayModeStateChange.EnteredEditMode:
                {
                    EditorEvent?.Invoke(Event.ExitedPlayMode, null);
                }
                    break;
            }
        }
    }
}
