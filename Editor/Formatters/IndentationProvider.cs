using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public static class IndentationProvider
    {
        static Dictionary<int, string> m_Cache = new Dictionary<int, string>(32);
        const char k_IndentationChar = '\t';

        public static string Get(int count)
        {
            if (count < 1)
            {
                return string.Empty;
            }
            if (!m_Cache.ContainsKey(count))
            {
                m_Cache.Add(count, new string(k_IndentationChar, count));
            }
            return m_Cache[count];
        }
    }
}
