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
        public ParticleSystem confettiParticles;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        private Vector2 defaultPos12 = new Vector2(-160, -95);
        private Vector2 defaultPos16 = new Vector2(0, -95);
        private Vector2 defaultPos18 = new Vector2(160, -95);
        private bool positionsCached = false;

        private void Awake()
        {
            CachePositions();
        }

        private void CachePositions()
        {
            if (!positionsCached)
            {
                if (block12Rect != null) defaultPos12 = block12Rect.anchoredPosition;
                if (block16Rect != null) defaultPos16 = block16Rect.anchoredPosition;
                if (block18Rect != null) defaultPos18 = block18Rect.anchoredPosition;
                positionsCached = true;
            }
        }

        public void InitializeTask()
        {
            CachePositions();
            isCompleted = false;

            if (solved4x4GridDisplay != null) solved4x4GridDisplay.SetActive(false);
            if (confettiParticles != null) confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ResetBlock(block12Rect, defaultPos12);
            ResetBlock(block16Rect, defaultPos16);
            ResetBlock(block18Rect, defaultPos18);

            if (slotGreenGlow != null)
            {
                slotGreenGlow.DOKill();
                slotGreenGlow.gameObject.SetActive(true);
                slotGreenGlow.color = new Color(0.2f, 1f, 0.4f, 0.3f);
                slotGreenGlow.DOFade(0.85f, 0.9f).SetLoops(-1, LoopType.Yoyo);
            }

            SetupButton(block12Button, () => OnWrongBlockClicked(block12Rect, 12, "3x4"));
            SetupButton(block18Button, () => OnWrongBlockClicked(block18Rect, 18, "3x6"));
            SetupButton(block16Button, OnCorrectBlockClicked);
        }

        private void ResetBlock(RectTransform rt, Vector2 defaultPos)
        {
            if (rt == null) return;
            rt.DOKill();
            rt.anchoredPosition = defaultPos;
            rt.localScale = Vector3.one;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOKill();
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }
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
                            SoundManager.Instance.PlayFanfare();
                        }

                        if (solved4x4GridDisplay != null) solved4x4GridDisplay.SetActive(true);
                        block16Rect.DOPunchScale(Vector3.one * 0.2f, 0.4f, 4, 0.5f);
                        if (confettiParticles != null) confettiParticles.Play();

                        if (slotGreenGlow != null)
                        {
                            slotGreenGlow.DOKill();
                            slotGreenGlow.color = new Color(0.2f, 1f, 0.4f, 1f);
                        }

                        ScoreManager.Instance?.AddScore(150, "SQUARE NUMBER 16 FITTED!");

                        DOVirtual.DelayedCall(1.8f, () =>
                        {
                            onTaskCompleted?.Invoke();
                        });
                    });
            }
        }
    }
}
