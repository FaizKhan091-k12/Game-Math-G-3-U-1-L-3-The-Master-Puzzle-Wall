using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class BaseTenCodeTask : MonoBehaviour
    {
        [Header("Lock Bar & Display")]
        public RectTransform codeBarRect;
        public Image codeBarGlow;
        public TextMeshProUGUI promptDisplayText; // shows "[ ___ 0 ]"

        [Header("Plaque Options")]
        public RectTransform plaqueOddRect;
        public RectTransform plaqueDivide10Rect;
        public RectTransform plaqueLessThan10Rect;

        public Button plaqueOddButton;
        public Button plaqueDivide10Button;
        public Button plaqueLessThan10Button;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        public void InitializeTask()
        {
            isCompleted = false;

            if (promptDisplayText != null)
            {
                promptDisplayText.text = "<size=75%>ANY NUMBER ENDING IN ZERO:</size>\n<b>[ ___ 0 ]</b>";
            }

            if (codeBarGlow != null)
            {
                codeBarGlow.gameObject.SetActive(true);
                codeBarGlow.color = new Color(1f, 0.85f, 0.2f, 0.3f);
                codeBarGlow.DOFade(0.85f, 0.8f).SetLoops(-1, LoopType.Yoyo);
            }

            // Animate plaques lifting up from stone base
            AnimatePlaqueEntrance(plaqueOddRect, 0.0f);
            AnimatePlaqueEntrance(plaqueDivide10Rect, 0.15f);
            AnimatePlaqueEntrance(plaqueLessThan10Rect, 0.3f);

            SetupPlaque(plaqueOddButton, () => OnWrongPlaqueClicked(plaqueOddRect, "Numbers ending in 0 (like 10, 20, 30) are EVEN, not odd!"));
            SetupPlaque(plaqueLessThan10Button, () => OnWrongPlaqueClicked(plaqueLessThan10Rect, "Numbers ending in 0 can be huge, like 50, 100, or 1000! They are not always less than 10!"));
            SetupPlaque(plaqueDivide10Button, OnCorrectPlaqueClicked);
        }

        private void AnimatePlaqueEntrance(RectTransform rt, float delay)
        {
            if (rt == null) return;
            Vector2 targetPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(targetPos.x, targetPos.y - 80f);
            rt.DOAnchorPosY(targetPos.y, 0.5f).SetDelay(delay).SetEase(Ease.OutBack);
        }

        private void SetupPlaque(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void OnWrongPlaqueClicked(RectTransform rect, string explanation)
        {
            if (isCompleted) return;

            if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
            if (rect != null)
            {
                rect.DOPunchPosition(new Vector3(12f, 0, 0), 0.5f, 6, 0.4f);
            }

            GameManager.Instance?.GolemBot.SetConfused(true);
            GameManager.Instance?.GolemBot.Speak(explanation, 3.8f, () =>
            {
                GameManager.Instance?.GolemBot.SetConfused(false);
            });
        }

        private void OnCorrectPlaqueClicked()
        {
            if (isCompleted) return;
            isCompleted = true;

            plaqueOddButton.interactable = false;
            plaqueDivide10Button.interactable = false;
            plaqueLessThan10Button.interactable = false;

            if (plaqueOddRect != null) plaqueOddRect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);
            if (plaqueLessThan10Rect != null) plaqueLessThan10Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySnap();
                SoundManager.Instance.PlayChime();
            }

            // Punch correct plaque
            if (plaqueDivide10Rect != null)
            {
                plaqueDivide10Rect.DOPunchScale(Vector3.one * 0.2f, 0.5f, 4, 0.5f);
                plaqueDivide10Rect.GetComponent<Image>()?.DOColor(new Color(1f, 0.9f, 0.3f), 0.3f);
            }

            // Slide lock bar open
            if (codeBarRect != null)
            {
                codeBarRect.DOAnchorPosY(codeBarRect.anchoredPosition.y - 40f, 0.8f).SetEase(Ease.InCubic);
            }

            if (codeBarGlow != null)
            {
                codeBarGlow.DOKill();
                codeBarGlow.color = new Color(1f, 0.95f, 0.4f, 1f);
            }

            onTaskCompleted?.Invoke();
        }
    }
}
