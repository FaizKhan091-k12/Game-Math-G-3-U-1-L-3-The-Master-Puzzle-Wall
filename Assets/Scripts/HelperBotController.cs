using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class HelperBotController : MonoBehaviour
    {
        [Header("Visual Components")]
        public Image botImage;
        public Sprite normalSprite;
        public Sprite confusedSprite;
        public RectTransform botRect;

        [Header("Speech Bubble")]
        public GameObject speechBubbleRoot;
        public TextMeshProUGUI speechText;
        public CanvasGroup speechBubbleGroup;

        [Header("Audio Voice Blip")]
        public AudioClip voiceBlipClip;

        private Coroutine typeRoutine;
        private Tween idleTween;
        private Tween bounceTween;

        private void Start()
        {
            if (botRect == null) botRect = GetComponent<RectTransform>();
            StartIdleAnimation();
        }

        public void StartIdleAnimation()
        {
            if (idleTween != null) idleTween.Kill();
            
            // Smooth float and subtle rock tilt for living golem feel
            Vector2 basePos = botRect.anchoredPosition;
            idleTween = botRect.DOAnchorPosY(basePos.y + 12f, 1.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            botRect.DORotate(new Vector3(0, 0, 2f), 2.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void Wave()
        {
            // Happy wave gesture
            botRect.DOPunchRotation(new Vector3(0, 0, 15f), 1.2f, 5, 0.5f);
        }

        public void Cheer()
        {
            if (botImage != null && normalSprite != null)
                botImage.sprite = normalSprite;

            if (botRect != null)
            {
                botRect.DOKill();
                botRect.DOPunchScale(new Vector3(0.28f, 0.28f, 0), 0.75f, 4, 0.4f);
                botRect.DOPunchPosition(new Vector3(0, 40f, 0), 0.75f, 3, 0.3f);
                botRect.DOPunchRotation(new Vector3(0, 0, 16f), 0.75f, 4, 0.4f).OnComplete(() =>
                {
                    StartIdleAnimation();
                });
            }
        }

        public void SetConfused(bool isConfused)
        {
            if (botImage == null) return;

            if (isConfused)
            {
                if (confusedSprite != null) botImage.sprite = confusedSprite;
                if (botRect != null)
                {
                    botRect.DOKill();
                    botRect.DOPunchRotation(new Vector3(0, 0, -14f), 0.8f, 3, 0.5f);
                    botRect.DOPunchPosition(new Vector3(-6f, -10f, 0), 0.8f, 2, 0.3f);
                }
            }
            else
            {
                if (normalSprite != null) botImage.sprite = normalSprite;
            }
        }

        public void Shiver()
        {
            if (botRect == null) return;
            botRect.DOKill();
            botRect.DOShakePosition(0.7f, new Vector3(10f, 0, 0), 25, 90f).OnComplete(() =>
            {
                StartIdleAnimation();
            });
        }

        public void Speak(string text, float duration = 0, System.Action onComplete = null)
        {
            if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
            if (speechText != null)
            {
                speechText.enableAutoSizing = true;
                speechText.fontSizeMin = 14;
                speechText.fontSizeMax = 20;
            }
            if (speechBubbleGroup != null)
            {
                speechBubbleGroup.alpha = 0f;
                speechBubbleGroup.DOFade(1f, 0.3f);
            }
            if (speechBubbleRoot != null)
            {
                speechBubbleRoot.transform.localScale = Vector3.zero;
                speechBubbleRoot.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypewriterRoutine(text, duration, onComplete));
        }

        private IEnumerator TypewriterRoutine(string fullText, float displayDuration, System.Action onComplete)
        {
            if (speechText == null) yield break;
            speechText.text = "";

            float charDelay = 0.025f;
            for (int i = 0; i < fullText.Length; i++)
            {
                speechText.text += fullText[i];
                yield return new WaitForSeconds(charDelay);
            }

            if (displayDuration > 0)
            {
                yield return new WaitForSeconds(displayDuration);
                HideSpeech();
            }

            onComplete?.Invoke();
        }

        public void HideSpeech()
        {
            if (speechBubbleGroup != null)
            {
                speechBubbleGroup.DOFade(0f, 0.25f).OnComplete(() =>
                {
                    if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
                });
            }
            else if (speechBubbleRoot != null)
            {
                speechBubbleRoot.SetActive(false);
            }
        }
    }
}
