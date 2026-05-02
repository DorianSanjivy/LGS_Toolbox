using System;
using System.Collections.Generic;
using UnityEngine;

namespace LGSToolbox
{
    [Serializable]
    public class StatsSession
    {
        public string sessionId = Guid.NewGuid().ToString();
        public string playerName = "None";
        public string version = "";
        public bool isDev;
        public string publicIP = "";
        public DateTime startUtc = DateTime.UtcNow;

        public Dictionary<string, string> custom = new Dictionary<string, string>();

        public float ElapsedSeconds
        {
            get { return (float)(DateTime.UtcNow - startUtc).TotalSeconds; }
        }

        public void SetCustom(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            custom[key] = value ?? "";
        }

        public string GetCustom(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback;

            return custom.TryGetValue(key, out string value) ? value : fallback;
        }

        public void ClearCustom()
        {
            custom.Clear();
        }
    }
}