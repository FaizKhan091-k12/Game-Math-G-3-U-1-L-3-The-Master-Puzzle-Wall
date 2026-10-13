using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class TwinProngTask : MonoBehaviour
    {
        [Header("Socket Target")]
        public RectTransform socketAnchor;
        public Image socketBlinkGlow;
        public ParticleSystem electricSparkParticle;
        public ParticleSystem confettiParticles;

        [Header("Plug Tokens")]
        public RectTransform token17Rect;
        public RectTransform token24Rect;
        public Button token17Button;
        public Button token24Button;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        private Vector2 defaultPos17 = new Vector2(-160, -90);
        private Vector2 defaultPos24 = new Vector2(160, -90);
        private bool positionsCached = false;

        private void Awake()
        {
            CachePositions();
        }

        private void CachePositions()
        {
            if (!positionsCached)
            {
                if (token17Rect != null) defaultPos17 = token17Rect.anchoredPosition;
                if (token24Rect != null) defaultPos24 = token24Rect.anchoredPosition;
                positionsCached = true;
            }
        }

        public void InitializeTask()
        {
            CachePositions();
            isCompleted = false;

            if (confettiParticles != null) confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (electricSparkParticle != null) electricSparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (token17Rect != null)
            {
                token17Rect.DOKill();
                token17Rect.anchoredPosition = defaultPos17;
                token17Rect.localScale = Vector3.one;
                var cg = token17Rect.GetComponent<CanvasGroup>();
                if (cg != null) { cg.DOKill(); cg.alpha = 1f; }
            }

            if (token24Rect != null)
            {
                token24Rect.DOKill();
                token24Rect.anchoredPosition = defaultPos24;
                token24Rect.localScale = Vector3.one;
                var cg = token24Rect.GetComponent<CanvasGroup>();
                if (cg != null) { cg.DOKill(); cg.alpha = 1f; }
            }

            if (token17Button != null)
            {
                token17Button.interactable = true;
                token17Button.onClick.RemoveAllListeners();
                token17Button.onClick.AddListener(OnToken17Clicked);
            }

            if (token24Button != null)
            {
                token24Button.interactable = true;
                token24Button.onClick.RemoveAllListeners();
                token24Button.onClick.AddListener(OnToken24Clicked);
            }

            // Blinking socket effect
            if (socketBlinkGlow != null)
            {
                socketBlinkGlow.DOKill();
                socketBlinkGlow.gameObject.SetActive(true);
                socketBlinkGlow.color = new Color(0.08f, 0.12f, 0.20f, 0.98f);
                socketBlinkGlow.DOFade(0.85f, 0.6f).SetLoops(-1, LoopType.Yoyo);
            }

            // Idle gentle float for both tokens
            if (token17Rect != null)
            {
                token17Rect.DOAnchorPosY(defaultPos17.y + 10f, 1.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
            if (token24Rect != null)
            {
                token24Rect.DOAnchorPosY(defaultPos24.y - 10f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnToken17Clicked()
        {
            if (isCompleted) return;

            if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
            if (token17Rect != null)
            {
                token17Rect.DOPunchPosition(new Vector3(15f, 0, 0), 0.5f, 6, 0.4f);
            }

            GameManager.Instance?.GolemBot.SetConfused(true);
            GameManager.Instance?.GolemBot.Speak("17 ends in 7! That's an ODD number with 1 left over. Look for the EVEN number ending in 0, 2, 4, 6, or 8!", 3.5f, () =>
            {
                GameManager.Instance?.GolemBot.SetConfused(false);
            });
        }

        private void OnToken24Clicked()
        {
            if (isCompleted) return;
            isCompleted = true;

            token17Button.interactable = false;
            token24Button.interactable = false;

            token17Rect.DOKill();
            token24Rect.DOKill();
            if (socketBlinkGlow != null) socketBlinkGlow.DOKill();

            if (token17Rect != null)
            {
                token17Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.5f);
            }

            // Animate 24 sliding into the twin-slot socket
            if (token24Rect != null && socketAnchor != null)
            {
                token24Rect.DOMove(socketAnchor.position, 0.65f)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() =>
                    {
                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.PlaySnap();
                            SoundManager.Instance.PlayMagicHum();
                            SoundManager.Instance.PlayChime();
                            SoundManager.Instance.PlayFanfare();
                        }

                        token24Rect.DOPunchScale(Vector3.one * 0.25f, 0.4f, 4, 0.5f);
                        if (electricSparkParticle != null) electricSparkParticle.Play();
                        if (confettiParticles != null) confettiParticles.Play();

                        if (socketBlinkGlow != null)
                        {
                            socketBlinkGlow.color = new Color(0.2f, 1f, 0.5f, 1f);
                        }

                        ScoreManager.Instance?.AddScore(150, "EVEN NUMBER 24 PLUGGED!");

                        DOVirtual.DelayedCall(1.8f, () =>
                        {
                            onTaskCompleted?.Invoke();
                        });
                    });
            }
        }
    }
}
