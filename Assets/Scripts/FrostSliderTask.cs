using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class FrostSliderTask : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("Slider Geometry")]
        public RectTransform trackRect;
        public RectTransform sliderGem;
        public Image sliderGemGlow;
        public Image fluidFillImage;
        public Image frostVaporOverlay;
        public GameObject zeroLineHighlight;

        [Header("Reactive Wall Runes")]
        public Image[] reactiveRuneFrostIcons;
        public TextMeshProUGUI[] tickLabelTexts;

        [Header("Readout & Stage Prompts")]
        public TextMeshProUGUI currentValueText;
        public TextMeshProUGUI subZeroDescText;
        [Header("Juice & Effects")]
        public ParticleSystem confettiParticles;
        public GameObject targetRuneSealButton;
        public TextMeshProUGUI sealButtonText;

        public const string GOLEM_INSTRUCTION = "Grab the glowing slider gem and pull it straight down past the white zero line!";

        [Header("Track Limits")]
        public float topY = 190f;      // Represents +2
        public float zeroY = 70f;      // Represents 0 (Freezing line)
        public float bottomY = -230f;  // Represents -5
        public int minValue = -5;
        public int maxValue = 2;

        [Header("Multi-Step Puzzle State")]
        public int currentStep = 0;    // 0: Target -2, 1: Target -4, 2: Completed
        public int targetValue = -2;
        public bool isLocked = false;
        public Action onTaskCompleted;

        private int currentVal = 0;
        private bool isDragging = false;
        public bool IsDragging => isDragging;
        private Tween fluidColorTween;

        private void Awake()
        {
            SetupSealButton();
        }

        private void Start()
        {
            SetupSealButton();
        }

        public void SetupSealButton()
        {
            if (targetRuneSealButton == null) return;

            Button btn = targetRuneSealButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnSealButtonTapped);
            }

            EventTrigger trigger = targetRuneSealButton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = targetRuneSealButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => OnSealButtonTapped());
            trigger.triggers.Add(entry);
        }

        public void InitializeTask()
        {
            isLocked = false;
            isDragging = false;
            currentStep = 0;
            targetValue = -4;
            currentVal = 0;

            if (confettiParticles != null) confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (targetRuneSealButton != null)
            {
                targetRuneSealButton.transform.DOKill();
                targetRuneSealButton.SetActive(false);
                SetupSealButton();
            }

            if (sliderGem != null)
            {
                sliderGem.DOKill();
                sliderGem.localScale = Vector3.one;
            }

            SetGemPositionByValue(0);
            UpdateVisualFeedback(0);

            if (sliderGem != null)
            {
                sliderGem.DOPunchScale(Vector3.one * 0.3f, 0.6f, 3, 0.4f);
            }

            // Golem instruction from GameManager
            GameManager.Instance?.GolemBot?.SetConfused(false);
            string instruction = GameManager.Instance != null ? GameManager.Instance.GetTaskInstruction(0) : GOLEM_INSTRUCTION;
            GameManager.Instance?.GolemBot?.Speak(instruction, 0);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            isDragging = true;
            GameManager.Instance?.GolemBot?.SetConfused(false);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayIceSlide();
            sliderGem.DOScale(1.18f, 0.15f).SetEase(Ease.OutBack);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isLocked || trackRect == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                trackRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

            float clampedY = Mathf.Clamp(localPoint.y, bottomY, topY);
            sliderGem.anchoredPosition = new Vector2(0, clampedY);

            int calculatedVal = YToValue(clampedY);
            if (calculatedVal != currentVal)
            {
                currentVal = calculatedVal;
                UpdateVisualFeedback(currentVal);

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayClick();
                }

                // Golem shiver reaction when deep below zero
                if (currentVal <= -3)
                {
                    GameManager.Instance?.GolemBot?.SetConfused(false);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            isDragging = false;
            sliderGem.DOScale(1.0f, 0.15f);

            // Snap gem smoothly to exact tick
            SnapToValue(currentVal);

            // Check step objectives
            CheckStepProgress();
        }

        private void CheckStepProgress()
        {
            if (currentVal == targetValue)
            {
                // CORRECT ANSWER: Reached -4!
                isLocked = true;

                ScoreManager.Instance?.AddScore(1000, "DEEP FREEZE -4!", sliderGem.position);
                ScoreManager.Instance?.IncreaseCombo();

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySnap();
                    SoundManager.Instance.PlayFanfare();
                    SoundManager.Instance.PlayChime();
                }

                // Festive confetti and burst particles!
                if (confettiParticles != null) confettiParticles.Play();
                if (GameManager.Instance?.taskCompleteBurst != null) GameManager.Instance.taskCompleteBurst.Play();

                sliderGem.DOPunchScale(Vector3.one * 0.45f, 0.6f, 5, 0.5f);
                GameManager.Instance?.TriggerScreenFlash(new Color(0.3f, 0.95f, 1f, 0.5f));

                // Celebration Golem!
                GameManager.Instance?.GolemBot?.SetConfused(false);
                GameManager.Instance?.GolemBot?.Cheer();
                GameManager.Instance?.GolemBot?.HideSpeech();

                // Reveal interactive seal button
                if (targetRuneSealButton != null)
                {
                    targetRuneSealButton.SetActive(true);
                    targetRuneSealButton.transform.SetAsLastSibling();
                    SetupSealButton();
                    targetRuneSealButton.transform.localScale = Vector3.zero;
                    targetRuneSealButton.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
                }
                else
                {
                    DOVirtual.DelayedCall(1.2f, () => onTaskCompleted?.Invoke());
                }
            }
            else
            {
                // WRONG ANSWER: left somewhere other than -4
                ScoreManager.Instance?.ResetCombo();

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayError();
                }

                // Golem gets sad on wrong answer and repeats the exact instruction!
                GameManager.Instance?.GolemBot?.SetConfused(true);
                string wrongAnswerPrompt = GameManager.Instance != null ? GameManager.Instance.GetTaskInstruction(0) : GOLEM_INSTRUCTION;
                GameManager.Instance?.GolemBot?.Speak(wrongAnswerPrompt, 0);

                sliderGem.DOPunchPosition(new Vector3(10f, 0, 0), 0.35f, 5, 0.3f);
            }
        }

        public void OnSealButtonTapped()
        {
            Debug.Log("<color=#00FFFF><b>[FrostSliderTask]</b> SHATTER ICE & ACTIVATE button tapped!</color>");

            if (targetRuneSealButton != null)
            {
                targetRuneSealButton.transform.DOPunchScale(Vector3.one * 0.35f, 0.4f, 4, 0.4f);
                Button btn = targetRuneSealButton.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }

            ScoreManager.Instance?.AddScore(500, "ICE SHATTERED!", sliderGem.position);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayFanfare();
                SoundManager.Instance.PlaySnap();
            }

            if (confettiParticles != null) confettiParticles.Play();
            GameManager.Instance?.TriggerScreenFlash(new Color(0.5f, 1f, 0.8f, 0.5f));
            GameManager.Instance?.GolemBot?.Cheer();

            DOVirtual.DelayedCall(0.5f, () =>
            {
                Debug.Log("<color=#00FF88><b>[FrostSliderTask]</b> Task 1 finished. Invoking onTaskCompleted...</color>");
                onTaskCompleted?.Invoke();
            });
        }

        private void UpdateVisualFeedback(int val)
        {
            // 1. Text Readout
            if (currentValueText != null)
            {
                if (val > 0)
                {
                    currentValueText.text = $"<b><color=#FBBF24>+{val}C  (WARM)</color></b>";
                }
                else if (val == 0)
                {
                    currentValueText.text = "<b><color=#FFFFFF>0C  (FREEZING POINT)</color></b>";
                }
                else
                {
                    int steps = Mathf.Abs(val);
                    string targetStar = (val == targetValue) ? " [GOAL!]" : "";
                    currentValueText.text = $"<b><color=#38BDF8>{val}C  ({steps} BELOW ZERO){targetStar}</color></b>";
                }
            }

            if (subZeroDescText != null)
            {
                if (val < 0)
                    subZeroDescText.text = $"<color=#7DD3FC>MINUS SIGN (-) MEANS {Mathf.Abs(val)} STEPS COLDER THAN ZERO</color>";
                else if (val == 0)
                    subZeroDescText.text = "<color=#F8FAFC>THE WHITE ZERO LINE SEPARATES WARM FROM COLD</color>";
                else
                    subZeroDescText.text = "<color=#FDE68A>ABOVE ZERO IS WARMER TEMPERATURE</color>";
            }

            // 2. Fluid Fill & Color
            if (fluidFillImage != null)
            {
                float normalizedFill = Mathf.InverseLerp(minValue, maxValue, val);
                fluidFillImage.fillAmount = normalizedFill;

                Color targetCol = val switch
                {
                    > 0 => new Color(1f, 0.6f, 0.15f, 0.9f),
                    0 => new Color(0.9f, 0.95f, 1f, 0.9f),
                    _ => new Color(0.2f, 0.8f, 1f, 0.95f)
                };

                fluidFillImage.color = targetCol;
            }

            // 3. Frost Vapor
            if (frostVaporOverlay != null)
            {
                float vaporAlpha = val < 0 ? Mathf.Clamp01(Mathf.Abs(val) / 5f) : 0f;
                frostVaporOverlay.DOFade(vaporAlpha * 0.7f, 0.2f);
            }

            // 4. Reactive Runes Frost Crystal Freeze Effect
            if (reactiveRuneFrostIcons != null)
            {
                for (int i = 0; i < reactiveRuneFrostIcons.Length; i++)
                {
                    if (reactiveRuneFrostIcons[i] == null) continue;
                    float frostIntensity = Mathf.Clamp01((Mathf.Abs(val) - (i * 1.2f)) / 2f);
                    reactiveRuneFrostIcons[i].color = new Color(0.4f, 0.9f, 1f, val < 0 ? frostIntensity : 0f);
                    reactiveRuneFrostIcons[i].transform.localScale = Vector3.one * (1f + (frostIntensity * 0.15f));
                }
            }

            // 5. Highlight Ticks
            if (tickLabelTexts != null)
            {
                for (int i = 0; i < tickLabelTexts.Length; i++)
                {
                    if (tickLabelTexts[i] == null) continue;
                    int tickVal = maxValue - i;
                    bool isCurrent = (tickVal == val);
                    tickLabelTexts[i].color = isCurrent ? new Color(1f, 0.95f, 0.3f, 1f) : new Color(0.7f, 0.8f, 0.9f, 0.7f);
                    tickLabelTexts[i].transform.localScale = isCurrent ? Vector3.one * 1.2f : Vector3.one;
                }
            }
        }

        private int YToValue(float y)
        {
            if (y >= zeroY)
            {
                float t = Mathf.InverseLerp(zeroY, topY, y);
                return Mathf.RoundToInt(Mathf.Lerp(0, maxValue, t));
            }
            else
            {
                float t = Mathf.InverseLerp(zeroY, bottomY, y);
                return -Mathf.RoundToInt(Mathf.Lerp(0, Mathf.Abs(minValue), t));
            }
        }

        private float ValueToY(int val)
        {
            if (val >= 0)
            {
                float t = (float)val / maxValue;
                return Mathf.Lerp(zeroY, topY, t);
            }
            else
            {
                float t = Mathf.Abs(val) / (float)Mathf.Abs(minValue);
                return Mathf.Lerp(zeroY, bottomY, t);
            }
        }

        private void SetGemPositionByValue(int val)
        {
            if (sliderGem == null) return;
            sliderGem.anchoredPosition = new Vector2(0, ValueToY(val));
        }

        private void SnapToValue(int val)
        {
            float targetY = ValueToY(val);
            sliderGem.DOAnchorPosY(targetY, 0.22f).SetEase(Ease.OutBack);
        }
    }
}
