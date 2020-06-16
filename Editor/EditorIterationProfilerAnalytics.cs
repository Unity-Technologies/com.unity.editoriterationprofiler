using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.EditorIterationProfiler
{
    class EditorIterationProfilerAnalytics
    {
        const string k_VendorKey = "unity.editoriterationprofiler";

        static bool s_ExportEventRegistered;
        static bool s_InteractionEventRegistered;
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_ExportEventName = "eipExport";
        const string k_InteractionEventName = "eipInteraction";

        static bool EnableAnalytics()
        {
            AnalyticsResult resultExport = EditorAnalytics.RegisterEventWithLimit(k_ExportEventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
            AnalyticsResult resultInteraction = EditorAnalytics.RegisterEventWithLimit(k_InteractionEventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            if (resultExport == AnalyticsResult.Ok)
            {
                s_ExportEventRegistered = true;
            }

            if (resultExport == AnalyticsResult.Ok)
            {
                s_InteractionEventRegistered = true;
            }

            return s_ExportEventRegistered && s_InteractionEventRegistered;
        }

        internal enum ExportStatus
        {
            Started,
            Finished,
            Error
        }

        internal enum ExportType
        {
            Selected,
            MultiWindow,
            Multi,
            Captured,
            CapturedAll
        }

        struct ExportEventData
        {
            public string guid;
            public string format;
            public string type;
            public string status;
        }

        struct InteractionEventData
        {
            public bool eipState;
            public bool isPlaying;
            public bool deepProfile;
            public bool flatten;
            public bool userCode;
        }

        public static void SendExportEvent(string format, string type, string status, string guid)
        {
            if (!UnityEngine.Analytics.Analytics.enabled || !EnableAnalytics())
            {
                return;
            }

            var data = new ExportEventData()
            {
                format = format,
                type = type,
                status = status,
                guid = guid
            };

            EditorAnalytics.SendEventWithLimit(k_ExportEventName, data);
        }

        public static void SendInteractionEvent(bool state, bool isPlaying, bool deepProfile, bool flatten, bool userCode)
        {
            if (!UnityEngine.Analytics.Analytics.enabled || !EnableAnalytics())
            {
                return;
            }

            var data = new InteractionEventData()
            {
                eipState = state,
                isPlaying = isPlaying,
                deepProfile = deepProfile,
                flatten = flatten,
                userCode = userCode
            };

            EditorAnalytics.SendEventWithLimit(k_InteractionEventName, data);
        }

    }
}
