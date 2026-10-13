using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class VictoryPanel : MonoBehaviour
    {
        [Header("Sliding Wall Halves")]
        public RectTransform wallLeftHalf;
        public RectTransform wallRightHalf;
        public Image goldenLightFlood;

        [Header("Gemstone Trophy")]
        public RectTransform trophyRect;
        public Image trophyImage;

        [Header("Text & UI")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI victorySpeechText;
        public GameObject recapContainer;
        public Button playAgainButton;
        public Button mainMenuButton;

        [Header("Particles")]
        public ParticleSystem victorySparkles;

        private Vector2 initialLeftPos;
        private Vector2 initialRightPos;

        private void Awake()
        {
            if (wallLeftHalf != null) initialLeftPos = wallLeftHalf.anchoredPosition;
            if (wallRightHalf != null) initialRightPos = wallRightHalf.anchoredPosition;

            if (playAgainButton != null) playAgainButton.onClick.AddListener(() => GameManager.Instance?.StartGame(0));
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.ShowMainMenu());
        }

        public void ShowVictory()
        {
            gameObject.SetActive(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayFanfare();
            }

            // Wall halves slide open
            if (wallLeftHalf != null)
            {
                wallLeftHalf.DOKill();
                wallLeftHalf.anchoredPosition = initialLeftPos;
                wallLeftHalf.DOAnchorPosX(initialLeftPos.x - 700f, 1.4f).SetEase(Ease.InOutCubic);
            }

            if (wallRightHalf != null)
            {
                wallRightHalf.DOKill();
                wallRightHalf.anchoredPosition = initialRightPos;
                wallRightHalf.DOAnchorPosX(initialRightPos.x + 700f, 1.4f).SetEase(Ease.InOutCubic);
            }

            // Golden light flood
            if (goldenLightFlood != null)
            {
                goldenLightFlood.color = new Color(1f, 0.95f, 0.6f, 0f);
                goldenLightFlood.DOFade(0.85f, 1.2f);
            }

            // Trophy appearance
            if (trophyRect != null)
            {
                trophyRect.localScale = Vector3.zero;
                trophyRect.DOScale(Vector3.one, 0.8f).SetDelay(0.7f).SetEase(Ease.OutBack);
                trophyRect.DOAnchorPosY(trophyRect.anchoredPosition.y + 15f, 1.5f)
                    .SetDelay(1.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (victorySparkles != null)
            {
                victorySparkles.Play();
            }

            if (recapContainer != null)
            {
                recapContainer.transform.localScale = Vector3.zero;
                recapContainer.transform.DOScale(Vector3.one, 0.5f).SetDelay(1.2f).SetEase(Ease.OutBack);
            }

            GameManager.Instance?.GolemBot.Cheer();
            GameManager.Instance?.GolemBot.Speak(
                "Success! The master puzzle wall has opened wide! Every single track, slot, wheel, and panel was fixed perfectly. You are a Master Pattern Solver! Fantastic job!",
                6.0f
            );
        }

        public void ResetWallPositions()
        {
            if (wallLeftHalf != null)
            {
                wallLeftHalf.DOKill();
                wallLeftHalf.anchoredPosition = initialLeftPos;
            }
            if (wallRightHalf != null)
            {
                wallRightHalf.DOKill();
                wallRightHalf.anchoredPosition = initialRightPos;
            }
        }
    }
}
