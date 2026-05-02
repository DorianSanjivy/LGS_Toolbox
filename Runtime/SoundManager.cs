using System.Collections.Generic;
using UnityEngine;

namespace LGSToolbox
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Tooltip("Minimum delay between two plays of the same sound name.")]
        [SerializeField] private float duplicateBlockWindow = 0.10f;

        [Tooltip("If true, use unscaled time. Useful if the game is paused or Time.timeScale is modified.")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Registered sounds by name")]
        [SerializeField] private SoundItem[] sounds;

        private readonly Dictionary<string, float> lastPlayedAt = new();
        private Dictionary<string, SoundItem> soundsByName;

        private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildSoundLookup();
        }

        private void BuildSoundLookup()
        {
            soundsByName = new Dictionary<string, SoundItem>();

            if (sounds == null)
                return;

            foreach (SoundItem soundItem in sounds)
            {
                if (soundItem == null)
                    continue;

                if (string.IsNullOrWhiteSpace(soundItem.Name))
                    continue;

                if (soundsByName.ContainsKey(soundItem.Name))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[LGS SoundManager] Duplicate sound name ignored: '{soundItem.Name}'.");
#endif
                    continue;
                }

                soundsByName.Add(soundItem.Name, soundItem);
            }
        }

        public static void Play(string soundName, float volume = 1f, float pitch = 1f)
        {
            Instance?.PlaySound(soundName, volume, pitch);
        }

        public static void Stop(string soundName)
        {
            Instance?.StopSound(soundName);
        }

        public void PlaySound(string soundName, float volume = 1f, float pitch = 1f)
        {
            if (!TryGetSound(soundName, out SoundItem soundItem))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS SoundManager] Sound not found: '{soundName}'.");
#endif
                return;
            }

            if (!CanPlay(soundName))
                return;

            soundItem.Play(volume, pitch);
        }

        public void StopSound(string soundName)
        {
            if (!TryGetSound(soundName, out SoundItem soundItem))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS SoundManager] Sound not found: '{soundName}'.");
#endif
                return;
            }

            soundItem.Stop();
        }

        private bool TryGetSound(string soundName, out SoundItem soundItem)
        {
            soundItem = null;

            if (string.IsNullOrWhiteSpace(soundName))
                return false;

            if (soundsByName == null)
                BuildSoundLookup();

            return soundsByName != null && soundsByName.TryGetValue(soundName, out soundItem);
        }

        private bool CanPlay(string soundName)
        {
            if (lastPlayedAt.TryGetValue(soundName, out float lastPlayTime))
            {
                float timeSinceLastPlay = CurrentTime - lastPlayTime;

                if (timeSinceLastPlay < duplicateBlockWindow)
                    return false;
            }

            lastPlayedAt[soundName] = CurrentTime;
            return true;
        }
    }

    [System.Serializable]
    public class SoundItem
    {
        [SerializeField] private string name;
        [SerializeField] private AudioSource[] audios;

        private int roundRobinIndex;

        public string Name => name;

        public void Play(float volume = 1f, float pitch = 1f)
        {
            if (audios == null || audios.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS SoundItem] '{name}' has no AudioSources.");
#endif
                return;
            }

            AudioSource source = audios[roundRobinIndex];

            if (source == null || source.clip == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS SoundItem] '{name}' AudioSource is missing or has no AudioClip.");
#endif
                MoveToNextAudioSource();
                return;
            }

            source.pitch = pitch;
            source.PlayOneShot(source.clip, volume);

            MoveToNextAudioSource();
        }

        public void Stop()
        {
            if (audios == null)
                return;

            foreach (AudioSource source in audios)
            {
                if (source != null)
                    source.Stop();
            }
        }

        private void MoveToNextAudioSource()
        {
            roundRobinIndex++;

            if (roundRobinIndex >= audios.Length)
                roundRobinIndex = 0;
        }
    }
}