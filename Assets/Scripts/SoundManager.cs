using UnityEngine;

namespace MasterPuzzleWall
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Clips")]
        public AudioClip sfxClick;
        public AudioClip sfxSnap;
        public AudioClip sfxChime;
        public AudioClip sfxFanfare;
        public AudioClip sfxError;
        public AudioClip sfxRatchet;
        public AudioClip sfxMagicHum;
        public AudioClip sfxIceSlide;
        public AudioClip sfxAmbience;

        [Header("Audio Sources")]
        public AudioSource sfxSource;
        public AudioSource sfxLoopSource;
        public AudioSource bgmSource;

        private bool isMuted = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (sfxLoopSource == null) sfxLoopSource = gameObject.AddComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxLoopSource.playOnAwake = false;
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.35f;

            if (sfxAmbience != null)
            {
                bgmSource.clip = sfxAmbience;
                bgmSource.Play();
            }
        }

        public void PlaySound(AudioClip clip, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
        {
            if (isMuted || clip == null || sfxSource == null) return;
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayClick() => PlaySound(sfxClick, 0.9f, 0.95f, 1.08f);
        public void PlaySnap() => PlaySound(sfxSnap, 1f, 0.92f, 1.05f);
        public void PlayChime() => PlaySound(sfxChime, 1f, 0.98f, 1.02f);
        public void PlayFanfare() => PlaySound(sfxFanfare, 1f, 1.0f, 1.0f);
        public void PlayError() => PlaySound(sfxError, 0.85f, 0.9f, 1.0f);
        public void PlayRatchet() => PlaySound(sfxRatchet, 0.8f, 0.96f, 1.04f);
        public void PlayMagicHum() => PlaySound(sfxMagicHum, 0.85f, 0.98f, 1.02f);
        public void PlayIceSlide() => PlaySound(sfxIceSlide, 0.75f, 0.95f, 1.05f);

        public void ToggleMute()
        {
            isMuted = !isMuted;
            if (bgmSource != null)
            {
                bgmSource.mute = isMuted;
            }
        }

        public bool IsMuted => isMuted;
    }
}
