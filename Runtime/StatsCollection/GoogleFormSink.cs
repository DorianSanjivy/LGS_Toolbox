using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace LGSToolbox
{
    [CreateAssetMenu(menuName = "LGS Toolbox/Stats/Google Form Sink")]
    public class GoogleFormSink : ScriptableObject, IStatsSink
    {
        [Header("Google Form")]
        [Tooltip("Use the Google Form response URL. A /viewform URL will automatically be converted to /formResponse.")]
        [SerializeField] private string formUrl;

        [Header("Field Mapping")]
        [SerializeField] private List<FieldMap> fieldMapping = new List<FieldMap>();

        [Serializable]
        public struct FieldMap
        {
            [Tooltip("Google Form field entry name, for example: entry.123456789")]
            public string googleFormEntry;

            [Tooltip("StatsSession field/property/custom key, for example: sessionId, version, playerName, score, deaths")]
            public string statName;
        }

        public IEnumerator Send(StatsSession session, IEnumerable<string> statKeys = null)
        {
            if (session == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS GoogleFormSink] Cannot send a null StatsSession.");
#endif
                yield break;
            }

            string submitUrl = BuildSubmitUrl(formUrl);

            if (string.IsNullOrWhiteSpace(submitUrl))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS GoogleFormSink] Missing Google Form URL.");
#endif
                yield break;
            }

            HashSet<string> filter = statKeys != null
                ? new HashSet<string>(statKeys)
                : null;

            WWWForm form = new WWWForm();

            foreach (FieldMap map in fieldMapping)
            {
                if (string.IsNullOrWhiteSpace(map.googleFormEntry))
                    continue;

                if (string.IsNullOrWhiteSpace(map.statName))
                    continue;

                if (filter != null && !filter.Contains(map.statName))
                    continue;

                string value = GetValue(session, map.statName);
                form.AddField(map.googleFormEntry, value);
            }

            using (UnityWebRequest request = UnityWebRequest.Post(submitUrl, form))
            {
                yield return request.SendWebRequest();

#if UNITY_EDITOR
                if (request.result == UnityWebRequest.Result.Success)
                    Debug.Log("[LGS GoogleFormSink] Stats sent successfully.");
                else
                    Debug.LogWarning($"[LGS GoogleFormSink] Send failed: {request.error}");
#endif
            }
        }

        private string BuildSubmitUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";

            if (url.Contains("/viewform"))
                return url.Replace("/viewform", "/formResponse");

            return url;
        }

        private string GetValue(StatsSession session, string key)
        {
            if (session.custom.TryGetValue(key, out string customValue))
                return customValue ?? "";

            Type type = typeof(StatsSession);

            FieldInfo field = type.GetField(key, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                return ConvertToString(field.GetValue(session));

            PropertyInfo property = type.GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
                return ConvertToString(property.GetValue(session, null));

            return "";
        }

        private string ConvertToString(object value)
        {
            if (value == null)
                return "";

            if (value is DateTime dateTime)
                return dateTime.ToString("o", CultureInfo.InvariantCulture);

            if (value is bool boolValue)
                return boolValue ? "true" : "false";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString();
        }
    }
}