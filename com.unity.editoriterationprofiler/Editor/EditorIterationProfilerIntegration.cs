using System.Linq;
using UnityEditor.EditorIterationProfiler.API;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.EditorIterationProfiler
{
    [InitializeOnLoad]
    public static class EditorIterationProfilerIntegration
    {
        static EditorIterationProfilerController s_Instance;
        public static IEditorIterationProfilerController Instance
        {
            get
            {
                if (s_Instance != null)
                {
                    return s_Instance;
                }

                FindOrCreateInstance();
                return s_Instance;
            }
        }

        static EditorIterationProfilerIntegration()
        { 
            EditorApplication.delayCall += Initialize;
        }

        internal static void Initialize()
        {
            FindOrCreateInstance();
        }

        static void FindOrCreateInstance()
        {
            EditorIterationProfilerController[] instances = Resources.FindObjectsOfTypeAll<EditorIterationProfilerController>();

            if (instances != null)
            {
                s_Instance = instances.FirstOrDefault();
            }

            if (s_Instance == null)
            {
                s_Instance = ScriptableObject.CreateInstance<EditorIterationProfilerController>();
                s_Instance.hideFlags = HideFlags.DontSave;
            }
            s_Instance.Initialize();
        }

        public static void Clear()
        {
            s_Instance.Clear();
        }


        [MenuItem("Window/Analysis/Editor Iteration Profiler/Purge Caches", priority = 21)]
        public static void PurgeScriptableObjects()
        {
            Debug.Log("Caches Purged!", s_Instance);

            if (s_Instance != null)
            {
                Clear();
            }

            EditorIterationProfilerController[] instances = Resources.FindObjectsOfTypeAll<EditorIterationProfilerController>();
            foreach (var instance in instances)
            {
                Object.DestroyImmediate(instance);
            }

            FindOrCreateInstance();
        }
    }
}
