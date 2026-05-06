#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    public class EditorFullscreenController : MonoBehaviour
    {
        public static EditorFullscreenController Instance { get; private set; }

        [Header("Fullscreen")]
        [SerializeField] private bool fullscreenOnStart = false;
        [SerializeField] private bool allowToggle = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.Period;

        [Header("Persistence")]
        [SerializeField] private bool persistAcrossScenes = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (fullscreenOnStart)
                FullscreenGameView.Open();
        }

        private void Update()
        {
            if (!allowToggle)
                return;

            if (Input.GetKeyDown(toggleKey))
                FullscreenGameView.Toggle();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    public static class FullscreenGameView
    {
        private static readonly Type GameViewType =
            Type.GetType("UnityEditor.GameView,UnityEditor");

        private static readonly PropertyInfo ShowToolbarProperty =
            GameViewType?.GetProperty(
                "showToolbar",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private static readonly object False = false;

        private static EditorWindow instance;

        public static bool IsOpen => instance != null;

        static FullscreenGameView()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                Close();
        }

        [MenuItem("Window/General/Game View Fullscreen %#&2", priority = 2)]
        public static void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public static void Open()
        {
            if (GameViewType == null)
            {
                Debug.LogError("[LGS FullscreenGameView] UnityEditor.GameView type not found.");
                return;
            }

            if (instance != null)
                return;

            instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            ShowToolbarProperty?.SetValue(instance, False);

            Vector2 desktopResolution = new Vector2(
                Screen.currentResolution.width,
                Screen.currentResolution.height
            );

            Rect fullscreenRect = new Rect(Vector2.zero, desktopResolution);

            instance.ShowPopup();
            instance.position = fullscreenRect;
            instance.Focus();

            Debug.Log("[LGS FullscreenGameView] Fullscreen enabled.");
        }

        public static void Close()
        {
            if (instance == null)
                return;

            instance.Close();
            instance = null;

            Debug.Log("[LGS FullscreenGameView] Fullscreen disabled.");
        }
    }
}

#endif