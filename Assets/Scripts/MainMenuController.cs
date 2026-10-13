using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Menu Elements")]
        public RectTransform titleLogoRect;
        public Button playButton;
        public Button levelSelectButton;
        public Button soundToggleButton;
        public TextMeshProUGUI soundToggleText;
        public Image soundToggleIcon;
        public Button starterGemButton;
        public RectTransform starterGemRect;
        public RectTransform gemPromptBadgeRect;

        [Header("Level Select Modal")]
        public GameObject levelSelectPanel;
        public Button[] taskSelectButtons; // 6 buttons for Task 1 to 6
        public Button closeLevelSelectButton;

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (starterGemButton != null) starterGemButton.onClick.AddListener(OnPlayClicked);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OpenLevelSelect);
            if (closeLevelSelectButton != null) closeLevelSelectButton.onClick.AddListener(CloseLevelSelect);
            if (soundToggleButton != null)
            {
                soundToggleButton.onClick.AddListener(OnSoundToggle);
                if (soundToggleText == null)
                {
                    soundToggleText = soundToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                }
                UpdateSoundButtonUI();
            }

            for (int i = 0; i < taskSelectButtons.Length; i++)
            {
                int taskIndex = i;
                taskSelectButtons[i].onClick.AddListener(() =>
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
                    CloseLevelSelect();
                    GameManager.Instance?.StartGame(taskIndex);
                });
            }

            // HoverScale component handles idle pulse and hover transitions
            if (starterGemRect != null && starterGemRect.GetComponent<HoverScale>() == null)
            {
                var hs = starterGemRect.gameObject.AddComponent<HoverScale>();
                hs.hasIdlePulse = true;
                hs.hoverScale = 1.15f;
            }

            if (gemPromptBadgeRect != null && gemPromptBadgeRect.GetComponent<HoverScale>() == null)
            {
                var hs = gemPromptBadgeRect.gameObject.AddComponent<HoverScale>();
                hs.hoverScale = 1.06f;
            }
        }

        public void ShowMenu()
        {
            gameObject.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            UpdateSoundButtonUI();

            if (titleLogoRect != null)
            {
                titleLogoRect.localScale = Vector3.zero;
                titleLogoRect.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack);
            }

            if (playButton != null)
            {
                playButton.transform.localScale = Vector3.zero;
                playButton.transform.DOScale(Vector3.one, 0.5f).SetDelay(0.3f).SetEase(Ease.OutBack);
            }

            GameManager.Instance?.GolemBot.Wave();
            GameManager.Instance?.GolemBot.Speak(
                "Welcome to the Master Puzzle Wall! Press the glowing starter gem to begin your run!",
                4.5f
            );
        }

        private void OnPlayClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayClick();
                SoundManager.Instance.PlayMagicHum();
            }

            starterGemRect?.DOPunchScale(Vector3.one * 0.3f, 0.5f, 4, 0.5f);

            // Animate transition to game
            DOVirtual.DelayedCall(0.3f, () =>
            {
                GameManager.Instance?.StartGame(0);
            });
        }

        private void OpenLevelSelect()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
            if (levelSelectPanel != null)
            {
                levelSelectPanel.SetActive(true);
                levelSelectPanel.transform.localScale = Vector3.zero;
                levelSelectPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }

        private void CloseLevelSelect()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
            if (levelSelectPanel != null)
            {
                levelSelectPanel.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() =>
                {
                    levelSelectPanel.SetActive(false);
                });
            }
        }

        private void OnSoundToggle()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
                UpdateSoundButtonUI();
            }
        }

        public void UpdateSoundButtonUI()
        {
            if (SoundManager.Instance == null) return;
            bool isMuted = SoundManager.Instance.IsMuted;
            if (soundToggleText != null)
            {
                soundToggleText.text = isMuted ? "<b>AUDIO: OFF</b>" : "<b>AUDIO: ON</b>";
                soundToggleText.color = isMuted ? new Color(0.7f, 0.7f, 0.7f, 0.85f) : Color.white;
            }
            if (soundToggleIcon != null)
            {
                soundToggleIcon.color = isMuted ? new Color(0.6f, 0.6f, 0.6f, 0.5f) : Color.white;
            }
        }
    }
}
