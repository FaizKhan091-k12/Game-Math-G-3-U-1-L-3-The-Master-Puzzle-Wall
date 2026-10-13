using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class SquareTileTask : MonoBehaviour
    {
        [Header("Square Slot Target")]
        public RectTransform squareSlotTarget;
        public Image slotGreenGlow;

        [Header("Tile Choices")]
        public RectTransform block12Rect;
        public RectTransform block16Rect;
        public RectTransform block18Rect;
        public Button block12Button;
        public Button block16Button;
        public Button block18Button;

        [Header("Visual Grid 4x4 In Slot")]
        public GameObject solved4x4GridDisplay;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        public void InitializeTask()
        {
            isCompleted = false;
            if (solved4x4GridDisplay != null) solved4x4GridDisplay.SetActive(false);

            if (slotGreenGlow != null)
            {
                slotGreenGlow.gameObject.SetActive(true);
                slotGreenGlow.color = new Color(0.2f, 1f, 0.4f, 0.3f);
                slotGreenGlow.DOFade(0.85f, 0.9f).SetLoops(-1, LoopType.Yoyo);
            }

            SetupButton(block12Button, () => OnWrongBlockClicked(block12Rect, 12, "3x4"));
            SetupButton(block18Button, () => OnWrongBlockClicked(block18Rect, 18, "3x6"));
            SetupButton(block16Button, OnCorrectBlockClicked);
        }

        private void SetupButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void OnWrongBlockClicked(RectTransform rect, int num, string dimensions)
        {
            if (isCompleted) return;

            if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
            if (rect != null)
            {
                rect.DOPunchPosition(new Vector3(12f, 0, 0), 0.5f, 6, 0.4f);
            }

            GameManager.Instance?.GolemBot.SetConfused(true);
            GameManager.Instance?.GolemBot.Speak($"{num} tiles make a {dimensions} rectangle, not a square! A square number needs equal sides, like 4 x 4!", 3.5f, () =>
            {
                GameManager.Instance?.GolemBot.SetConfused(false);
            });
        }

        private void OnCorrectBlockClicked()
        {
            if (isCompleted) return;
            isCompleted = true;

            block12Button.interactable = false;
            block16Button.interactable = false;
            block18Button.interactable = false;

            if (block12Rect != null) block12Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);
            if (block18Rect != null) block18Rect.GetComponent<CanvasGroup>()?.DOFade(0.2f, 0.4f);

            if (block16Rect != null && squareSlotTarget != null)
            {
                block16Rect.DOMove(squareSlotTarget.position, 0.65f)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.PlaySnap();
                            SoundManager.Instance.PlayChime();
                        }

                        if (solved4x4GridDisplay != null) solved4x4GridDisplay.SetActive(true);
                        block16Rect.DOPunchScale(Vector3.one * 0.2f, 0.4f, 4, 0.5f);

                        if (slotGreenGlow != null)
                        {
                            slotGreenGlow.DOKill();
                            slotGreenGlow.color = new Color(0.2f, 1f, 0.4f, 1f);
                        }

                        onTaskCompleted?.Invoke();
                    });
            }
        }
    }
}
