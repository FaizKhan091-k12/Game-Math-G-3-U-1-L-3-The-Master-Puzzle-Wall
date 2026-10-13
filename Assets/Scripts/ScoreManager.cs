using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("HUD Displays")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI comboText;
        public Image[] starImages;
        public Color starActiveColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color starInactiveColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);

        [Header("Popup Container")]
        public Transform popupContainer;

        public int CurrentScore { get; private set; } = 0;
        public int ComboStreak { get; private set; } = 1;
        public int StarsEarned { get; private set; } = 0;

        private int displayScore = 0;
        private Tween scoreTween;

        private readonly int[] starThresholds = new int[] { 1500, 3500, 6000 };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ResetScore();
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            displayScore = 0;
            ComboStreak = 1;
            StarsEarned = 0;
            UpdateScoreUI(immediate: true);
            UpdateStarsUI();
            UpdateComboUI();
        }

        public void AddScore(int basePoints, string label = "", Vector2? screenPos = null)
        {
            int earned = basePoints * ComboStreak;
            CurrentScore += earned;

            // Trigger score tween roll
            scoreTween?.Kill();
            scoreTween = DOTween.To(() => displayScore, x =>
            {
                displayScore = x;
                UpdateScoreUI(immediate: false);
            }, CurrentScore, 0.6f).SetEase(Ease.OutQuad);

            // Punch score text
            if (scoreText != null)
            {
                scoreText.transform.DOKill();
                scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 3, 0.4f);
            }

            // Spawn floating popup
            if (popupContainer != null)
            {
                Vector2 spawnPos = screenPos ?? new Vector2(0, 100);
                SpawnPopup(earned, label, spawnPos);
            }

            // Check star unlocks
            CheckStarThresholds();
        }

        public void IncreaseCombo()
        {
            ComboStreak = Mathf.Min(ComboStreak + 1, 5);
            UpdateComboUI();

            if (comboText != null && ComboStreak > 1)
            {
                comboText.transform.DOKill();
                comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.35f, 4, 0.5f);
            }
        }

        public void ResetCombo()
        {
            ComboStreak = 1;
            UpdateComboUI();
        }

        private void CheckStarThresholds()
        {
            for (int i = 0; i < starThresholds.Length; i++)
            {
                if (CurrentScore >= starThresholds[i] && StarsEarned <= i)
                {
                    StarsEarned = i + 1;
                    AnimateStarUnlock(i);
                }
            }
        }

        private void AnimateStarUnlock(int starIndex)
        {
            if (starImages != null && starIndex < starImages.Length && starImages[starIndex] != null)
            {
                Image star = starImages[starIndex];
                star.color = starActiveColor;
                star.transform.DOKill();
                star.transform.localScale = Vector3.zero;
                star.transform.DOScale(1.25f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    star.transform.DOScale(1.0f, 0.15f);
                });

                if (SoundManager.Instance != null) SoundManager.Instance.PlayChime();
            }
        }

        private void UpdateScoreUI(bool immediate)
        {
            if (scoreText != null)
            {
                scoreText.text = $"<b>SCORE: <color=#FDE047>{displayScore:N0}</color></b>";
            }
        }

        private void UpdateComboUI()
        {
            if (comboText != null)
            {
                if (ComboStreak > 1)
                {
                    comboText.gameObject.SetActive(true);
                    comboText.text = $"<b><color=#F97316>STREAK x{ComboStreak}!</color></b>";
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateStarsUI()
        {
            if (starImages == null) return;
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = (i < StarsEarned) ? starActiveColor : starInactiveColor;
                    starImages[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void SpawnPopup(int points, string label, Vector2 pos)
        {
            GameObject popupObj = new GameObject("ScorePopup");
            popupObj.transform.SetParent(popupContainer, false);
            RectTransform rt = popupObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 60);

            TextMeshProUGUI tmp = popupObj.AddComponent<TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(label) ? $"<b>+{points}</b>" : $"<b>+{points} {label}</b>";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.9f, 0.3f, 1f);

            // Animate popup rising and fading
            rt.DOAnchorPosY(pos.y + 70f, 0.8f).SetEase(Ease.OutQuad);
            rt.DOPunchScale(Vector3.one * 0.3f, 0.4f, 3, 0.5f);
            tmp.DOFade(0f, 0.4f).SetDelay(0.45f).OnComplete(() =>
            {
                Destroy(popupObj);
            });
        }
    }
}
