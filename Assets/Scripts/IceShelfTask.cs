using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class IceShelfTask : MonoBehaviour
    {
        [Header("Shelf Slots")]
        public RectTransform[] slotPositions; // 3 slot anchors horizontally
        public Image shelfGlowImage;

        [Header("Key Blocks")]
        public KeyBlockItem[] keyBlocks; // 3 blocks: values 2, -4, -1

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        public void InitializeTask()
        {
            isCompleted = false;
            if (shelfGlowImage != null)
            {
                shelfGlowImage.color = new Color(1f, 0.85f, 0.3f, 0.2f);
                shelfGlowImage.DOFade(0.7f, 1f).SetLoops(-1, LoopType.Yoyo);
            }

            // Initial scrambled arrangement: Slot 0 = 2, Slot 1 = -4, Slot 2 = -1
            int[] initialValues = new int[] { 2, -4, -1 };
            for (int i = 0; i < keyBlocks.Length && i < slotPositions.Length; i++)
            {
                keyBlocks[i].Setup(this, initialValues[i], i);
                keyBlocks[i].transform.position = slotPositions[i].position;
            }
        }

        public void OnBlockDropped(KeyBlockItem draggedBlock)
        {
            if (isCompleted) return;

            // Find closest slot
            int bestSlot = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < slotPositions.Length; i++)
            {
                float d = Vector2.Distance(draggedBlock.transform.position, slotPositions[i].position);
                if (d < minDistance)
                {
                    minDistance = d;
                    bestSlot = i;
                }
            }

            // Find existing block in bestSlot
            KeyBlockItem occupant = null;
            foreach (var block in keyBlocks)
            {
                if (block != draggedBlock && block.currentSlotIndex == bestSlot)
                {
                    occupant = block;
                    break;
                }
            }

            if (occupant != null)
            {
                // Swap slots
                int oldSlot = draggedBlock.currentSlotIndex;
                occupant.currentSlotIndex = oldSlot;
                occupant.transform.DOMove(slotPositions[oldSlot].position, 0.25f).SetEase(Ease.OutBack);
            }

            draggedBlock.currentSlotIndex = bestSlot;
            draggedBlock.transform.DOMove(slotPositions[bestSlot].position, 0.25f).SetEase(Ease.OutBack);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySnap();

            CheckOrder();
        }

        private void CheckOrder()
        {
            // Build current ordered list by slot 0, 1, 2
            int[] currentOrder = new int[3];
            foreach (var block in keyBlocks)
            {
                if (block.currentSlotIndex >= 0 && block.currentSlotIndex < 3)
                {
                    currentOrder[block.currentSlotIndex] = block.value;
                }
            }

            // Correct order: -4, -1, 2
            if (currentOrder[0] == -4 && currentOrder[1] == -1 && currentOrder[2] == 2)
            {
                Success();
            }
        }

        private void Success()
        {
            isCompleted = true;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayChime();
                SoundManager.Instance.PlayMagicHum();
            }

            if (shelfGlowImage != null)
            {
                shelfGlowImage.DOKill();
                shelfGlowImage.color = new Color(1f, 0.9f, 0.2f, 1f);
                shelfGlowImage.rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3, 0.5f);
            }

            foreach (var block in keyBlocks)
            {
                block.SetLocked();
            }

            onTaskCompleted?.Invoke();
        }
    }
}
