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
        public TextMeshProUGUI subLabelText;
        public Image blockImage;
        public Image borderGlow;

        private IceShelfTask shelfTask;
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private bool isLocked = false;
        private Vector3 startDragPosition;
        private Vector2 pointerOffset;

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
            ResetItem();

            if (valueText != null)
            {
                string signStr = value > 0 ? "+" + value : value.ToString();
                valueText.text = $"<b>{signStr}</b>";
                valueText.color = value < 0 ? new Color(0.35f, 0.95f, 1f, 1f) : new Color(1f, 0.92f, 0.35f, 1f);
            }

            if (subLabelText != null)
            {
                if (value == -4) subLabelText.text = "COLDEST";
                else if (value == -1) subLabelText.text = "COLD";
                else subLabelText.text = "WARM";
                subLabelText.color = value < 0 ? new Color(0.6f, 0.9f, 1f, 0.9f) : new Color(1f, 0.95f, 0.7f, 0.9f);
            }
        }

        public void ResetItem()
        {
            isLocked = false;
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (borderGlow != null)
            {
                borderGlow.DOKill();
                borderGlow.gameObject.SetActive(false);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked) return;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            startDragPosition = transform.position;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
            rectTransform.DOKill();
            rectTransform.DOScale(1.18f, 0.15f).SetEase(Ease.OutBack);

            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, cam, out Vector3 worldPos))
            {
                transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            }

            if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, cam, out Vector3 worldPos))
            {
                transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            }
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
                borderGlow.color = new Color(1f, 0.88f, 0.25f, 0.95f);
                borderGlow.DOKill();
                borderGlow.DOFade(0.4f, 0.8f).SetLoops(-1, LoopType.Yoyo);
            }
            rectTransform.DOKill();
            rectTransform.DOScale(1.08f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                rectTransform.DOScale(1.0f, 0.15f);
            });
        }
    }
}
