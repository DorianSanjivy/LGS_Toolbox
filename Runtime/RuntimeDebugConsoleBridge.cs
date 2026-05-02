using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using IngameDebugConsole;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    public class RuntimeDebugConsoleBridge : MonoBehaviour
    {
        public static RuntimeDebugConsoleBridge Instance { get; private set; }

        private static bool commandsRegistered;
        private static readonly SortedDictionary<string, string> customHelpEntries = new();

        [Header("Console Toggle")]
        [SerializeField] private bool debugEnabled = true;
        [SerializeField] private KeyCode toggleConsoleKey = KeyCode.LeftAlt;
        [SerializeField] private bool hideConsoleOnStart = true;

        [Header("Logs")]
        [SerializeField] private bool hideLogStackTraces = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (hideLogStackTraces)
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            if (!commandsRegistered)
                RegisterDefaultCommands();
        }

        private void Start()
        {
            if (hideConsoleOnStart)
                HideConsoleCompletely();
        }

        private void Update()
        {
            if (!debugEnabled)
                return;

            if (Input.GetKeyDown(toggleConsoleKey))
                ToggleConsoleCompletely();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            UnregisterDefaultCommands();

            Instance = null;
            commandsRegistered = false;
        }

        public void SetDebugEnabled(bool enabled)
        {
            debugEnabled = enabled;

            if (!debugEnabled)
                HideConsoleCompletely();
        }

        public static void RegisterHelpEntry(string command, string description)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            customHelpEntries[command] = description;
        }

        public static void UnregisterHelpEntry(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            customHelpEntries.Remove(command);
        }

        public static void Show()
        {
            DebugLogManager console = DebugLogManager.Instance;

            if (console == null)
                return;

            console.ShowLogWindow();
        }

        public static void Hide()
        {
            DebugLogManager console = DebugLogManager.Instance;

            if (console == null)
                return;

            console.PopupEnabled = false;
            console.HideLogWindow();
        }

        public static void Toggle()
        {
            DebugLogManager console = DebugLogManager.Instance;

            if (console == null)
                return;

            if (console.IsLogWindowVisible)
            {
                console.PopupEnabled = false;
                console.HideLogWindow();
            }
            else
            {
                console.ShowLogWindow();
            }
        }

        private void HideConsoleCompletely()
        {
            Hide();
        }

        private void ToggleConsoleCompletely()
        {
            Toggle();
        }

        private void RegisterDefaultCommands()
        {
            commandsRegistered = true;

            DebugLogConsole.AddCommand("help", "Show toolbox debug commands", Help);
            DebugLogConsole.AddCommand("scene", "Show the active scene name", GetActiveSceneName);
            DebugLogConsole.AddCommand("reload", "Reload the active scene", ReloadScene);
            DebugLogConsole.AddCommand<float>("timescale", "Set Time.timeScale", SetTimeScale);
            DebugLogConsole.AddCommand("logpath", "Show current log file path", GetLogPath);
            DebugLogConsole.AddCommand("show", "Show the debug console", ShowConsoleCommand);
            DebugLogConsole.AddCommand("hide", "Hide the debug console", HideConsoleCommand);
        }

        private void UnregisterDefaultCommands()
        {
            string[] commands =
            {
                "help",
                "scene",
                "reload",
                "timescale",
                "logpath",
                "show",
                "hide"
            };

            foreach (string command in commands)
                DebugLogConsole.RemoveCommand(command);
        }

        private string Help()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("=== TOOLBOX COMMANDS ===");
            builder.AppendLine("help               -> Show this help");
            builder.AppendLine("scene              -> Show the active scene name");
            builder.AppendLine("reload             -> Reload the active scene");
            builder.AppendLine("timescale [Float]  -> Set Time.timeScale");
            builder.AppendLine("logpath            -> Show current log file path");
            builder.AppendLine("show               -> Show the debug console");
            builder.AppendLine("hide               -> Hide the debug console");

            if (customHelpEntries.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("=== PROJECT COMMANDS ===");

                foreach (KeyValuePair<string, string> entry in customHelpEntries)
                {
                    builder.AppendLine($"{entry.Key,-18} -> {entry.Value}");
                }
            }

            return builder.ToString();
        }

        private string GetActiveSceneName()
        {
            return $"Active scene: {SceneManager.GetActiveScene().name}";
        }

        private string ReloadScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);

            return $"Reloading scene: {activeScene.name}";
        }

        private void SetTimeScale(float value)
        {
            Time.timeScale = Mathf.Max(0f, value);
            Debug.Log($"Time.timeScale = {Time.timeScale}");
        }

        private string GetLogPath()
        {
            string path = LogManager.GetCurrentLogPath();

            if (string.IsNullOrWhiteSpace(path))
                return "No LogManager active";

            return path;
        }

        private string ShowConsoleCommand()
        {
            Show();
            return "Console shown";
        }

        private string HideConsoleCommand()
        {
            Hide();
            return "Console hidden";
        }
    }
}