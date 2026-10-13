using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class FactorWheelTask : MonoBehaviour
    {
        [Header("Wheel Mechanism")]
        public RectTransform wheelRect;
        public Image wheelGlowImage;
        public GameObject factorIndicatorsGroup; // 6 glowing group markers around the 18 slots

        [Header("Peg Tools")]
        public RectTransform peg4Rect;
        public RectTransform peg3Rect;
        public RectTransform peg8Rect;
        public Button peg4Button;
        public Button peg3Button;
        public Button peg8Button;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        private Tween spinTween;

        public void InitializeTask()
        {
            isCompleted = false;
            if (factorIndicatorsGroup != null) factorIndicatorsGroup.SetActive(false);

            if (wheelRect != null)
            {
                wheelRect.DOKill();
                // Gentle continuous spin
                spinTween = wheelRect.DORotate(new Vector3(0, 0, -360f), 12f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1);
            }

            if (wheelGlowImage != null)
            {
                wheelGlowImage.gameObject.SetActive(true);
                wheelGlowImage.color = new Color(1f, 0.8f, 0.3f, 0.25f);
                wheelGlowImage.DOFade(0.8f, 1f).SetLoops(-1, LoopType.Yoyo);
            }

            SetupPegButton(peg4Button, () => OnWrongPegClicked(peg4Rect, 4, "18 / 4 = 4 with 2 left over!"));
            SetupPegButton(peg8Button, () => OnWrongPegClicked(peg8Rect, 8, "18 / 8 = 2 with 2 left over!"));
            SetupPegButton(peg3Button, OnCorrectPegClicked);
        }

        private void SetupPegButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void OnWrongPegClicked(RectTransform rect, int pegVal, string reason)
        {
            if (isCompleted) return;

            if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
            if (rect != null)
            {
                rect.DOPunchPosition(new Vector3(12f, 0, 0), 0.5f, 6, 0.4f);
            }

            GameManager.Instance?.GolemBot.SetConfused(true);
            GameManager.Instance?.GolemBot.Speak($"Peg {pegVal} leaves empty slots! {reason} We need a factor that divides 18 with 0 remainder!", 3.5f, () =>
            {
                GameManager.Instance?.GolemBot.SetConfused(false);
            });
        }

        private void OnCorrectPegClicked()
        {
            if (isCompleted) return;
            isCompleted = true;

            peg4Button.interactable = false;
            peg3Button.interactable = false;
            peg8Button.interactable = false;

            if (peg4Rect != null) peg4Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);
            if (peg8Rect != null) peg8Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);

            // Move peg 3 into center hub of wheel
            if (peg3Rect != null && wheelRect != null)
            {
                peg3Rect.DOMove(wheelRect.position, 0.6f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (spinTween != null) spinTween.Kill();
                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.PlayRatchet();
                            SoundManager.Instance.PlaySnap();
                            SoundManager.Instance.PlayChime();
                        }

                        // Wheel snaps to lock position
                        wheelRect.DORotate(new Vector3(0, 0, 0), 0.4f).SetEase(Ease.OutBounce);
                        wheelRect.DOPunchScale(Vector3.one * 0.15f, 0.5f, 4, 0.5f);

                        if (factorIndicatorsGroup != null) factorIndicatorsGroup.SetActive(true);

                        if (wheelGlowImage != null)
                        {
                            wheelGlowImage.DOKill();
                            wheelGlowImage.color = new Color(1f, 0.9f, 0.2f, 1f);
                        }

                        onTaskCompleted?.Invoke();
                    });
            }
        }
    }
}
