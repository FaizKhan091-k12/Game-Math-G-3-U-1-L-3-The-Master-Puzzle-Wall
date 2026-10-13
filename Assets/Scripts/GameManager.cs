using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MasterPuzzleWall
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Controllers & Helpers")]
        public MainMenuController mainMenu;
        public HelperBotController GolemBot;
        public VictoryPanel victoryPanel;
        public FailPanel failPanel;

        [Header("Top HUD & Prompt Display")]
        public GameObject topHudRoot;
        public TextMeshProUGUI taskTitleText;
        public TextMeshProUGUI loudReaderPromptText;
        public Button homeButton;
        public Button restartTaskButton;

        [Header("Tasks Roots")]
        public FrostSliderTask task1FrostSlider;
        public IceShelfTask task2IceShelf;
        public TwinProngTask task3TwinProng;
        public SquareTileTask task4SquareTile;
        public FactorWheelTask task5FactorWheel;
        public BaseTenCodeTask task6BaseTenCode;

        [Header("Wall Panel Glowing Highlights")]
        public Image leftPanelGlow;    // Glowing highlight for Task 1 & 2
        public Image middlePanelGlow;  // Glowing highlight for Task 3 & 5
        public Image rightPanelGlow;   // Glowing highlight for Task 4
        public Image bottomBarGlow;    // Glowing highlight for Task 6

        [Header("Juice & Effects")]
        public ParticleSystem ambientDustParticles;
        public ParticleSystem taskCompleteBurst;
        public Image screenFlashImage;

        public int CurrentTaskIndex { get; private set; } = -1; // 0 to 5

        private readonly string[] taskTitles = new string[]
        {
            "TASK 1: THE FROST SLOT SLIDER",
            "TASK 2: THE ICE KEY SHELF",
            "TASK 3: THE TWIN PRONG SOCKET",
            "TASK 4: THE SQUARE TILE ARRAY",
            "TASK 5: THE CLEAN FIT FACTOR WHEEL",
            "TASK 6: THE MASTER BASE TEN CODE"
        };

        private readonly string[] loudReaderPrompts = new string[]
        {
            "Look at the left side of the wall! The vertical frost slot is glowing. We need to drop the temperature to exactly 4 steps below the zero mark to clear the ice. Grab the glowing slider gem and pull it straight down past the white zero line until it sits on -4!",
            "Now, look right below the slider at the horizontal stone shelf. Three code keys have appeared out of order. We must line them up from the smallest number to the biggest number. Slide the keys so they go from the lowest minus number up to the normal positive number!",
            "Move your eyes to the lower middle part of the wall. A twin-prong socket is flashing! It needs an even number plug to match its double slots. Look at the tokens: 17 and 24. Which number ends in a balanced, even digit? Tap the right plug token!",
            "Look over at the right side of the wall. A large square space has opened up! We need to fill it with a square number block that has the exact same number of rows and columns. Choose from the blocks below: 12, 16, or 18. Which block forms a perfect square shape?",
            "Look right at the dead center of the wall! The big stone wheel is spinning with 18 slots on its ring. We must plug in a key peg that fits inside 18 perfectly with no empty spaces left behind. Look at pegs 4, 3, and 8. Which divides into 18 with nothing left over?",
            "We are at the final code bar at the very bottom of the wall! The master puzzle window ends with a zero digit [ ___ 0 ]. What is always true about any whole number that ends with a zero? Choose the master rule plaque to open the wall!"
        };

        private readonly string[] successFeedbacks = new string[]
        {
            "Click! Slot locked! You set the track to -4. The minus sign shows we are below zero. The left wall zone is active!",
            "Perfect match! -4 is the smallest, -1 is next, and 2 is the largest. The shelf glows gold!",
            "Pop! Power connected! 24 is an even number because it ends in a 4. The middle circuit is humming!",
            "Snap! A beautiful fit! 16 is a square number because 4 rows of 4 make a perfect square. The right wall panel is complete!",
            "Click! The wheel locks into place! 3 fits inside 18 exactly six times. You found the perfect factor peg!",
            "Hooray! Any number ending in 0 divides cleanly by ten with no remainder. The final lock bar slides back!"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (homeButton != null) homeButton.onClick.AddListener(ShowMainMenu);
            if (restartTaskButton != null) restartTaskButton.onClick.AddListener(() => StartGame(CurrentTaskIndex));
        }

        private void Start()
        {
            HookTaskEvents();
            ShowMainMenu();
        }

        private void HookTaskEvents()
        {
            if (task1FrostSlider != null) task1FrostSlider.onTaskCompleted = () => OnTaskSuccess(0);
            if (task2IceShelf != null) task2IceShelf.onTaskCompleted = () => OnTaskSuccess(1);
            if (task3TwinProng != null) task3TwinProng.onTaskCompleted = () => OnTaskSuccess(2);
            if (task4SquareTile != null) task4SquareTile.onTaskCompleted = () => OnTaskSuccess(3);
            if (task5FactorWheel != null) task5FactorWheel.onTaskCompleted = () => OnTaskSuccess(4);
            if (task6BaseTenCode != null) task6BaseTenCode.onTaskCompleted = () => OnTaskSuccess(5);
        }

        public void ShowMainMenu()
        {
            CurrentTaskIndex = -1;
            HideAllTasks();
            if (topHudRoot != null) topHudRoot.SetActive(false);
            if (victoryPanel != null)
            {
                victoryPanel.ResetWallPositions();
                victoryPanel.gameObject.SetActive(false);
            }
            if (failPanel != null) failPanel.gameObject.SetActive(false);
            if (mainMenu != null) mainMenu.ShowMenu();
        }

        public void StartGame(int startTaskIndex = 0)
        {
            if (mainMenu != null) mainMenu.gameObject.SetActive(false);
            if (victoryPanel != null)
            {
                victoryPanel.ResetWallPositions();
                victoryPanel.gameObject.SetActive(false);
            }
            if (failPanel != null) failPanel.gameObject.SetActive(false);

            if (topHudRoot != null) topHudRoot.SetActive(true);

            LoadTask(startTaskIndex);
        }

        public void LoadTask(int index)
        {
            CurrentTaskIndex = Mathf.Clamp(index, 0, 5);
            HideAllTasks();

            // Update Header HUD
            if (taskTitleText != null) taskTitleText.text = taskTitles[CurrentTaskIndex];
            if (loudReaderPromptText != null) loudReaderPromptText.text = loudReaderPrompts[CurrentTaskIndex];

            // Update Wall Glowing Highlight (moving across panels from left to right)
            UpdateWallPanelHighlights(CurrentTaskIndex);

            // Golem speech prompt
            GolemBot.Cheer();
            GolemBot.Speak(loudReaderPrompts[CurrentTaskIndex], 6.5f);

            // Activate specific task
            switch (CurrentTaskIndex)
            {
                case 0:
                    if (task1FrostSlider != null)
                    {
                        task1FrostSlider.gameObject.SetActive(true);
                        task1FrostSlider.InitializeTask();
                    }
                    break;
                case 1:
                    if (task2IceShelf != null)
                    {
                        task2IceShelf.gameObject.SetActive(true);
                        task2IceShelf.InitializeTask();
                    }
                    break;
                case 2:
                    if (task3TwinProng != null)
                    {
                        task3TwinProng.gameObject.SetActive(true);
                        task3TwinProng.InitializeTask();
                    }
                    break;
                case 3:
                    if (task4SquareTile != null)
                    {
                        task4SquareTile.gameObject.SetActive(true);
                        task4SquareTile.InitializeTask();
                    }
                    break;
                case 4:
                    if (task5FactorWheel != null)
                    {
                        task5FactorWheel.gameObject.SetActive(true);
                        task5FactorWheel.InitializeTask();
                    }
                    break;
                case 5:
                    if (task6BaseTenCode != null)
                    {
                        task6BaseTenCode.gameObject.SetActive(true);
                        task6BaseTenCode.InitializeTask();
                    }
                    break;
            }
        }

        private void UpdateWallPanelHighlights(int taskIdx)
        {
            SetHighlight(leftPanelGlow, taskIdx == 0 || taskIdx == 1);
            SetHighlight(middlePanelGlow, taskIdx == 2 || taskIdx == 4);
            SetHighlight(rightPanelGlow, taskIdx == 3);
            SetHighlight(bottomBarGlow, taskIdx == 5);
        }

        private void SetHighlight(Image glowImg, bool active)
        {
            if (glowImg == null) return;
            glowImg.DOKill();
            if (active)
            {
                glowImg.gameObject.SetActive(true);
                glowImg.color = new Color(glowImg.color.r, glowImg.color.g, glowImg.color.b, 0.25f);
                glowImg.DOFade(0.85f, 0.9f).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                glowImg.DOFade(0f, 0.4f).OnComplete(() => glowImg.gameObject.SetActive(false));
            }
        }

        public void TriggerScreenFlash(Color color)
        {
            if (screenFlashImage == null) return;
            screenFlashImage.color = color;
            screenFlashImage.DOKill();
            screenFlashImage.DOFade(0.4f, 0.1f).OnComplete(() => screenFlashImage.DOFade(0f, 0.35f));
        }

        private void OnTaskSuccess(int taskIdx)
        {
            StartCoroutine(TaskSuccessSequence(taskIdx));
        }

        private IEnumerator TaskSuccessSequence(int taskIdx)
        {
            TriggerScreenFlash(new Color(0.3f, 1f, 0.6f, 0.4f));
            if (taskCompleteBurst != null) taskCompleteBurst.Play();

            GolemBot.Cheer();
            GolemBot.Speak(successFeedbacks[taskIdx], 4.5f);

            yield return new WaitForSeconds(3.5f);

            if (taskIdx < 5)
            {
                LoadTask(taskIdx + 1);
            }
            else
            {
                // All 6 tasks solved! Show victory
                TriggerVictory();
            }
        }

        private void TriggerVictory()
        {
            HideAllTasks();
            if (topHudRoot != null) topHudRoot.SetActive(false);
            if (victoryPanel != null) victoryPanel.ShowVictory();
        }

        public void TriggerFailLockdown()
        {
            if (failPanel != null)
            {
                failPanel.ShowLockdown(() => StartGame(CurrentTaskIndex));
            }
        }

        private void HideAllTasks()
        {
            if (task1FrostSlider != null) task1FrostSlider.gameObject.SetActive(false);
            if (task2IceShelf != null) task2IceShelf.gameObject.SetActive(false);
            if (task3TwinProng != null) task3TwinProng.gameObject.SetActive(false);
            if (task4SquareTile != null) task4SquareTile.gameObject.SetActive(false);
            if (task5FactorWheel != null) task5FactorWheel.gameObject.SetActive(false);
            if (task6BaseTenCode != null) task6BaseTenCode.gameObject.SetActive(false);
        }
    }
}