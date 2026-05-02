using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        // Optional alias if you like short access: StatsManager.I
        public static StatsManager I
        {
            get { return Instance; }
        }

        [Header("Sink")]
        [Tooltip("Assign a ScriptableObject that implements IStatsSink, for example GoogleFormSink.")]
        [SerializeField] private ScriptableObject sinkAsset;

        [Header("Session")]
        [SerializeField] private string playerName = "None";

        [Header("Public IP")]
        [Tooltip("Disabled by default because public IP is personal data. Enable only if your project needs it.")]
        [SerializeField] private bool fetchPublicIPOnStart = false;

        private StatsCollector collector;
        private IStatsSink sink;

        public StatsSession Session
        {
            get { return collector != null ? collector.Session : null; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            collector = new StatsCollector();

            collector.Session.version = Application.version;
            collector.Session.isDev = Application.isEditor;
            collector.Session.playerName = playerName;

            SetSink(sinkAsset);

            if (fetchPublicIPOnStart)
                StartCoroutine(FetchPublicIP());
        }

        public void SetSink(ScriptableObject newSinkAsset)
        {
            sinkAsset = newSinkAsset;
            sink = sinkAsset as IStatsSink;

#if UNITY_EDITOR
            if (sinkAsset != null && sink == null)
                Debug.LogWarning("[LGS StatsManager] Assigned sink asset does not implement IStatsSink.");
#endif
        }

        public void SetPlayerName(string newPlayerName)
        {
            playerName = string.IsNullOrWhiteSpace(newPlayerName) ? "None" : newPlayerName;

            if (collector != null)
                collector.Session.playerName = playerName;
        }

        public void SetCustom(string key, string value)
        {
            collector.SetCustom(key, value);
        }

        public void SetInt(string key, int value)
        {
            collector.SetInt(key, value);
        }

        public void SetFloat(string key, float value)
        {
            collector.SetFloat(key, value);
        }

        public void SetBool(string key, bool value)
        {
            collector.SetBool(key, value);
        }

        public void IncrementInt(string key, int amount = 1)
        {
            collector.IncrementInt(key, amount);
        }

        public void ClearCustom()
        {
            collector.ClearCustom();
        }

        public void SendAll()
        {
            if (!CanSend())
                return;

            StartCoroutine(sink.Send(collector.Session));
        }

        public void SendOnly(params string[] keys)
        {
            if (!CanSend())
                return;

            StartCoroutine(sink.Send(collector.Session, keys));
        }

        public Coroutine SendAllCoroutine()
        {
            if (!CanSend())
                return null;

            return StartCoroutine(sink.Send(collector.Session));
        }

        public Coroutine SendOnlyCoroutine(params string[] keys)
        {
            if (!CanSend())
                return null;

            return StartCoroutine(sink.Send(collector.Session, keys));
        }

        private bool CanSend()
        {
            if (collector == null || collector.Session == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS StatsManager] No active stats session.");
#endif
                return false;
            }

            if (sink == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS StatsManager] No valid stats sink assigned.");
#endif
                return false;
            }

            return true;
        }

        public void FetchPublicIPNow()
        {
            StartCoroutine(FetchPublicIP());
        }

        private IEnumerator FetchPublicIP()
        {
            using (UnityWebRequest request = UnityWebRequest.Get("https://api64.ipify.org?format=text"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    collector.Session.publicIP = request.downloadHandler.text;
#if UNITY_EDITOR
                else
                    Debug.LogWarning($"[LGS StatsManager] Could not fetch public IP: {request.error}");
#endif
            }
        }
    }
}