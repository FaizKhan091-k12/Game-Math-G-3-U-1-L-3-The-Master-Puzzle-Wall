using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.08f;
        public float pressScale = 0.94f;
        public float transitionDuration = 0.18f;
        public bool hasIdlePulse = false;

        private Vector3 baseScale;
        private Tween currentTween;
        private Tween idlePulseTween;
        private bool isHovered = false;

        private void Awake()
        {
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
        }

        private void Start()
        {
            if (hasIdlePulse)
            {
                StartIdlePulse();
            }
        }

        public void StartIdlePulse()
        {
            idlePulseTween?.Kill();
            transform.localScale = baseScale;
            idlePulseTween = transform.DOScale(baseScale * 1.06f, 1.1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            idlePulseTween?.Pause();
            currentTween?.Kill();
            currentTween = transform.DOScale(baseScale * hoverScale, transitionDuration).SetEase(Ease.OutBack);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            currentTween?.Kill();
            currentTween = transform.DOScale(baseScale, transitionDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (hasIdlePulse && !isHovered)
                {
                    StartIdlePulse();
                }
            });
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            currentTween?.Kill();
            currentTween = transform.DOScale(baseScale * pressScale, 0.08f).SetEase(Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            currentTween?.Kill();
            currentTween = transform.DOScale(baseScale * (isHovered ? hoverScale : 1f), transitionDuration).SetEase(Ease.OutBack);
        }

        private void OnDisable()
        {
            currentTween?.Kill();
            idlePulseTween?.Kill();
            transform.localScale = baseScale;
        }
    }
}
