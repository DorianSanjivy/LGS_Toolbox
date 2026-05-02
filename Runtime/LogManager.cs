using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public class LogManager : MonoBehaviour
    {
        public static LogManager Instance { get; private set; }

        [Header("File Logging")]
        [SerializeField] private bool logWarnings = false;
        [SerializeField] private bool includeStackTracesForAll = false;
        [SerializeField] private int keepLastSessions = 10;

        [Header("Log Location")]
        [SerializeField] private string logFolderName = "Logs";

        private string logDirectory;
        private string logPath;
        private StreamWriter writer;

        private readonly object fileLock = new();

        public string LogPath => logPath;
        public string LogDirectory => logDirectory;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogWarning("[LGS LogManager] File logging is not supported in WebGL builds.");
            Destroy(gameObject);
            return;
#endif

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StartLogSession();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            UnregisterCallbacks();
            CloseWriter();

            Instance = null;
        }

        private void StartLogSession()
        {
            logDirectory = GetLogDirectory();
            Directory.CreateDirectory(logDirectory);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string gameName = SanitizeFileName(Application.productName);
            string version = SanitizeFileName(Application.version);

            logPath = Path.Combine(logDirectory, $"{gameName}_v{version}_{timestamp}.log");

            writer = new StreamWriter(logPath, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            RotateOldFiles(gameName);

            WriteSessionHeader();

            RegisterCallbacks();

            Debug.Log($"[LGS LogManager] Writing logs to: {logPath}");
        }

        private void RegisterCallbacks()
        {
            Application.logMessageReceivedThreaded += HandleUnityLog;
            Application.quitting += OnApplicationQuitting;
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            SceneManager.activeSceneChanged += HandleSceneChanged;
        }

        private void UnregisterCallbacks()
        {
            Application.logMessageReceivedThreaded -= HandleUnityLog;
            Application.quitting -= OnApplicationQuitting;
            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            SceneManager.activeSceneChanged -= HandleSceneChanged;
        }

        private void WriteSessionHeader()
        {
            WriteLine("=== LOG SESSION START ===");
            WriteLine($"Game: {Application.productName}");
            WriteLine($"Company: {Application.companyName}");
            WriteLine($"Version: {Application.version}");
            WriteLine($"Unity: {Application.unityVersion}");
            WriteLine($"Platform: {Application.platform}");
            WriteLine($"ActiveScene: {SceneManager.GetActiveScene().name}");
            WriteLine($"persistentDataPath: {Application.persistentDataPath}");
            WriteLine($"logDirectory: {logDirectory}");
            WriteLine($"logPath: {logPath}");
            WriteLine("");
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && !logWarnings)
                return;

            string time = DateTime.Now.ToString("HH:mm:ss");
            WriteLine($"[{time}] [{type}] {condition}");

            bool shouldWriteStackTrace =
                includeStackTracesForAll ||
                type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert;

            if (shouldWriteStackTrace && !string.IsNullOrWhiteSpace(stackTrace))
                WriteLine(stackTrace);

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                FlushWriter();
        }

        private void HandleSceneChanged(Scene oldScene, Scene newScene)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            WriteLine("");
            WriteLine("============================================================");
            WriteLine($"SCENE CHANGE @ {time}");
            WriteLine($"{oldScene.name} -> {newScene.name}");
            WriteLine("============================================================");
            WriteLine("");
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            try
            {
                WriteLine("[FATAL] Unhandled exception");
                WriteLine(eventArgs.ExceptionObject?.ToString() ?? "(null)");
                FlushWriter();
            }
            catch
            {
                // Ignored on purpose: logging must never crash the game.
            }
        }

        private void OnApplicationQuitting()
        {
            WriteLine("");
            WriteLine("=== LOG SESSION END ===");
            CloseWriter();
        }

        private void WriteLine(string line)
        {
            if (writer == null)
                return;

            lock (fileLock)
            {
                try
                {
                    writer.WriteLine(line);
                }
                catch
                {
                    // Ignored on purpose.
                }
            }
        }

        private void FlushWriter()
        {
            lock (fileLock)
            {
                try
                {
                    writer?.Flush();
                }
                catch
                {
                    // Ignored on purpose.
                }
            }
        }

        private void CloseWriter()
        {
            lock (fileLock)
            {
                try
                {
                    writer?.Flush();
                    writer?.Close();
                }
                catch
                {
                    // Ignored on purpose.
                }

                writer = null;
            }
        }

        private void RotateOldFiles(string sanitizedGameName)
        {
            if (keepLastSessions <= 0)
                return;

            try
            {
                DirectoryInfo directoryInfo = new(logDirectory);
                FileInfo[] files = directoryInfo.GetFiles($"{sanitizedGameName}_v*.log");

                Array.Sort(files, (a, b) => b.CreationTimeUtc.CompareTo(a.CreationTimeUtc));

                for (int i = keepLastSessions; i < files.Length; i++)
                {
                    try
                    {
                        files[i].Delete();
                    }
                    catch
                    {
                        // Ignored on purpose.
                    }
                }
            }
            catch
            {
                // Ignored on purpose.
            }
        }

        private string GetLogDirectory()
        {
            string preferredDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", logFolderName)
            );

            if (TryEnsureDirectoryWritable(preferredDirectory))
                return preferredDirectory;

            string fallbackDirectory = Path.Combine(Application.persistentDataPath, logFolderName);
            TryEnsureDirectoryWritable(fallbackDirectory);

            return fallbackDirectory;
        }

        private bool TryEnsureDirectoryWritable(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);

                string testFilePath = Path.Combine(directory, ".write_test");
                File.WriteAllText(testFilePath, "ok");
                File.Delete(testFilePath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName;
        }

        public static string GetCurrentLogPath()
        {
            return Instance != null ? Instance.LogPath : string.Empty;
        }

        public static string GetCurrentLogDirectory()
        {
            return Instance != null ? Instance.LogDirectory : string.Empty;
        }
    }
}