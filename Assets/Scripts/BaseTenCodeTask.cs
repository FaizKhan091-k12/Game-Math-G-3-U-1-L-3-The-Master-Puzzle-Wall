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

        private Vector2 defaultPosOdd = new Vector2(-160, -90);
        private Vector2 defaultPos10 = new Vector2(0, -90);
        private Vector2 defaultPosLess = new Vector2(160, -90);
        private Vector2 defaultPosCodeBar = new Vector2(0, -220);
        private Color defaultPlaqueColor = new Color(0.12f, 0.22f, 0.35f, 0.95f);
        private bool positionsCached = false;

        private void Awake()
        {
            CachePositions();
        }

        private void CachePositions()
        {
            if (!positionsCached)
            {
                if (plaqueOddRect != null) defaultPosOdd = plaqueOddRect.anchoredPosition;
                if (plaqueDivide10Rect != null)
                {
                    defaultPos10 = plaqueDivide10Rect.anchoredPosition;
                    var img = plaqueDivide10Rect.GetComponent<Image>();
                    if (img != null) defaultPlaqueColor = img.color;
                }
                if (plaqueLessThan10Rect != null) defaultPosLess = plaqueLessThan10Rect.anchoredPosition;
                if (codeBarRect != null) defaultPosCodeBar = codeBarRect.anchoredPosition;
                positionsCached = true;
            }
        }

        public void InitializeTask()
        {
            CachePositions();
            isCompleted = false;

            if (promptDisplayText != null)
            {
                promptDisplayText.text = "<size=75%>ANY NUMBER ENDING IN ZERO:</size>\n<b>[ ___ 0 ]</b>";
            }

            ResetPlaque(plaqueOddRect, defaultPosOdd);
            ResetPlaque(plaqueDivide10Rect, defaultPos10);
            ResetPlaque(plaqueLessThan10Rect, defaultPosLess);

            if (plaqueDivide10Rect != null)
            {
                var img = plaqueDivide10Rect.GetComponent<Image>();
                if (img != null) img.color = defaultPlaqueColor;
            }

            if (codeBarRect != null)
            {
                codeBarRect.DOKill();
                codeBarRect.anchoredPosition = defaultPosCodeBar;
            }

            if (codeBarGlow != null)
            {
                codeBarGlow.DOKill();
                codeBarGlow.gameObject.SetActive(true);
                codeBarGlow.color = new Color(0f, 0.0f, 0.0f, 0.3f);
                codeBarGlow.DOFade(0.85f, 0.8f).SetLoops(-1, LoopType.Yoyo);
            }

            // Animate plaques lifting up from stone base
            AnimatePlaqueEntrance(plaqueOddRect, defaultPosOdd, 0.0f);
            AnimatePlaqueEntrance(plaqueDivide10Rect, defaultPos10, 0.15f);
            AnimatePlaqueEntrance(plaqueLessThan10Rect, defaultPosLess, 0.3f);

            SetupPlaque(plaqueOddButton, () => OnWrongPlaqueClicked(plaqueOddRect, "Numbers ending in 0 (like 10, 20, 30) are EVEN, not odd!"));
            SetupPlaque(plaqueLessThan10Button, () => OnWrongPlaqueClicked(plaqueLessThan10Rect, "Numbers ending in 0 can be huge, like 50, 100, or 1000! They are not always less than 10!"));
            SetupPlaque(plaqueDivide10Button, OnCorrectPlaqueClicked);
        }

        private void ResetPlaque(RectTransform rt, Vector2 targetPos)
        {
            if (rt == null) return;
            rt.DOKill();
            rt.anchoredPosition = targetPos;
            rt.localScale = Vector3.one;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOKill();
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }
        }

        private void AnimatePlaqueEntrance(RectTransform rt, Vector2 targetPos, float delay)
        {
            if (rt == null) return;
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
