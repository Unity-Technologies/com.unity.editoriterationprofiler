using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorIterationProfiler.Formatting;
using UnityEngine.Assertions;

namespace UnityEditor.EditorIterationProfiler
{
    public interface IDataReporterProvider
    {
        IList<T> GetAllReporters<T>() where T : IDataReporter;
        T TryGetReporter<T>(Type type) where T : IDataReporter;
        bool IsFileExtensionSupported(string extension);
        IDataReporter TryGetDataReporter(string typeName);
        IFileDataReporter TryGetDataFileReporter(string typeName);
    }

    public class DataReporterProvider : IDataReporterProvider
    {
        HashSet<IDataReporter> m_DataReporters;
        HashSet<IFileDataReporter> m_DataFileReporters;

        public DataReporterProvider()
        {
            InitializeDataFileReporters();

            InitializeDataReporters();
        }

        void InitializeDataFileReporters()
        {
            m_DataFileReporters = new HashSet<IFileDataReporter>();

            foreach (var rep in TypeCache.GetTypesDerivedFrom(typeof(IFileDataReporter)))
            {
                if (rep.GetConstructors().Length > 0)
                {
                    var instance = (IFileDataReporter)Activator.CreateInstance(rep);

                    var processedExtension = SanitizeExtension(instance.Extension);

                    if (processedExtension != instance.Extension)
                    {
                        throw new ArgumentException($"Extension must be lowercase characters only ({rep})");
                    }
                    else
                    {
                        m_DataFileReporters.Add(instance);
                    }
                }
            }
        }

        void InitializeDataReporters()
        {
            m_DataReporters = new HashSet<IDataReporter>();

            foreach (var rep in TypeCache.GetTypesDerivedFrom(typeof(IDataReporter)))
            {
                if (rep.GetConstructors().Length > 0)
                {
                    var instance = (IDataReporter)Activator.CreateInstance(rep);

                    if (!(instance is FileReporter))
                    {
                        m_DataReporters.Add(instance);
                    }
                }
            }
        }

        public IList<T> GetAllReporters<T>() where T : IDataReporter
        {
            if (typeof(T) == typeof(IDataReporter))
            {
                return (IList<T>)m_DataReporters.ToList();
            }

            if (typeof(T) == typeof(IFileDataReporter))
            {
                return (IList<T>)m_DataFileReporters.ToList();
            }

            return default;
        }

        public T TryGetReporter<T>(Type type) where T : IDataReporter
        {
            if (typeof(T) == typeof(IDataReporter))
            {
                return (T)m_DataReporters.FirstOrDefault(x => x.GetType() == type);
            }

            if (typeof(T) == typeof(IFileDataReporter))
            {
                return (T)m_DataFileReporters.FirstOrDefault(x => x.GetType() == type);
            }

            return default;
        }

        public IDataReporter TryGetDataReporter(string typeName)
        {
            return m_DataReporters.FirstOrDefault(reporter => reporter.GetType().Name == typeName);
        }

        public IFileDataReporter TryGetDataFileReporter(string typeName)
        {
            return m_DataFileReporters.FirstOrDefault(reporter => reporter.GetType().Name == typeName);
        }

        public bool IsFileExtensionSupported(string extension)
        {
            return m_DataFileReporters.Any(reporter => reporter.Extension == SanitizeExtension(extension));
        }

        static string SanitizeExtension(string extension)
        {
            Assert.IsNotNull(extension, "Extension cannot be null");

            if (extension.StartsWith("."))
            {
                return extension.Substring(1);
            }

            extension = extension.ToLower();

            return extension;
        }
    }
}
