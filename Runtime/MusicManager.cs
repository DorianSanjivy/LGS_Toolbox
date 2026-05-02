using System.Collections;
using UnityEngine;

namespace LGSToolbox
{
    [DisallowMultipleComponent]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Volume")]
        [SerializeField] private float targetVolume = 0.08f;

        [Header("Fade")]
        [SerializeField] private float defaultFadeDuration = 1f;
        [SerializeField] private bool useUnscaledTime = true;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private Coroutine transitionRoutine;

        private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateAudioSources();
        }

        private void CreateAudioSources()
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();

            sourceA = existingSources.Length > 0
                ? existingSources[0]
                : gameObject.AddComponent<AudioSource>();

            sourceB = existingSources.Length > 1
                ? existingSources[1]
                : gameObject.AddComponent<AudioSource>();

            ConfigureSource(sourceA);
            ConfigureSource(sourceB);

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        private void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
        }

        private static MusicManager GetOrCreateInstance()
        {
            if (Instance != null)
                return Instance;

            GameObject managerObject = new GameObject("[LGS] Music Manager");
            return managerObject.AddComponent<MusicManager>();
        }

        public static void PlayFromResources(
            string resourcesPath,
            bool transition = true,
            float fadeDuration = -1f,
            bool preserveTime = false
        )
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                return;

            AudioClip clip = Resources.Load<AudioClip>(resourcesPath);

            if (clip == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LGS MusicManager] Music not found in Resources: '{resourcesPath}'.");
#endif
                return;
            }

            GetOrCreateInstance().PlayMusic(clip, transition, fadeDuration, preserveTime);
        }

        public static void Play(
            AudioClip clip,
            bool transition = true,
            float fadeDuration = -1f,
            bool preserveTime = false
        )
        {
            GetOrCreateInstance().PlayMusic(clip, transition, fadeDuration, preserveTime);
        }

        public static void Stop(bool fade = true, float fadeDuration = -1f)
        {
            if (Instance == null)
                return;

            Instance.StopMusic(fade, fadeDuration);
        }

        public static void SetVolume(float volume)
        {
            GetOrCreateInstance().SetTargetVolume(volume);
        }

        public void PlayMusic(
            AudioClip clip,
            bool transition = true,
            float fadeDuration = -1f,
            bool preserveTime = false
        )
        {
            if (clip == null)
                return;

            float duration = fadeDuration >= 0f ? fadeDuration : defaultFadeDuration;

            if (activeSource.clip == clip)
            {
                if (!activeSource.isPlaying)
                    activeSource.Play();

                activeSource.volume = targetVolume;
                return;
            }

            StopCurrentTransition();

            if (!transition || duration <= 0f || activeSource.clip == null || !activeSource.isPlaying)
            {
                StartImmediate(clip, preserveTime);
                return;
            }

            transitionRoutine = StartCoroutine(CrossfadeTo(clip, duration, preserveTime));
        }

        private void StartImmediate(AudioClip clip, bool preserveTime)
        {
            float startTime = GetPreservedTime(clip, preserveTime);

            inactiveSource.Stop();
            inactiveSource.clip = null;
            inactiveSource.volume = 0f;

            activeSource.Stop();
            activeSource.clip = clip;
            activeSource.loop = true;
            activeSource.volume = targetVolume;
            activeSource.time = startTime;
            activeSource.Play();
        }

        private IEnumerator CrossfadeTo(AudioClip newClip, float duration, bool preserveTime)
        {
            float startTime = GetPreservedTime(newClip, preserveTime);

            inactiveSource.Stop();
            inactiveSource.clip = newClip;
            inactiveSource.loop = true;
            inactiveSource.volume = 0f;
            inactiveSource.time = startTime;
            inactiveSource.Play();

            float startVolume = activeSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
                inactiveSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            activeSource.volume = 0f;
            activeSource.Stop();
            activeSource.clip = null;

            inactiveSource.volume = targetVolume;

            SwapSources();

            transitionRoutine = null;
        }

        public void StopMusic(bool fade = true, float fadeDuration = -1f)
        {
            float duration = fadeDuration >= 0f ? fadeDuration : defaultFadeDuration;

            StopCurrentTransition();

            if (!fade || duration <= 0f)
            {
                StopImmediate();
                return;
            }

            transitionRoutine = StartCoroutine(FadeOut(duration));
        }

        private IEnumerator FadeOut(float duration)
        {
            float startVolumeA = sourceA.volume;
            float startVolumeB = sourceB.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += DeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                sourceA.volume = Mathf.Lerp(startVolumeA, 0f, t);
                sourceB.volume = Mathf.Lerp(startVolumeB, 0f, t);

                yield return null;
            }

            StopImmediate();
            transitionRoutine = null;
        }

        private void StopImmediate()
        {
            sourceA.Stop();
            sourceB.Stop();

            sourceA.clip = null;
            sourceB.clip = null;

            sourceA.volume = 0f;
            sourceB.volume = 0f;

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        private float GetPreservedTime(AudioClip newClip, bool preserveTime)
        {
            if (!preserveTime)
                return 0f;

            if (activeSource == null || activeSource.clip == null || newClip == null)
                return 0f;

            if (newClip.length <= 0f)
                return 0f;

            return Mathf.Clamp(
                activeSource.time % newClip.length,
                0f,
                Mathf.Max(0f, newClip.length - 0.01f)
            );
        }

        private void SetTargetVolume(float volume)
        {
            targetVolume = Mathf.Clamp01(volume);

            if (activeSource != null && activeSource.isPlaying)
                activeSource.volume = targetVolume;
        }

        private void SwapSources()
        {
            AudioSource oldActive = activeSource;
            activeSource = inactiveSource;
            inactiveSource = oldActive;
        }

        private void StopCurrentTransition()
        {
            if (transitionRoutine == null)
                return;

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }
}