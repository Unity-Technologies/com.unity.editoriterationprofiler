using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.EditorIterationProfiler.Formatting
{
    public abstract class FileReporter : Formatter, IFileDataReporter
    {
        string m_Extension = null;
        public virtual string Extension => m_Extension;

        string m_Name = null;
        public virtual string Name => m_Name;

        public virtual void Report(in IIterationList iterationList, string path)
        {
            ReportToFile(GetFormatString(iterationList), path);
        }

        protected virtual void ReportToFile(string message, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path), "Path cannot be null");
            }

            File.WriteAllText(path, message);

            Debug.Log($"\"{path}\" exported!");
        }

        public abstract override string ToString();
    }
}
