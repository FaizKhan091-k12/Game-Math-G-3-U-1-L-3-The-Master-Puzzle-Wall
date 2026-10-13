using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace MasterPuzzleWall.Editor
{
    public static class PuzzleWallSceneBuilder
    {
        private const int SCENE_BUILD_VERSION = 8;

        [InitializeOnLoadMethod]
        private static void AutoBuildOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                    return;

                string scenePath = "Assets/Scenes/MasterPuzzleWall.unity";
                int savedVer = EditorPrefs.GetInt("MasterPuzzleWall_SceneVer", 0);
                if (!File.Exists(scenePath) || savedVer < SCENE_BUILD_VERSION)
                {
                    EditorPrefs.SetInt("MasterPuzzleWall_SceneVer", SCENE_BUILD_VERSION);
                    BuildScene();
                }
            };
        }

        [MenuItem("Tools/Master Puzzle Wall/2. Build Master Puzzle Wall Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            {
                Debug.LogWarning("[MasterPuzzleWall] Scene build skipped because Editor is in or entering Play Mode.");
                return;
            }

            // 1. Ensure assets exist and transparency is processed
            WhiteBackgroundRemover.ProcessAllWhiteBackgrounds();
            ProceduralAssetGenerator.GenerateAllAssets();

            // 2. Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.position = new Vector3(0, 0, -10f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camObj.AddComponent<AudioListener>();

            // 4. EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();

            // 5. Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 0;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // SoundManager
            GameObject smObj = new GameObject("SoundManager");
            SoundManager sm = smObj.AddComponent<SoundManager>();
            AssignAudioClips(sm);

            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();

            // 6. UI Hierarchy
            // Background Static Wall
            Sprite wallBgSprite = LoadSprite("puzzle_wall_bg");
            GameObject wallBgObj = CreateUIElement("StaticPuzzleWall_BG", canvasObj.transform);
            RectTransform wallBgRt = wallBgObj.GetComponent<RectTransform>();
            SetStretchFull(wallBgRt);
            Image wallBgImg = wallBgObj.AddComponent<Image>();
            if (wallBgSprite != null) wallBgImg.sprite = wallBgSprite;
            wallBgImg.color = Color.white;

            // Wall Panel Glowing Highlights
            gm.leftPanelGlow = CreatePanelGlow("LeftPanel_Glow", canvasObj.transform, new Vector2(-600, 0), new Vector2(500, 900), new Color(0.2f, 0.7f, 1f, 0f));
            gm.middlePanelGlow = CreatePanelGlow("MiddlePanel_Glow", canvasObj.transform, new Vector2(0, 0), new Vector2(650, 900), new Color(1f, 0.85f, 0.2f, 0f));
            gm.rightPanelGlow = CreatePanelGlow("RightPanel_Glow", canvasObj.transform, new Vector2(600, 0), new Vector2(500, 900), new Color(0.2f, 1f, 0.4f, 0f));
            gm.bottomBarGlow = CreatePanelGlow("BottomBar_Glow", canvasObj.transform, new Vector2(0, -420), new Vector2(1700, 180), new Color(1f, 0.85f, 0.3f, 0f));

            // Screen Flash
            GameObject flashObj = CreateUIElement("ScreenFlashOverlay", canvasObj.transform);
            SetStretchFull(flashObj.GetComponent<RectTransform>());
            Image flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(1, 1, 1, 0);
            flashImg.raycastTarget = false;
            gm.screenFlashImage = flashImg;

            // Particles Container
            GameObject particlesRoot = new GameObject("ParticleEffects");
            particlesRoot.transform.SetParent(canvasObj.transform, false);
            Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ParticleGlow.mat");
            gm.ambientDustParticles = CreateAmbientDust(particlesRoot.transform, particleMat);
            gm.taskCompleteBurst = CreateTaskBurst(particlesRoot.transform, particleMat);

            // Golem Bot & Helper Controller
            gm.GolemBot = CreateHelperBot(canvasObj.transform);

            // Top HUD
            CreateTopHUD(canvasObj.transform, gm);

            // Tasks Container
            GameObject tasksRoot = CreateUIElement("Tasks_Container", canvasObj.transform);
            SetStretchFull(tasksRoot.GetComponent<RectTransform>());

            // Task 1: Frost Slider
            gm.task1FrostSlider = CreateTask1FrostSlider(tasksRoot.transform);

            // Task 2: Ice Key Shelf
            gm.task2IceShelf = CreateTask2IceShelf(tasksRoot.transform);

            // Task 3: Twin Prong Socket
            gm.task3TwinProng = CreateTask3TwinProng(tasksRoot.transform);

            // Task 4: Square Tile Array
            gm.task4SquareTile = CreateTask4SquareTile(tasksRoot.transform);

            // Task 5: Factor Wheel
            gm.task5FactorWheel = CreateTask5FactorWheel(tasksRoot.transform);

            // Task 6: Master Base Ten Code
            gm.task6BaseTenCode = CreateTask6BaseTenCode(tasksRoot.transform);

            // Victory Panel
            gm.victoryPanel = CreateVictoryPanel(canvasObj.transform);

            // Fail Panel
            gm.failPanel = CreateFailPanel(canvasObj.transform);

            // Main Menu
            gm.mainMenu = CreateMainMenu(canvasObj.transform);

            // 7. Save Scene
            string scenePath = "Assets/Scenes/MasterPuzzleWall.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
            Debug.Log("<color=#00FF88><b>[MasterPuzzleWall]</b> Scene successfully built and saved to " + scenePath + " and SampleScene.unity!</color>");

            // Set in build settings
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static HelperBotController CreateHelperBot(Transform parent)
        {
            GameObject botRoot = CreateUIElement("HelperBot_Golem", parent);
            RectTransform botRt = botRoot.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0, 0);
            botRt.anchorMax = new Vector2(0, 0);
            botRt.pivot = new Vector2(0.5f, 0.5f);
            botRt.anchoredPosition = new Vector2(170, 170);
            botRt.sizeDelta = new Vector2(280, 280);

            HelperBotController bot = botRoot.AddComponent<HelperBotController>();
            bot.botRect = botRt;

            Image botImg = botRoot.AddComponent<Image>();
            bot.botImage = botImg;
            bot.normalSprite = LoadSprite("stone_golem_bot");
            bot.confusedSprite = LoadSprite("stone_golem_confused");
            if (bot.normalSprite != null) botImg.sprite = bot.normalSprite;

            // Speech Bubble
            GameObject bubbleObj = CreateUIElement("SpeechBubble", botRoot.transform);
            RectTransform bubbleRt = bubbleObj.GetComponent<RectTransform>();
            bubbleRt.anchorMin = new Vector2(0.5f, 1f);
            bubbleRt.anchorMax = new Vector2(0.5f, 1f);
            bubbleRt.pivot = new Vector2(0f, 0f);
            bubbleRt.anchoredPosition = new Vector2(20, 20);
            bubbleRt.sizeDelta = new Vector2(400, 170);

            ProceduralImage bubbleBg = bubbleObj.AddComponent<ProceduralImage>();
            bubbleBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            bubbleBg.BorderWidth = 0f;
            UniformModifier um = bubbleObj.AddComponent<UniformModifier>();
            um.Radius = 18f;

            CanvasGroup bubbleCg = bubbleObj.AddComponent<CanvasGroup>();
            bot.speechBubbleRoot = bubbleObj;
            bot.speechBubbleGroup = bubbleCg;

            // Speaker Label
            GameObject labelObj = CreateUIElement("SpeakerLabel", bubbleObj.transform);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 1);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.pivot = new Vector2(0.5f, 1);
            labelRt.anchoredPosition = new Vector2(0, -10);
            labelRt.sizeDelta = new Vector2(-30, 30);
            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "<b><color=#5EEAD4>GOLEM HELPER BOT:</color></b>";
            labelTmp.fontSize = 20;

            // Speech Text
            GameObject textObj = CreateUIElement("SpeechText", bubbleObj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = new Vector2(0, -15);
            textRt.sizeDelta = new Vector2(-30, -50);
            TextMeshProUGUI speechTmp = textObj.AddComponent<TextMeshProUGUI>();
            speechTmp.text = "Hello friend! Let's solve the master puzzle!";
            speechTmp.fontSize = 20;
            speechTmp.textWrappingMode = TextWrappingModes.Normal;
            speechTmp.color = Color.white;
            bot.speechText = speechTmp;

            return bot;
        }

        private static void CreateTopHUD(Transform parent, GameManager gm)
        {
            GameObject hudRoot = CreateUIElement("TopHUD", parent);
            RectTransform hudRt = hudRoot.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0.5f, 1f);
            hudRt.anchorMax = new Vector2(0.5f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0, -15);
            hudRt.sizeDelta = new Vector2(1780, 140);
            gm.topHudRoot = hudRoot;

            // Procedural Background
            ProceduralImage hudBg = hudRoot.AddComponent<ProceduralImage>();
            hudBg.color = new Color(0.08f, 0.1f, 0.15f, 0.92f);
            hudBg.BorderWidth = 0f;
            UniformModifier um = hudRoot.AddComponent<UniformModifier>();
            um.Radius = 16f;

            // Task Title
            GameObject titleObj = CreateUIElement("TaskTitle", hudRoot.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0, -10);
            titleRt.sizeDelta = new Vector2(1400, 38);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "TASK 1: THE FROST SLOT SLIDER";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.88f, 0.4f);
            gm.taskTitleText = titleTmp;

            // Loud Reader Prompt Box
            GameObject promptObj = CreateUIElement("LoudReaderPrompt", hudRoot.transform);
            RectTransform promptRt = promptObj.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0f);
            promptRt.anchorMax = new Vector2(0.5f, 0f);
            promptRt.pivot = new Vector2(0.5f, 0f);
            promptRt.anchoredPosition = new Vector2(0, 12);
            promptRt.sizeDelta = new Vector2(1480, 75);
            TextMeshProUGUI promptTmp = promptObj.AddComponent<TextMeshProUGUI>();
            promptTmp.text = "Prompt text for loud reader or student...";
            promptTmp.fontSize = 22;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.92f, 0.96f, 1f);
            promptTmp.textWrappingMode = TextWrappingModes.Normal;
            gm.loudReaderPromptText = promptTmp;

            // Home Button
            GameObject homeObj = CreateButton("HomeBtn", hudRoot.transform, new Vector2(-820, 0), new Vector2(100, 60), "HOME", 20);
            gm.homeButton = homeObj.GetComponent<Button>();

            // Restart Task Button
            GameObject restartObj = CreateButton("RestartBtn", hudRoot.transform, new Vector2(820, 0), new Vector2(100, 60), "RESET", 20);
            gm.restartTaskButton = restartObj.GetComponent<Button>();
        }

        private static FrostSliderTask CreateTask1FrostSlider(Transform parent)
        {
            GameObject root = CreateUIElement("Task1_FrostSlider", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-610, 0);
            rootRt.sizeDelta = new Vector2(280, 680);

            FrostSliderTask task = root.AddComponent<FrostSliderTask>();

            // Track Background
            GameObject trackObj = CreateUIElement("FrostTrack", root.transform);
            RectTransform trackRt = trackObj.GetComponent<RectTransform>();
            trackRt.sizeDelta = new Vector2(90, 520);
            ProceduralImage trackBg = trackObj.AddComponent<ProceduralImage>();
            trackBg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);
            trackBg.BorderWidth = 0f;
            UniformModifier um = trackObj.AddComponent<UniformModifier>();
            um.Radius = 45f;
            task.trackRect = trackRt;

            // Subzero frozen blue tint zone
            GameObject frozenZoneObj = CreateUIElement("FrozenZone", trackObj.transform);
            RectTransform frozenZoneRt = frozenZoneObj.GetComponent<RectTransform>();
            frozenZoneRt.anchorMin = new Vector2(0, 0);
            frozenZoneRt.anchorMax = new Vector2(1, 0.45f);
            frozenZoneRt.sizeDelta = Vector2.zero;
            Image frozenImg = frozenZoneObj.AddComponent<Image>();
            frozenImg.color = new Color(0.1f, 0.5f, 0.9f, 0.35f);

            // Zero Line Highlight
            GameObject zeroLineObj = CreateUIElement("ZeroLine", trackObj.transform);
            RectTransform zeroLineRt = zeroLineObj.GetComponent<RectTransform>();
            zeroLineRt.anchoredPosition = new Vector2(0, 60);
            zeroLineRt.sizeDelta = new Vector2(120, 6);
            Image zeroLineImg = zeroLineObj.AddComponent<Image>();
            zeroLineImg.color = Color.white;
            task.zeroLineHighlight = zeroLineObj;

            // Zero label text
            GameObject zeroLabelObj = CreateUIElement("ZeroLabel", zeroLineObj.transform);
            RectTransform zeroLabelRt = zeroLabelObj.GetComponent<RectTransform>();
            zeroLabelRt.anchoredPosition = new Vector2(85, 0);
            TextMeshProUGUI zeroTmp = zeroLabelObj.AddComponent<TextMeshProUGUI>();
            zeroTmp.text = "<b>0 (ZERO)</b>";
            zeroTmp.fontSize = 20;
            zeroTmp.color = Color.white;

            // Target -4 Label
            GameObject targetLabelObj = CreateUIElement("TargetMinus4Label", trackObj.transform);
            RectTransform targetLabelRt = targetLabelObj.GetComponent<RectTransform>();
            targetLabelRt.anchoredPosition = new Vector2(100, -125);
            TextMeshProUGUI targetTmp = targetLabelObj.AddComponent<TextMeshProUGUI>();
            targetTmp.text = "<b><color=#38BDF8>GOAL: -4</color></b>";
            targetTmp.fontSize = 22;

            // Slider Gem
            GameObject gemObj = CreateUIElement("SliderGem", trackObj.transform);
            RectTransform gemRt = gemObj.GetComponent<RectTransform>();
            gemRt.sizeDelta = new Vector2(95, 95);
            gemRt.anchoredPosition = new Vector2(0, 60);
            Image gemImg = gemObj.AddComponent<Image>();
            Sprite gemSprite = LoadSprite("glowing_starter_gem");
            if (gemSprite != null) gemImg.sprite = gemSprite;
            task.sliderGem = gemRt;
            task.sliderGemGlow = gemImg;

            // Readout Text
            GameObject readoutObj = CreateUIElement("ReadoutText", root.transform);
            RectTransform readoutRt = readoutObj.GetComponent<RectTransform>();
            readoutRt.anchoredPosition = new Vector2(0, -300);
            readoutRt.sizeDelta = new Vector2(250, 60);
            TextMeshProUGUI readoutTmp = readoutObj.AddComponent<TextMeshProUGUI>();
            readoutTmp.fontSize = 26;
            readoutTmp.alignment = TextAlignmentOptions.Center;
            task.currentValueText = readoutTmp;

            return task;
        }

        private static IceShelfTask CreateTask2IceShelf(Transform parent)
        {
            GameObject root = CreateUIElement("Task2_IceShelf", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-600, -180);
            rootRt.sizeDelta = new Vector2(500, 240);

            IceShelfTask task = root.AddComponent<IceShelfTask>();

            // Shelf Base Image
            GameObject shelfBaseObj = CreateUIElement("ShelfBase", root.transform);
            RectTransform shelfBaseRt = shelfBaseObj.GetComponent<RectTransform>();
            shelfBaseRt.sizeDelta = new Vector2(460, 160);
            ProceduralImage shelfImg = shelfBaseObj.AddComponent<ProceduralImage>();
            shelfImg.color = new Color(0.12f, 0.15f, 0.2f, 0.9f);
            shelfImg.BorderWidth = 0f;
            shelfBaseObj.AddComponent<UniformModifier>().Radius = 16f;
            task.shelfGlowImage = shelfImg;

            // 3 Slot Anchors
            task.slotPositions = new RectTransform[3];
            float[] slotX = new float[] { -140f, 0f, 140f };
            string[] slotLabels = new string[] { "COLDEST\n(Least)", "MIDDLE", "WARMEST\n(Greatest)" };

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = CreateUIElement("Slot_" + i, shelfBaseObj.transform);
                RectTransform slotRt = slotObj.GetComponent<RectTransform>();
                slotRt.anchoredPosition = new Vector2(slotX[i], 10);
                slotRt.sizeDelta = new Vector2(110, 110);
                task.slotPositions[i] = slotRt;

                ProceduralImage sImg = slotObj.AddComponent<ProceduralImage>();
                sImg.color = new Color(0.06f, 0.08f, 0.12f, 0.7f);
                sImg.BorderWidth = 0f;
                slotObj.AddComponent<UniformModifier>().Radius = 12f;

                // Slot label
                GameObject lblObj = CreateUIElement("Label", slotObj.transform);
                RectTransform lblRt = lblObj.GetComponent<RectTransform>();
                lblRt.anchoredPosition = new Vector2(0, -68);
                lblRt.sizeDelta = new Vector2(130, 40);
                TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
                lblTmp.text = slotLabels[i];
                lblTmp.fontSize = 15;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.color = new Color(0.8f, 0.9f, 1f, 0.8f);
            }

            // 3 Draggable Key Blocks
            task.keyBlocks = new KeyBlockItem[3];
            Sprite tileSprite = LoadSprite("stone_rune_tile");
            for (int i = 0; i < 3; i++)
            {
                GameObject blockObj = CreateUIElement("BlockItem_" + i, root.transform);
                RectTransform blockRt = blockObj.GetComponent<RectTransform>();
                blockRt.sizeDelta = new Vector2(100, 100);

                KeyBlockItem block = blockObj.AddComponent<KeyBlockItem>();
                Image blockImg = blockObj.AddComponent<Image>();
                if (tileSprite != null) blockImg.sprite = tileSprite;
                block.blockImage = blockImg;

                GameObject valObj = CreateUIElement("ValueText", blockObj.transform);
                RectTransform valRt = valObj.GetComponent<RectTransform>();
                valRt.sizeDelta = new Vector2(90, 90);
                TextMeshProUGUI valTmp = valObj.AddComponent<TextMeshProUGUI>();
                valTmp.fontSize = 42;
                valTmp.fontStyle = FontStyles.Bold;
                valTmp.alignment = TextAlignmentOptions.Center;
                block.valueText = valTmp;

                task.keyBlocks[i] = block;
            }

            return task;
        }

        private static TwinProngTask CreateTask3TwinProng(Transform parent)
        {
            GameObject root = CreateUIElement("Task3_TwinProng", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0, -220);
            rootRt.sizeDelta = new Vector2(500, 320);

            TwinProngTask task = root.AddComponent<TwinProngTask>();

            // Socket Anchor
            GameObject socketObj = CreateUIElement("TwinSocketAnchor", root.transform);
            RectTransform socketRt = socketObj.GetComponent<RectTransform>();
            socketRt.anchoredPosition = new Vector2(0, 70);
            socketRt.sizeDelta = new Vector2(170, 130);
            task.socketAnchor = socketRt;

            ProceduralImage socketBg = socketObj.AddComponent<ProceduralImage>();
            socketBg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            socketBg.BorderWidth = 0f;
            socketObj.AddComponent<UniformModifier>().Radius = 20f;
            task.socketBlinkGlow = socketBg;

            // Double slot holes inside socket
            CreateSocketHole(socketObj.transform, new Vector2(-35, 10));
            CreateSocketHole(socketObj.transform, new Vector2(35, 10));

            // Socket hint text
            GameObject hintObj = CreateUIElement("SocketHint", socketObj.transform);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchoredPosition = new Vector2(0, -42);
            hintRt.sizeDelta = new Vector2(160, 30);
            TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "<b>TWIN SLOTS (EVEN)</b>";
            hintTmp.fontSize = 14;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.4f, 0.9f, 1f);

            // Two Plug Tokens: 17 and 24
            Sprite plugSprite = LoadSprite("stone_twin_plug");

            task.token17Rect = CreatePlugToken(root.transform, "Token_17", new Vector2(-150, -70), "17", plugSprite, out task.token17Button);
            task.token24Rect = CreatePlugToken(root.transform, "Token_24", new Vector2(150, -70), "24", plugSprite, out task.token24Button);

            return task;
        }

        private static RectTransform CreatePlugToken(Transform parent, string name, Vector2 pos, string val, Sprite sprite, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(140, 140);

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("ValText", obj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -10);
            textRt.sizeDelta = new Vector2(120, 60);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{val}</b>";
            tmp.fontSize = 44;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.7f);

            return rt;
        }

        private static void CreateSocketHole(Transform parent, Vector2 pos)
        {
            GameObject hole = CreateUIElement("ProngHole", parent);
            RectTransform rt = hole.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(24, 45);
            ProceduralImage pImg = hole.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.02f, 0.03f, 0.05f, 1f);
            pImg.BorderWidth = 0f;
            hole.AddComponent<UniformModifier>().Radius = 8f;
        }

        private static SquareTileTask CreateTask4SquareTile(Transform parent)
        {
            GameObject root = CreateUIElement("Task4_SquareTile", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(600, 0);
            rootRt.sizeDelta = new Vector2(480, 680);

            SquareTileTask task = root.AddComponent<SquareTileTask>();

            // Large Square Slot Target
            GameObject slotObj = CreateUIElement("SquareSlotTarget", root.transform);
            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchoredPosition = new Vector2(0, 140);
            slotRt.sizeDelta = new Vector2(230, 230);
            task.squareSlotTarget = slotRt;

            ProceduralImage slotBg = slotObj.AddComponent<ProceduralImage>();
            slotBg.color = new Color(0.08f, 0.14f, 0.1f, 0.9f);
            slotBg.BorderWidth = 0f;
            slotObj.AddComponent<UniformModifier>().Radius = 14f;
            task.slotGreenGlow = slotBg;

            // Indentation text
            GameObject slotTextObj = CreateUIElement("IndentText", slotObj.transform);
            RectTransform slotTextRt = slotTextObj.GetComponent<RectTransform>();
            slotTextRt.sizeDelta = new Vector2(210, 80);
            TextMeshProUGUI slotTmp = slotTextObj.AddComponent<TextMeshProUGUI>();
            slotTmp.text = "<size=75%>SQUARE INDENT</size>\n<b>? x ?</b>";
            slotTmp.fontSize = 24;
            slotTmp.alignment = TextAlignmentOptions.Center;
            slotTmp.color = new Color(0.4f, 1f, 0.6f, 0.7f);

            // 4x4 Grid Display (revealed on solve)
            GameObject grid4x4Obj = CreateUIElement("Grid4x4Display", slotObj.transform);
            RectTransform gridRt = grid4x4Obj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(210, 210);
            task.solved4x4GridDisplay = grid4x4Obj;
            GridLayoutGroup glg = grid4x4Obj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(46, 46);
            glg.spacing = new Vector2(6, 6);
            for (int t = 0; t < 16; t++)
            {
                GameObject tile = CreateUIElement("Tile_" + t, grid4x4Obj.transform);
                ProceduralImage pImg = tile.AddComponent<ProceduralImage>();
                pImg.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
                tile.AddComponent<UniformModifier>().Radius = 4f;
            }

            // 3 Tile Options: 12, 16, 18
            Sprite tileSprite = LoadSprite("stone_rune_tile");
            task.block12Rect = CreateTileChoice(root.transform, "Block_12", new Vector2(-150, -180), "12 Tiles\n<size=70%>(3 x 4)</size>", tileSprite, out task.block12Button);
            task.block16Rect = CreateTileChoice(root.transform, "Block_16", new Vector2(0, -180), "16 Tiles\n<size=70%>(4 x 4)</size>", tileSprite, out task.block16Button);
            task.block18Rect = CreateTileChoice(root.transform, "Block_18", new Vector2(150, -180), "18 Tiles\n<size=70%>(3 x 6)</size>", tileSprite, out task.block18Button);

            return task;
        }

        private static RectTransform CreateTileChoice(Transform parent, string name, Vector2 pos, string label, Sprite sprite, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(130, 130);

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("LabelText", obj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(120, 100);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return rt;
        }

        private static FactorWheelTask CreateTask5FactorWheel(Transform parent)
        {
            GameObject root = CreateUIElement("Task5_FactorWheel", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0, 40);
            rootRt.sizeDelta = new Vector2(600, 600);

            FactorWheelTask task = root.AddComponent<FactorWheelTask>();

            // Center Wheel
            GameObject wheelObj = CreateUIElement("StoneDialWheel", root.transform);
            RectTransform wheelRt = wheelObj.GetComponent<RectTransform>();
            wheelRt.sizeDelta = new Vector2(440, 440);
            task.wheelRect = wheelRt;

            Image wheelImg = wheelObj.AddComponent<Image>();
            Sprite wheelSprite = LoadSprite("stone_dial_wheel");
            if (wheelSprite != null) wheelImg.sprite = wheelSprite;
            task.wheelGlowImage = wheelImg;

            // Factor Indicators Group (6 glowing nodes around perimeter)
            GameObject indObj = CreateUIElement("FactorIndicators", wheelObj.transform);
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            SetStretchFull(indRt);
            task.factorIndicatorsGroup = indObj;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                GameObject node = CreateUIElement("FactorNode_" + i, indObj.transform);
                RectTransform nRt = node.GetComponent<RectTransform>();
                nRt.anchoredPosition = new Vector2(Mathf.Cos(angle) * 165f, Mathf.Sin(angle) * 165f);
                nRt.sizeDelta = new Vector2(28, 28);
                ProceduralImage pImg = node.AddComponent<ProceduralImage>();
                pImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                node.AddComponent<RoundModifier>();
            }

            // Wheel Title Info
            GameObject infoObj = CreateUIElement("WheelInfoText", root.transform);
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchoredPosition = new Vector2(0, -250);
            infoRt.sizeDelta = new Vector2(400, 40);
            TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
            infoTmp.text = "<b>WHEEL SLOTS: 18 | FIND FACTOR PEG</b>";
            infoTmp.fontSize = 20;
            infoTmp.alignment = TextAlignmentOptions.Center;
            infoTmp.color = new Color(1f, 0.88f, 0.4f);

            // Three Peg Tools: 4, 3, 8
            Sprite plugSprite = LoadSprite("stone_twin_plug");
            task.peg4Rect = CreatePegButton(root.transform, "Peg_4", new Vector2(-150, -320), "PEG 4", plugSprite, out task.peg4Button);
            task.peg3Rect = CreatePegButton(root.transform, "Peg_3", new Vector2(0, -320), "PEG 3", plugSprite, out task.peg3Button);
            task.peg8Rect = CreatePegButton(root.transform, "Peg_8", new Vector2(150, -320), "PEG 8", plugSprite, out task.peg8Button);

            return task;
        }

        private static RectTransform CreatePegButton(Transform parent, string name, Vector2 pos, string label, Sprite sprite, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(110, 110);

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("Label", obj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(100, 50);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.92f, 0.6f);

            return rt;
        }

        private static BaseTenCodeTask CreateTask6BaseTenCode(Transform parent)
        {
            GameObject root = CreateUIElement("Task6_BaseTenCode", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0, -380);
            rootRt.sizeDelta = new Vector2(1700, 220);

            BaseTenCodeTask task = root.AddComponent<BaseTenCodeTask>();

            // Lock Bar Prompt
            GameObject barObj = CreateUIElement("LockBarWindow", root.transform);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchoredPosition = new Vector2(0, 45);
            barRt.sizeDelta = new Vector2(1200, 90);
            task.codeBarRect = barRt;

            ProceduralImage barBg = barObj.AddComponent<ProceduralImage>();
            barBg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
            barBg.BorderWidth = 0f;
            barObj.AddComponent<UniformModifier>().Radius = 16f;
            task.codeBarGlow = barBg;

            GameObject promptTextObj = CreateUIElement("PromptText", barObj.transform);
            RectTransform ptRt = promptTextObj.GetComponent<RectTransform>();
            SetStretchFull(ptRt);
            TextMeshProUGUI ptTmp = promptTextObj.AddComponent<TextMeshProUGUI>();
            ptTmp.fontSize = 24;
            ptTmp.alignment = TextAlignmentOptions.Center;
            ptTmp.color = Color.white;
            task.promptDisplayText = ptTmp;

            // 3 Rule Plaques
            task.plaqueOddRect = CreateRulePlaque(root.transform, "Plaque_Odd", new Vector2(-420, -45), "It is always odd", out task.plaqueOddButton);
            task.plaqueDivide10Rect = CreateRulePlaque(root.transform, "Plaque_Divide10", new Vector2(0, -45), "It divides exactly by 10", out task.plaqueDivide10Button);
            task.plaqueLessThan10Rect = CreateRulePlaque(root.transform, "Plaque_LessThan10", new Vector2(420, -45), "It is less than 10", out task.plaqueLessThan10Button);

            return task;
        }

        private static RectTransform CreateRulePlaque(Transform parent, string name, Vector2 pos, string text, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(380, 75);

            ProceduralImage pImg = obj.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            pImg.BorderWidth = 0f;
            obj.AddComponent<UniformModifier>().Radius = 14f;

            btn = obj.AddComponent<Button>();

            GameObject textObj = CreateUIElement("PlaqueText", obj.transform);
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            SetStretchFull(tRt);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return rt;
        }

        private static VictoryPanel CreateVictoryPanel(Transform parent)
        {
            GameObject root = CreateUIElement("VictoryPanel", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            SetStretchFull(rootRt);
            VictoryPanel vp = root.AddComponent<VictoryPanel>();

            // Sliding Wall Halves
            Sprite wallBgSprite = LoadSprite("puzzle_wall_bg");

            GameObject leftHalf = CreateUIElement("WallLeftHalf", root.transform);
            RectTransform lRt = leftHalf.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(0.5f, 1f);
            lRt.sizeDelta = Vector2.zero;
            Image lImg = leftHalf.AddComponent<Image>();
            if (wallBgSprite != null) lImg.sprite = wallBgSprite;
            vp.wallLeftHalf = lRt;

            GameObject rightHalf = CreateUIElement("WallRightHalf", root.transform);
            RectTransform rRt = rightHalf.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 0);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.sizeDelta = Vector2.zero;
            Image rImg = rightHalf.AddComponent<Image>();
            if (wallBgSprite != null) rImg.sprite = wallBgSprite;
            vp.wallRightHalf = rRt;

            // Golden Light Flood
            GameObject goldObj = CreateUIElement("GoldenLightFlood", root.transform);
            SetStretchFull(goldObj.GetComponent<RectTransform>());
            Image gImg = goldObj.AddComponent<Image>();
            gImg.color = new Color(1f, 0.92f, 0.5f, 0.85f);
            vp.goldenLightFlood = gImg;

            // Victory Trophy
            GameObject trophyObj = CreateUIElement("GemstoneTrophy", root.transform);
            RectTransform trRt = trophyObj.GetComponent<RectTransform>();
            trRt.anchoredPosition = new Vector2(0, 180);
            trRt.sizeDelta = new Vector2(360, 360);
            Image trImg = trophyObj.AddComponent<Image>();
            Sprite trSprite = LoadSprite("gemstone_trophy");
            if (trSprite != null) trImg.sprite = trSprite;
            vp.trophyRect = trRt;
            vp.trophyImage = trImg;

            // Victory Title
            GameObject titleObj = CreateUIElement("VictoryTitle", root.transform);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, -60);
            tRt.sizeDelta = new Vector2(1200, 70);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b><color=#FDE047>THE MASTER PUZZLE WALL UNLOCKED!</color></b>";
            tTmp.fontSize = 42;
            tTmp.alignment = TextAlignmentOptions.Center;
            vp.titleText = tTmp;

            // Recap Container
            GameObject recapObj = CreateUIElement("LearningRecapContainer", root.transform);
            RectTransform recapRt = recapObj.GetComponent<RectTransform>();
            recapRt.anchoredPosition = new Vector2(0, -190);
            recapRt.sizeDelta = new Vector2(1400, 160);
            vp.recapContainer = recapObj;

            ProceduralImage recapBg = recapObj.AddComponent<ProceduralImage>();
            recapBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            recapBg.BorderWidth = 0f;
            recapObj.AddComponent<UniformModifier>().Radius = 18f;

            GameObject recapTextObj = CreateUIElement("RecapText", recapObj.transform);
            RectTransform rtText = recapTextObj.GetComponent<RectTransform>();
            SetStretchFull(rtText);
            TextMeshProUGUI rtTmp = recapTextObj.AddComponent<TextMeshProUGUI>();
            rtTmp.text = "<b>MASTER PATTERN SOLVER RECAP</b>\n" +
                         "- <b>Negative Numbers:</b> Below zero use a minus sign and decrease downward.\n" +
                         "- <b>Even Numbers:</b> End in 0, 2, 4, 6, 8 and split evenly into two equal parts.\n" +
                         "- <b>Square Numbers:</b> Form perfect square arrays (4 x 4 = 16) with identical rows and columns.\n" +
                         "- <b>Divisibility:</b> Factors fit evenly with 0 remainder. Any whole number ending in 0 divides cleanly by 10!";
            rtTmp.fontSize = 20;
            rtTmp.alignment = TextAlignmentOptions.Center;
            rtTmp.color = Color.white;

            // Play Again Button
            GameObject playAgainBtn = CreateButton("PlayAgainBtn", root.transform, new Vector2(-150, -350), new Vector2(260, 70), "PLAY AGAIN", 24);
            vp.playAgainButton = playAgainBtn.GetComponent<Button>();

            // Main Menu Button
            GameObject menuBtn = CreateButton("MainMenuBtn", root.transform, new Vector2(150, -350), new Vector2(260, 70), "MAIN MENU", 24);
            vp.mainMenuButton = menuBtn.GetComponent<Button>();

            Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ParticleGlow.mat");
            vp.victorySparkles = CreateVictorySparkles(root.transform, particleMat);

            root.SetActive(false);
            return vp;
        }

        private static FailPanel CreateFailPanel(Transform parent)
        {
            GameObject root = CreateUIElement("FailPanel", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            SetStretchFull(rootRt);
            FailPanel fp = root.AddComponent<FailPanel>();

            // Dull Gray Overlay
            GameObject overlayObj = CreateUIElement("DullGrayOverlay", root.transform);
            SetStretchFull(overlayObj.GetComponent<RectTransform>());
            Image oImg = overlayObj.AddComponent<Image>();
            oImg.color = new Color(0.2f, 0.22f, 0.25f, 0.85f);
            fp.dullGrayOverlay = oImg;

            // Title
            GameObject titleObj = CreateUIElement("LockdownTitle", root.transform);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 120);
            tRt.sizeDelta = new Vector2(1000, 80);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b><color=#FB923C>WALL LOCKDOWN (CIRCUITS RESET)</color></b>";
            tTmp.fontSize = 38;
            tTmp.alignment = TextAlignmentOptions.Center;

            // Retry Button
            GameObject retryBtn = CreateButton("RetryCodeBtn", root.transform, new Vector2(0, -60), new Vector2(300, 80), "RETRY CODE", 28);
            ProceduralImage btnImg = retryBtn.GetComponent<ProceduralImage>();
            if (btnImg != null)
            {
                btnImg.color = new Color(0.9f, 0.45f, 0.1f, 1f);
            }
            fp.retryCodeButton = retryBtn.GetComponent<Button>();

            root.SetActive(false);
            return fp;
        }

        private static MainMenuController CreateMainMenu(Transform parent)
        {
            GameObject root = CreateUIElement("MainMenuRoot", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            SetStretchFull(rootRt);
            MainMenuController mm = root.AddComponent<MainMenuController>();

            // Title Logo
            GameObject logoObj = CreateUIElement("TitleLogo", root.transform);
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.anchoredPosition = new Vector2(0, 260);
            logoRt.sizeDelta = new Vector2(800, 450);
            Image logoImg = logoObj.AddComponent<Image>();
            Sprite logoSprite = LoadSprite("game_title_logo");
            if (logoSprite != null) logoImg.sprite = logoSprite;
            mm.titleLogoRect = logoRt;

            // Glowing Starter Gem (center interactive gem from Screen 1 - the single play button)
            GameObject gemObj = CreateUIElement("StarterGemButton", root.transform);
            RectTransform gemRt = gemObj.GetComponent<RectTransform>();
            gemRt.anchoredPosition = new Vector2(0, -70);
            gemRt.sizeDelta = new Vector2(220, 220);
            Image gemImg = gemObj.AddComponent<Image>();
            Sprite gemSprite = LoadSprite("glowing_starter_gem");
            if (gemSprite != null) gemImg.sprite = gemSprite;
            Button gemBtn = gemObj.AddComponent<Button>();
            HoverScale gemHs = gemObj.AddComponent<HoverScale>();
            gemHs.hasIdlePulse = true;
            gemHs.hoverScale = 1.15f;
            mm.starterGemRect = gemRt;
            mm.starterGemButton = gemBtn;

            // "PRESS GLOWING GEM TO PLAY" Badge Container
            GameObject promptBadge = CreateUIElement("GemPromptBadge", root.transform);
            RectTransform pbRt = promptBadge.GetComponent<RectTransform>();
            pbRt.anchoredPosition = new Vector2(0, -195);
            pbRt.sizeDelta = new Vector2(460, 48);

            ProceduralImage badgeBg = promptBadge.AddComponent<ProceduralImage>();
            badgeBg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
            badgeBg.BorderWidth = 0f;
            promptBadge.AddComponent<UniformModifier>().Radius = 24f;

            Button badgeBtn = promptBadge.AddComponent<Button>();
            ColorBlock bcb = badgeBtn.colors;
            bcb.highlightedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            bcb.pressedColor = new Color(0.04f, 0.06f, 0.10f, 1f);
            badgeBtn.colors = bcb;
            badgeBtn.onClick.AddListener(gemBtn.onClick.Invoke);

            HoverScale badgeHs = promptBadge.AddComponent<HoverScale>();
            badgeHs.hoverScale = 1.06f;

            GameObject gemLabel = CreateUIElement("GemPromptText", promptBadge.transform);
            RectTransform glRt = gemLabel.GetComponent<RectTransform>();
            SetStretchFull(glRt);
            TextMeshProUGUI glTmp = gemLabel.AddComponent<TextMeshProUGUI>();
            glTmp.text = "<b><color=#FDE047>PRESS GLOWING GEM TO PLAY</color></b>";
            glTmp.fontSize = 20;
            glTmp.alignment = TextAlignmentOptions.Center;
            glTmp.color = Color.white;

            mm.gemPromptBadgeRect = pbRt;

            // Level Select Button
            GameObject selectBtnObj = CreateButton("LevelSelectButton", root.transform, new Vector2(-160, -290), new Vector2(280, 68), "SELECT TASK", 22);
            mm.levelSelectButton = selectBtnObj.GetComponent<Button>();

            // Sound Toggle Button
            GameObject soundBtnObj = CreateButton("SoundToggleBtn", root.transform, new Vector2(160, -290), new Vector2(280, 68), "AUDIO: ON", 22);
            mm.soundToggleButton = soundBtnObj.GetComponent<Button>();
            mm.soundToggleText = soundBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Level Select Modal
            CreateLevelSelectModal(root.transform, mm);

            return mm;
        }

        private static void CreateLevelSelectModal(Transform parent, MainMenuController mm)
        {
            GameObject modalObj = CreateUIElement("LevelSelectModal", parent);
            RectTransform modalRt = modalObj.GetComponent<RectTransform>();
            modalRt.sizeDelta = new Vector2(850, 600);

            ProceduralImage modalBg = modalObj.AddComponent<ProceduralImage>();
            modalBg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            modalBg.BorderWidth = 0f;
            modalObj.AddComponent<UniformModifier>().Radius = 24f;

            mm.levelSelectPanel = modalObj;

            // Title
            GameObject titleObj = CreateUIElement("ModalTitle", modalObj.transform);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 240);
            tRt.sizeDelta = new Vector2(800, 50);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b>CHOOSE A PUZZLE WALL TASK</b>";
            tTmp.fontSize = 32;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(1f, 0.88f, 0.4f);

            // 6 Task Buttons
            string[] taskNames = new string[]
            {
                "1. Frost Slot Slider (Negative Numbers)",
                "2. Ice Key Shelf (Ordering -4, -1, 2)",
                "3. Twin Prong Socket (Odd vs Even)",
                "4. Square Tile Array (Square Number 16)",
                "5. Clean Fit Wheel (Factors of 18)",
                "6. Master Base Ten (Divisibility by 10)"
            };

            mm.taskSelectButtons = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float y = 160f - (i * 68f);
                GameObject btnObj = CreateButton("TaskBtn_" + i, modalObj.transform, new Vector2(0, y), new Vector2(720, 56), taskNames[i], 22);
                mm.taskSelectButtons[i] = btnObj.GetComponent<Button>();
            }

            // Close button
            GameObject closeBtnObj = CreateButton("CloseBtn", modalObj.transform, new Vector2(0, -250), new Vector2(220, 50), "CLOSE", 22);
            mm.closeLevelSelectButton = closeBtnObj.GetComponent<Button>();

            modalObj.SetActive(false);
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            ProceduralImage pImg = btnObj.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.18f, 0.28f, 0.42f, 0.95f);
            pImg.BorderWidth = 0f;
            btnObj.AddComponent<UniformModifier>().Radius = 14f;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.28f, 0.42f, 0.62f, 1f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            btn.colors = cb;

            HoverScale hs = btnObj.AddComponent<HoverScale>();
            hs.hoverScale = 1.08f;

            GameObject textObj = CreateUIElement("BtnText", btnObj.transform);
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            SetStretchFull(tRt);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

        private static Image CreatePanelGlow(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            ProceduralImage pImg = obj.AddComponent<ProceduralImage>();
            pImg.color = color;
            pImg.BorderWidth = 0f;
            obj.AddComponent<UniformModifier>().Radius = 24f;
            obj.SetActive(false);
            return pImg;
        }

        private static ParticleSystem CreateAmbientDust(Transform parent, Material mat)
        {
            GameObject pObj = new GameObject("AmbientDust");
            pObj.transform.SetParent(parent, false);
            pObj.transform.localPosition = new Vector3(0, 0, -1f);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.85f, 1f, 0.45f), new Color(1f, 0.9f, 0.4f, 0.45f));
            main.startSize = new ParticleSystem.MinMaxCurve(12f, 24f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 25f);
            main.maxParticles = 35;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 8;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1920, 1080, 1);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            vel.y = new ParticleSystem.MinMaxCurve(6f, 20f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            ParticleSystemRenderer renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 5;
            if (mat != null) renderer.material = mat;

            return ps;
        }

        private static ParticleSystem CreateTaskBurst(Transform parent, Material mat)
        {
            GameObject pObj = new GameObject("TaskCompleteBurst");
            pObj.transform.SetParent(parent, false);
            pObj.transform.localPosition = new Vector3(0, 0, -2f);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f, 1f), new Color(0.3f, 0.95f, 1f, 1f));
            main.startSize = new ParticleSystem.MinMaxCurve(16f, 34f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(140f, 320f);
            main.maxParticles = 80;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 40f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            ParticleSystemRenderer renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 30;
            if (mat != null) renderer.material = mat;

            return ps;
        }

        private static ParticleSystem CreateVictorySparkles(Transform parent, Material mat)
        {
            GameObject pObj = new GameObject("VictorySparkles");
            pObj.transform.SetParent(parent, false);
            pObj.transform.localPosition = new Vector3(0, 0, -2f);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f, 1f), new Color(1f, 0.7f, 0.2f, 1f));
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 32f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(60f, 180f);
            main.maxParticles = 80;
            main.loop = true;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 180f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            ParticleSystemRenderer renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 35;
            if (mat != null) renderer.material = mat;

            return ps;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void SetStretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Sprite LoadSprite(string baseName)
        {
            string pngPath = "Assets/Images/" + baseName + ".png";
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sp != null) return sp;

            string jpgPath = "Assets/Images/" + baseName + ".jpg";
            return AssetDatabase.LoadAssetAtPath<Sprite>(jpgPath);
        }

        private static void AssignAudioClips(SoundManager sm)
        {
            sm.sfxClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_click.wav");
            sm.sfxSnap = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_snap.wav");
            sm.sfxChime = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_chime.wav");
            sm.sfxFanfare = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_fanfare.wav");
            sm.sfxError = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_error.wav");
            sm.sfxRatchet = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_ratchet.wav");
            sm.sfxMagicHum = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_magic_hum.wav");
            sm.sfxIceSlide = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_ice_slide.wav");
            sm.sfxAmbience = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_bg_ambience.wav");
        }
    }
}
