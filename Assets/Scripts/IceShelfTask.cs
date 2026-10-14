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

        [Header("Effects & Feedback")]
        public ParticleSystem confettiParticles;
        public HelperBotController golemBot;

        [Header("State")]
        public bool isCompleted = false;
        public Action onTaskCompleted;

        public void InitializeTask()
        {
            isCompleted = false;
            if (confettiParticles != null) confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (shelfGlowImage != null)
            {
                shelfGlowImage.DOKill();
                shelfGlowImage.rectTransform.localScale = Vector3.one;
                shelfGlowImage.color = new Color(0.2f, 0.8f, 1f, 0.35f);
                shelfGlowImage.DOFade(0.8f, 1.2f).SetLoops(-1, LoopType.Yoyo);
            }

            if (golemBot == null)
            {
                golemBot = FindFirstObjectByType<HelperBotController>();
            }

            if (golemBot != null)
            {
                golemBot.Cheer();
            }

            // Initial scrambled arrangement: Slot 0 = 2, Slot 1 = -4, Slot 2 = -1
            int[] initialValues = new int[] { 2, -4, -1 };
            for (int i = 0; i < keyBlocks.Length && i < slotPositions.Length; i++)
            {
                keyBlocks[i].ResetItem();
                keyBlocks[i].Setup(this, initialValues[i], i);
                keyBlocks[i].transform.DOKill();
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
                occupant.transform.DOKill();
                occupant.transform.DOMove(slotPositions[oldSlot].position, 0.2f).SetEase(Ease.OutBack)
                    .OnComplete(() => { if (occupant != null) occupant.transform.position = slotPositions[oldSlot].position; });
            }

            draggedBlock.currentSlotIndex = bestSlot;
            draggedBlock.transform.DOKill();
            draggedBlock.transform.DOMove(slotPositions[bestSlot].position, 0.2f).SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    if (draggedBlock != null) draggedBlock.transform.position = slotPositions[bestSlot].position;
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySnap();
                    CheckOrder();
                });
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

            // Correct order: -4 (least/coldest), -1 (middle), 2 (greatest/warmest)
            if (currentOrder[0] == -4 && currentOrder[1] == -1 && currentOrder[2] == 2)
            {
                Success();
            }
        }

        private void Success()
        {
            if (isCompleted) return;
            isCompleted = true;

            // Enforce exact alignment on slots
            for (int i = 0; i < keyBlocks.Length && i < slotPositions.Length; i++)
            {
                if (keyBlocks[i] != null && keyBlocks[i].currentSlotIndex >= 0 && keyBlocks[i].currentSlotIndex < slotPositions.Length)
                {
                    keyBlocks[i].transform.DOKill();
                    keyBlocks[i].transform.position = slotPositions[keyBlocks[i].currentSlotIndex].position;
                    keyBlocks[i].SetLocked();
                }
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayChime();
                SoundManager.Instance.PlayFanfare();
            }

            if (confettiParticles != null)
            {
                confettiParticles.Play();
            }

            if (shelfGlowImage != null)
            {
                shelfGlowImage.DOKill();
                shelfGlowImage.color = new Color(1f, 0.88f, 0.2f, 1f);
                shelfGlowImage.rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3, 0.5f);
            }

            if (golemBot != null)
            {
                golemBot.Cheer();
            }

            ScoreManager.Instance?.AddScore(150, "COLD TO WARM ALIGNED!");

            DOVirtual.DelayedCall(1.8f, () =>
            {
                onTaskCompleted?.Invoke();
            });
        }
    }
}
