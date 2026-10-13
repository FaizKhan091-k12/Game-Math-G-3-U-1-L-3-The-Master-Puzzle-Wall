using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class FailPanel : MonoBehaviour
    {
        [Header("Lockdown Visuals")]
        public Image dullGrayOverlay;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI promptText;
        public Button retryCodeButton;

        private void Awake()
        {
            if (retryCodeButton != null)
            {
                retryCodeButton.onClick.AddListener(OnRetryClicked);
            }
        }

        public void ShowLockdown(Action onRetryAction)
        {
            gameObject.SetActive(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayError();
            }

            if (dullGrayOverlay != null)
            {
                dullGrayOverlay.color = new Color(0.2f, 0.22f, 0.25f, 0f);
                dullGrayOverlay.DOFade(0.75f, 0.6f);
            }

            if (retryCodeButton != null)
            {
                retryCodeButton.transform.localScale = Vector3.zero;
                retryCodeButton.transform.DOScale(Vector3.one, 0.4f).SetDelay(0.4f).SetEase(Ease.OutBack);
                retryCodeButton.onClick.RemoveAllListeners();
                retryCodeButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                    onRetryAction?.Invoke();
                });
            }

            GameManager.Instance?.GolemBot.SetConfused(true);
            GameManager.Instance?.GolemBot.Speak(
                "Uh oh! The code numbers got mixed up, and the stone panels have locked back up. Let's look over our slider tracks and check our ending digits one more time. Press the orange button to restart the wall circuits!",
                6.0f
            );
        }

        private void OnRetryClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
            GameManager.Instance?.GolemBot.SetConfused(false);
        }
    }
}
