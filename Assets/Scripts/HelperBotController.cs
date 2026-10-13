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

            botRect.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.6f, 4, 0.4f);
            botRect.DOPunchPosition(new Vector3(0, 30f, 0), 0.6f, 3, 0.3f);
        }

        public void SetConfused(bool isConfused)
        {
            if (botImage == null) return;

            if (isConfused)
            {
                if (confusedSprite != null) botImage.sprite = confusedSprite;
                botRect.DOPunchRotation(new Vector3(0, 0, -12f), 0.8f, 3, 0.5f);
            }
            else
            {
                if (normalSprite != null) botImage.sprite = normalSprite;
            }
        }

        public void Speak(string text, float duration = 0, System.Action onComplete = null)
        {
            if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
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
