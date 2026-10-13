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
        public TextMeshProUGUI finalScoreText;
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

            // Cinematic dark backdrop with radiant golden center aura
            if (goldenLightFlood != null)
            {
                goldenLightFlood.color = new Color(0.04f, 0.06f, 0.10f, 0f);
                goldenLightFlood.DOFade(0.92f, 1.0f);
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

            if (finalScoreText != null && ScoreManager.Instance != null)
            {
                int stars = ScoreManager.Instance.StarsEarned;
                finalScoreText.text = $"<size=60%><color=#94A3B8>TOTAL PUZZLE SCORE</color></size>\n<b><color=#FDE047><size=130%>{ScoreManager.Instance.CurrentScore:N0} POINTS</size></color></b>\n<size=80%><color=#FCD34D><b>{stars} / 3 STARS EARNED</b></color></size>";
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
            if (trophyRect != null)
            {
                trophyRect.DOKill();
                trophyRect.localScale = Vector3.zero;
            }
            if (recapContainer != null)
            {
                recapContainer.transform.DOKill();
                recapContainer.transform.localScale = Vector3.zero;
            }
            if (goldenLightFlood != null)
            {
                goldenLightFlood.DOKill();
                goldenLightFlood.color = new Color(0.04f, 0.06f, 0.10f, 0f);
            }
            if (victorySparkles != null)
            {
                victorySparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
