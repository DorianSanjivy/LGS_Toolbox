using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    public class NewgroundsAPI : MonoBehaviour
    {
        public static NewgroundsAPI Instance { get; private set; }

        [Header("Newgrounds App Settings")]
        [SerializeField] private string appId;
        [SerializeField] private string aesKey;
        [SerializeField] private string version = "1.0.0";

        [Header("Init Options")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool checkHostLicense = true;
        [SerializeField] private bool autoLogNewView = true;
        [SerializeField] private bool preloadMedals = true;
        [SerializeField] private bool preloadScoreBoards = true;
        [SerializeField] private bool preloadSaveSlots = true;

        [Header("Polling")]
        [SerializeField] private float connectionStatusRefreshRate = 0.5f;
        [SerializeField] private float keepSessionAliveRate = 30f;

        public bool IsInitialized { get; private set; }
        public bool IsReady { get; private set; }
        public string CurrentStatus { get; private set; }

        public event Action<string> OnStatusChanged;
        public event Action OnReady;
        public event Action<NewgroundsIO.objects.Medal> OnMedalUnlocked;
        public event Action<NewgroundsIO.objects.ScoreBoard, NewgroundsIO.objects.Score> OnScorePosted;

        private Coroutine connectionStatusRoutine;
        private Coroutine keepSessionAliveRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (initializeOnStart)
                Initialize();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            StopPolling();
            Instance = null;
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(aesKey))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS NewgroundsAPI] Missing App ID or AES key.");
#endif
                return;
            }

            Dictionary<string, object> options = new()
            {
                { "version", version },
                { "checkHostLicense", checkHostLicense },
                { "autoLogNewView", autoLogNewView },
                { "preloadMedals", preloadMedals },
                { "preloadScoreBoards", preloadScoreBoards },
                { "preloadSaveSlots", preloadSaveSlots }
            };

            NGIO.Init(appId, aesKey, options);

            IsInitialized = true;
            StartPolling();
        }

        private void StartPolling()
        {
            StopPolling();

            connectionStatusRoutine = StartCoroutine(ConnectionStatusLoop());
            keepSessionAliveRoutine = StartCoroutine(KeepSessionAliveLoop());
        }

        private void StopPolling()
        {
            if (connectionStatusRoutine != null)
            {
                StopCoroutine(connectionStatusRoutine);
                connectionStatusRoutine = null;
            }

            if (keepSessionAliveRoutine != null)
            {
                StopCoroutine(keepSessionAliveRoutine);
                keepSessionAliveRoutine = null;
            }
        }

        private IEnumerator ConnectionStatusLoop()
        {
            WaitForSeconds wait = new(connectionStatusRefreshRate);

            while (true)
            {
                yield return NGIO.GetConnectionStatus(HandleConnectionStatus);
                yield return wait;
            }
        }

        private IEnumerator KeepSessionAliveLoop()
        {
            WaitForSeconds wait = new(keepSessionAliveRate);

            while (true)
            {
                yield return NGIO.KeepSessionAlive();
                yield return wait;
            }
        }

        private void HandleConnectionStatus(string status)
        {
            if (CurrentStatus == status)
                return;

            CurrentStatus = status;
            OnStatusChanged?.Invoke(status);

            switch (status)
            {
                case NGIO.STATUS_CHECKING_LOCAL_VERSION:
#if UNITY_EDITOR
                    Debug.Log("[LGS NewgroundsAPI] Checking local version...");
#endif
                    break;

                case NGIO.STATUS_PRELOADING_ITEMS:
#if UNITY_EDITOR
                    Debug.Log("[LGS NewgroundsAPI] Preloading medals, scoreboards and save slots...");
#endif
                    break;

                case NGIO.STATUS_LOGIN_REQUIRED:
#if UNITY_EDITOR
                    Debug.Log("[LGS NewgroundsAPI] Login required.");
#endif
                    IsReady = false;
                    break;

                case NGIO.STATUS_READY:
                    IsReady = true;
                    OnReady?.Invoke();

#if UNITY_EDITOR
                    if (NGIO.hasUser)
                        Debug.Log($"[LGS NewgroundsAPI] Ready. Logged in as {NGIO.user.name}.");
                    else
                        Debug.Log("[LGS NewgroundsAPI] Ready. No user logged in.");
#endif
                    break;
            }
        }

        public static void OpenLoginPage()
        {
            NGIO.OpenLoginPage();
        }

        public static void SkipLogin()
        {
            NGIO.SkipLogin();
        }

        public static bool HasUser()
        {
            return NGIO.hasUser;
        }

        public static string GetUserName()
        {
            return NGIO.hasUser ? NGIO.user.name : string.Empty;
        }

        public void PostScore(int boardId, int score, string tag = null)
        {
            if (!IsReady)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS NewgroundsAPI] Cannot post score because API is not ready.");
#endif
                return;
            }

            StartCoroutine(NGIO.PostScore(boardId, score, tag, HandleScorePosted));
        }

        public void UnlockMedal(int medalId)
        {
            if (!IsReady)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LGS NewgroundsAPI] Cannot unlock medal because API is not ready.");
#endif
                return;
            }

            NewgroundsIO.objects.Medal medal = NGIO.GetMedal(medalId);

            if (medal == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS NewgroundsAPI] Medal not found: {medalId}");
#endif
                return;
            }

            if (medal.unlocked)
            {
#if UNITY_EDITOR
                Debug.Log($"[LGS NewgroundsAPI] Medal already unlocked: {medal.name}");
#endif
                return;
            }

            StartCoroutine(NGIO.UnlockMedal(medalId, HandleMedalUnlocked));
        }

        private void HandleMedalUnlocked(NewgroundsIO.objects.Medal medal)
        {
#if UNITY_EDITOR
            Debug.Log($"[LGS NewgroundsAPI] Medal unlocked: {medal.name}");
#endif
            OnMedalUnlocked?.Invoke(medal);
        }

        private void HandleScorePosted(NewgroundsIO.objects.ScoreBoard board, NewgroundsIO.objects.Score score)
        {
#if UNITY_EDITOR
            Debug.Log($"[LGS NewgroundsAPI] Score posted to board: {board.name}");
#endif
            OnScorePosted?.Invoke(board, score);
        }
    }
}