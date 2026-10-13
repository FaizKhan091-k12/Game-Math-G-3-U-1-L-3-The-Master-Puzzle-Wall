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
        public Image slotFrostGlow;

        [Header("Readout & Labels")]
        public TextMeshProUGUI currentValueText;
        public GameObject zeroLineHighlight;

        [Header("Track Limits")]
        public float topY = 180f;     // Represents +3
        public float zeroY = 60f;     // Represents 0
        public float bottomY = -220f; // Represents -6
        public int minValue = -6;
        public int maxValue = 3;
        public int targetValue = -4;

        [Header("Status")]
        public bool isLocked = false;
        public Action onTaskCompleted;

        private Canvas rootCanvas;
        private int currentVal = 0;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void InitializeTask()
        {
            isLocked = false;
            currentVal = 0;
            SetGemPositionByValue(0);
            UpdateReadout();

            if (slotFrostGlow != null)
            {
                slotFrostGlow.gameObject.SetActive(true);
                slotFrostGlow.DOFade(0.85f, 1f).SetLoops(-1, LoopType.Yoyo);
            }

            if (sliderGem != null)
            {
                sliderGem.DOPunchScale(Vector3.one * 0.25f, 0.6f, 3, 0.4f);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            if (SoundManager.Instance != null) SoundManager.Instance.PlayIceSlide();
            sliderGem.DOScale(1.15f, 0.15f);
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

            // Calculate current value based on Y
            int calculatedVal = YToValue(clampedY);
            if (calculatedVal != currentVal)
            {
                currentVal = calculatedVal;
                UpdateReadout();
                if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            sliderGem.DOScale(1.0f, 0.15f);

            // Snap gem smoothly to closest tick
            SnapToValue(currentVal);

            // Check if correct (-4)
            if (currentVal == targetValue)
            {
                LockSlotSuccess();
            }
            else
            {
                // Play gentle reminder feedback
                if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
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
            sliderGem.DOAnchorPosY(targetY, 0.25f).SetEase(Ease.OutBack);
        }

        private void UpdateReadout()
        {
            if (currentValueText != null)
            {
                string label = currentVal < 0 ? "FROST:" : "TEMP:";
                string valStr = currentVal > 0 ? "+" + currentVal : currentVal.ToString();
                currentValueText.text = $"<size=80%>{label}</size> <b>{valStr}</b>";
                currentValueText.color = currentVal < 0 ? new Color(0.4f, 0.85f, 1f) : new Color(1f, 0.8f, 0.4f);
            }
        }

        private void LockSlotSuccess()
        {
            isLocked = true;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySnap();
                SoundManager.Instance.PlayChime();
            }

            sliderGem.DOPunchScale(Vector3.one * 0.35f, 0.6f, 4, 0.5f);
            if (sliderGemGlow != null)
            {
                sliderGemGlow.color = new Color(0.2f, 1f, 0.9f, 1f);
                sliderGemGlow.DOFade(1f, 0.2f);
            }

            onTaskCompleted?.Invoke();
        }
    }
}
