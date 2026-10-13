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

        [Header("Plug Tokens")]
        public RectTransform token17Rect;
        public RectTransform token24Rect;
        public Button token17Button;
        public Button token24Button;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        private Vector2 initialPos17;
        private Vector2 initialPos24;

        public void InitializeTask()
        {
            isCompleted = false;
            if (token17Rect != null) initialPos17 = token17Rect.anchoredPosition;
            if (token24Rect != null) initialPos24 = token24Rect.anchoredPosition;

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
                socketBlinkGlow.gameObject.SetActive(true);
                socketBlinkGlow.DOFade(0.85f, 0.6f).SetLoops(-1, LoopType.Yoyo);
            }

            // Idle gentle float for both tokens
            if (token17Rect != null)
            {
                token17Rect.DOAnchorPosY(initialPos17.y + 10f, 1.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
            if (token24Rect != null)
            {
                token24Rect.DOAnchorPosY(initialPos24.y - 10f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
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
                        }

                        token24Rect.DOPunchScale(Vector3.one * 0.25f, 0.4f, 4, 0.5f);
                        if (electricSparkParticle != null) electricSparkParticle.Play();

                        if (socketBlinkGlow != null)
                        {
                            socketBlinkGlow.color = new Color(0.2f, 1f, 0.5f, 1f);
                        }

                        onTaskCompleted?.Invoke();
                    });
            }
        }
    }
}
