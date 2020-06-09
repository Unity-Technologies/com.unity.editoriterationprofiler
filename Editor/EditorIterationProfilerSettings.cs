using System;
using System.Text;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler.API
{
    [Serializable]
    public class EditorIterationProfilerSettings
    {
        [field: SerializeField]
        public bool DeepProfile { get; set; }

        [field: SerializeField]
        public bool Flatten { get; set; }

        [field: SerializeField]
        public bool UserCode { get; set; }

        internal string UnityVersion => Application.unityVersion;
        internal string ProductName => PlayerSettings.productName;
        internal string Time => DateTime.UtcNow + " UTC";
        internal bool FastEnterPlayMode => EditorSettings.enterPlayModeOptionsEnabled;
        internal string Platform => Application.platform.ToString();
#if UNITY_2020_OR_NEWER
        internal string CodeOptimization => CompilationPipeline.codeOptimization.ToString();
#endif
        internal string SystemInfo => UnityEngine.SystemInfo.processorType.TrimEnd() + "; " + UnityEngine.SystemInfo.systemMemorySize / 1000 + " GB RAM";

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Time: {EditorIterationProfilerIntegration.Instance.Settings.Time}");
            sb.AppendLine($"Unity Version: {EditorIterationProfilerIntegration.Instance.Settings.UnityVersion}");
            sb.AppendLine($"Platform: {EditorIterationProfilerIntegration.Instance.Settings.Platform}");
            sb.AppendLine($"System Specs: {EditorIterationProfilerIntegration.Instance.Settings.SystemInfo}");
            sb.AppendLine($"Product Name: {EditorIterationProfilerIntegration.Instance.Settings.ProductName}");
            sb.AppendLine($"Deep Profile: {EditorIterationProfilerIntegration.Instance.Settings.DeepProfile}");
            sb.AppendLine($"Flatten: {EditorIterationProfilerIntegration.Instance.Settings.Flatten}");
            sb.AppendLine($"User Code: {EditorIterationProfilerIntegration.Instance.Settings.UserCode}");
            sb.AppendLine($"Fast EnterPlayMode: {EditorIterationProfilerIntegration.Instance.Settings.FastEnterPlayMode}");
#if UNITY_2020_OR_NEWER
           sb.Append($"Editor Code Optimization: {EditorIterationProfilerIntegration.Instance.Settings.CodeOptimization}");
#endif
            return sb.ToString();
        }
    }
}
