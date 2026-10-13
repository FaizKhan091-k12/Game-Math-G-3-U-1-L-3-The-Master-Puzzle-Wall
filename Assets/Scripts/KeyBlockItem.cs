using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class KeyBlockItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int value;
        public int currentSlotIndex;
        public TextMeshProUGUI valueText;
        public Image blockImage;
        public Image borderGlow;

        private IceShelfTask shelfTask;
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private bool isLocked = false;
        private Vector3 startDragPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Setup(IceShelfTask task, int val, int slotIndex)
        {
            shelfTask = task;
            value = val;
            currentSlotIndex = slotIndex;
            isLocked = false;

            if (valueText != null)
            {
                valueText.text = (value > 0 ? "+" + value : value.ToString());
                valueText.color = value < 0 ? new Color(0.4f, 0.9f, 1f) : new Color(1f, 0.95f, 0.7f);
            }

            if (borderGlow != null)
            {
                borderGlow.gameObject.SetActive(false);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            startDragPosition = transform.position;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
            rectTransform.DOScale(1.18f, 0.15f);

            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            canvasGroup.blocksRaycasts = true;
            rectTransform.DOScale(1.0f, 0.15f);

            if (shelfTask != null)
            {
                shelfTask.OnBlockDropped(this);
            }
        }

        public void SetLocked()
        {
            isLocked = true;
            if (borderGlow != null)
            {
                borderGlow.gameObject.SetActive(true);
                borderGlow.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            }
            rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 3, 0.4f);
        }
    }
}
