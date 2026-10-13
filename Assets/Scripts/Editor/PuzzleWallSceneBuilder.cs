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
using MasterPuzzleWall;

namespace MasterPuzzleWall.Editor
{
    public static class PuzzleWallSceneBuilder
    {
        [MenuItem("Tools/Build Scene")]
        [MenuItem("Tools/Master Puzzle Wall/Build Scene")]
        [MenuItem("Tools/Master Puzzle Wall/Update Master Puzzle Wall Scene")]
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

            // 2. Work directly on current active scene 'MasterPuzzleWall'
            string scenePath = "Assets/Scenes/MasterPuzzleWall.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded || !scene.path.EndsWith("MasterPuzzleWall.unity", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(scenePath))
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            }

            // Clear previous root objects to rebuild cleanly in current scene
            var rootObjs = scene.GetRootGameObjects();
            for (int i = rootObjs.Length - 1; i >= 0; i--)
            {
                if (rootObjs[i] != null)
                {
                    GameObject.DestroyImmediate(rootObjs[i]);
                }
            }

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

            // Wall Panel Glowing Highlights (tightened so left glow does not cover Golem)
            gm.leftPanelGlow = CreatePanelGlow("LeftPanel_Glow", canvasObj.transform, new Vector2(-510, 0), new Vector2(400, 800), new Color(0.2f, 0.7f, 1f, 0f));
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

            // Tasks Container (Created BEFORE Golem so Golem renders ON TOP!)
            GameObject tasksRoot = CreateUIElement("Tasks_Container", canvasObj.transform);
            SetStretchFull(tasksRoot.GetComponent<RectTransform>());

            // Task 1: Frost Slider
            gm.task1FrostSlider = CreateTask1FrostSlider(tasksRoot.transform, particleMat);
            gm.task1FrostSlider.gameObject.SetActive(false);

            // Task 2: Ice Key Shelf
            gm.task2IceShelf = CreateTask2IceShelf(tasksRoot.transform, particleMat);
            gm.task2IceShelf.gameObject.SetActive(false);

            // Task 3: Twin Prong Socket
            gm.task3TwinProng = CreateTask3TwinProng(tasksRoot.transform, particleMat);
            gm.task3TwinProng.gameObject.SetActive(false);

            // Task 4: Square Tile Array
            gm.task4SquareTile = CreateTask4SquareTile(tasksRoot.transform, particleMat);
            gm.task4SquareTile.gameObject.SetActive(false);

            // Task 5: Factor Wheel
            gm.task5FactorWheel = CreateTask5FactorWheel(tasksRoot.transform, particleMat);
            gm.task5FactorWheel.gameObject.SetActive(false);

            // Task 6: Master Base Ten Code
            gm.task6BaseTenCode = CreateTask6BaseTenCode(tasksRoot.transform);
            gm.task6BaseTenCode.gameObject.SetActive(false);

            // Golem Bot & Helper Controller (Rendered ON TOP of Tasks!)
            gm.GolemBot = CreateHelperBot(canvasObj.transform);

            // Score Manager & Floating Popups Container
            ScoreManager scoreMgr = gmObj.AddComponent<ScoreManager>();
            GameObject popupContainer = CreateUIElement("ScorePopupContainer", canvasObj.transform);
            SetStretchFull(popupContainer.GetComponent<RectTransform>());
            scoreMgr.popupContainer = popupContainer.transform;

            // Top HUD
            CreateTopHUD(canvasObj.transform, gm, scoreMgr);
            gm.topHudRoot.SetActive(false);

            // Victory Panel
            gm.victoryPanel = CreateVictoryPanel(canvasObj.transform);
            gm.victoryPanel.gameObject.SetActive(false);

            // Fail Panel
            gm.failPanel = CreateFailPanel(canvasObj.transform);
            gm.failPanel.gameObject.SetActive(false);

            // Main Menu
            gm.mainMenu = CreateMainMenu(canvasObj.transform);
            gm.mainMenu.gameObject.SetActive(true);

            // 7. Save Current Scene In-Place
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#00FF88><b>[MasterPuzzleWall]</b> Current scene 'MasterPuzzleWall' updated and saved in-place!</color>");

            // Set in build settings
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static HelperBotController CreateHelperBot(Transform parent)
        {
            GameObject botRoot = CreateUIElement("HelperBot_Golem", parent);
            RectTransform botRt = botRoot.GetComponent<RectTransform>();
            botRt.anchorMin = new Vector2(1, 0);
            botRt.anchorMax = new Vector2(1, 0);
            botRt.pivot = new Vector2(0.5f, 0.5f);
            botRt.anchoredPosition = new Vector2(-190, 185);
            botRt.sizeDelta = new Vector2(260, 260);

            HelperBotController bot = botRoot.AddComponent<HelperBotController>();
            bot.botRect = botRt;

            Image botImg = botRoot.AddComponent<Image>();
            bot.botImage = botImg;
            bot.normalSprite = LoadSprite("stone_golem_bot");
            bot.confusedSprite = LoadSprite("stone_golem_confused");
            if (bot.normalSprite != null) botImg.sprite = bot.normalSprite;

            // Speech Bubble (anchored above Golem and extending leftwards over open area)
            GameObject bubbleObj = CreateUIElement("SpeechBubble", botRoot.transform);
            RectTransform bubbleRt = bubbleObj.GetComponent<RectTransform>();
            bubbleRt.anchorMin = new Vector2(0.5f, 1f);
            bubbleRt.anchorMax = new Vector2(0.5f, 1f);
            bubbleRt.pivot = new Vector2(1f, 0f);
            bubbleRt.anchoredPosition = new Vector2(-10, 10);
            bubbleRt.sizeDelta = new Vector2(390, 140);

            ProceduralImage bubbleBg = bubbleObj.AddComponent<ProceduralImage>();
            bubbleBg.color = new Color(0.06f, 0.10f, 0.16f, 0.96f);
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
            labelRt.sizeDelta = new Vector2(-30, 28);
            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "<b><color=#5EEAD4>GOLEM HELPER BOT:</color></b>";
            labelTmp.fontSize = 18;

            // Speech Text (Auto-sizing so it never overflows!)
            GameObject textObj = CreateUIElement("SpeechText", bubbleObj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = new Vector2(0, -15);
            textRt.sizeDelta = new Vector2(-36, -46);
            TextMeshProUGUI speechTmp = textObj.AddComponent<TextMeshProUGUI>();
            speechTmp.text = FrostSliderTask.GOLEM_INSTRUCTION;
            speechTmp.fontSize = 17;
            speechTmp.enableAutoSizing = true;
            speechTmp.fontSizeMin = 13;
            speechTmp.fontSizeMax = 18;
            speechTmp.textWrappingMode = TextWrappingModes.Normal;
            speechTmp.color = Color.white;
            bot.speechText = speechTmp;

            return bot;
        }

        private static void CreateTopHUD(Transform parent, GameManager gm, ScoreManager sm)
        {
            GameObject hudRoot = CreateUIElement("TopHUD", parent);
            RectTransform hudRt = hudRoot.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0.5f, 1f);
            hudRt.anchorMax = new Vector2(0.5f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0, -10);
            hudRt.sizeDelta = new Vector2(1820, 140);
            gm.topHudRoot = hudRoot;

            ProceduralImage hudBg = hudRoot.AddComponent<ProceduralImage>();
            hudBg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
            hudBg.BorderWidth = 0f;
            hudRoot.AddComponent<UniformModifier>().Radius = 18f;

            // Task Title (Center Top)
            GameObject titleObj = CreateUIElement("TaskTitle", hudRoot.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 40);
            titleRt.sizeDelta = new Vector2(700, 36);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "TASK 1: THE FROST SLOT SLIDER";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.88f, 0.4f);
            gm.taskTitleText = titleTmp;

            // Score Counter (Right Top)
            GameObject scoreObj = CreateUIElement("ScoreDisplay", hudRoot.transform);
            RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
            scoreRt.anchoredPosition = new Vector2(620, 40);
            scoreRt.sizeDelta = new Vector2(300, 36);
            TextMeshProUGUI scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.text = "<b>SCORE: <color=#FDE047>0</color></b>";
            scoreTmp.fontSize = 22;
            scoreTmp.alignment = TextAlignmentOptions.Right;
            scoreTmp.color = Color.white;
            if (sm != null) sm.scoreText = scoreTmp;

            // Streak Multiplier
            GameObject comboObj = CreateUIElement("ComboDisplay", hudRoot.transform);
            RectTransform comboRt = comboObj.GetComponent<RectTransform>();
            comboRt.anchoredPosition = new Vector2(620, 10);
            comboRt.sizeDelta = new Vector2(250, 26);
            TextMeshProUGUI comboTmp = comboObj.AddComponent<TextMeshProUGUI>();
            comboTmp.text = "<b><color=#F97316>STREAK x1!</color></b>";
            comboTmp.fontSize = 17;
            comboTmp.alignment = TextAlignmentOptions.Right;
            if (sm != null) sm.comboText = comboTmp;
            comboObj.SetActive(false);

            // Stars Rating (Left of Score)
            GameObject starsRoot = CreateUIElement("StarsContainer", hudRoot.transform);
            RectTransform starsRt = starsRoot.GetComponent<RectTransform>();
            starsRt.anchoredPosition = new Vector2(360, 40);
            starsRt.sizeDelta = new Vector2(140, 36);

            if (sm != null) sm.starImages = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                GameObject starObj = CreateUIElement("Star_" + s, starsRoot.transform);
                RectTransform sRt = starObj.GetComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2((s - 1) * 36f, 0);
                sRt.sizeDelta = new Vector2(28, 28);
                Image sImg = starObj.AddComponent<Image>();
                Sprite gemSp = LoadSprite("glowing_starter_gem");
                if (gemSp != null) sImg.sprite = gemSp;
                sImg.color = new Color(0.3f, 0.35f, 0.45f, 0.5f);
                if (sm != null) sm.starImages[s] = sImg;
            }

            // Loud Reader Prompt Box (Bottom row of TopHUD)
            GameObject promptObj = CreateUIElement("LoudReaderPrompt", hudRoot.transform);
            RectTransform promptRt = promptObj.GetComponent<RectTransform>();
            promptRt.anchoredPosition = new Vector2(0, -25);
            promptRt.sizeDelta = new Vector2(1480, 65);
            TextMeshProUGUI promptTmp = promptObj.AddComponent<TextMeshProUGUI>();
            promptTmp.text = "Prompt text for loud reader or student...";
            promptTmp.fontSize = 18;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.92f, 0.96f, 1f);
            promptTmp.textWrappingMode = TextWrappingModes.Normal;
            gm.loudReaderPromptText = promptTmp;

            // Home Button
            GameObject homeObj = CreateButton("HomeBtn", hudRoot.transform, new Vector2(-800, 20), new Vector2(100, 56), "HOME", 19);
            gm.homeButton = homeObj.GetComponent<Button>();

            // Restart Task Button
            GameObject restartObj = CreateButton("RestartBtn", hudRoot.transform, new Vector2(800, 20), new Vector2(100, 56), "RESET", 19);
            gm.restartTaskButton = restartObj.GetComponent<Button>();
        }

        private static FrostSliderTask CreateTask1FrostSlider(Transform parent, Material particleMat)
        {
            GameObject root = CreateUIElement("Task1_FrostSlider", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-510, 0);
            rootRt.sizeDelta = new Vector2(380, 740);

            FrostSliderTask task = root.AddComponent<FrostSliderTask>();

            // 1. Atmospheric Cyan Aura Halo Backplate
            GameObject auraObj = CreateUIElement("AtmosphericAura", root.transform);
            RectTransform auraRt = auraObj.GetComponent<RectTransform>();
            auraRt.sizeDelta = new Vector2(420, 780);
            ProceduralImage auraImg = auraObj.AddComponent<ProceduralImage>();
            auraImg.color = new Color(0.12f, 0.65f, 1f, 0.18f);
            auraImg.BorderWidth = 0f;
            auraObj.AddComponent<UniformModifier>().Radius = 36f;

            // 2. Reactor Stone Casing Base
            ProceduralImage casingBg = root.AddComponent<ProceduralImage>();
            casingBg.color = new Color(0.06f, 0.09f, 0.15f, 0.96f);
            casingBg.BorderWidth = 0f;
            root.AddComponent<UniformModifier>().Radius = 26f;

            // 3. Inner Frame Trim
            GameObject trimObj = CreateUIElement("CasingTrim", root.transform);
            RectTransform trimRt = trimObj.GetComponent<RectTransform>();
            trimRt.sizeDelta = new Vector2(360, 720);
            ProceduralImage trimImg = trimObj.AddComponent<ProceduralImage>();
            trimImg.color = new Color(0.12f, 0.32f, 0.52f, 0.40f);
            trimImg.BorderWidth = 0f;
            trimObj.AddComponent<UniformModifier>().Radius = 20f;

            // 4. Ornate Header Plaque Banner
            GameObject titleObj = CreateUIElement("ReactorTitle", root.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 328);
            titleRt.sizeDelta = new Vector2(330, 56);
            ProceduralImage titleBg = titleObj.AddComponent<ProceduralImage>();
            titleBg.color = new Color(0.08f, 0.14f, 0.24f, 0.95f);
            titleBg.BorderWidth = 0f;
            titleObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject titleTextObj = CreateUIElement("TitleText", titleObj.transform);
            SetStretchFull(titleTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38BDF8>THE FROST REACTOR</color></b>\n<size=62%><color=#94A3B8>CALIBRATE SUB-ZERO COLD</color></size>";
            titleTmp.fontSize = 19;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 5. Track Container
            GameObject trackObj = CreateUIElement("FrostTrack", root.transform);
            RectTransform trackRt = trackObj.GetComponent<RectTransform>();
            trackRt.anchoredPosition = new Vector2(-55, -15);
            trackRt.sizeDelta = new Vector2(80, 520);
            ProceduralImage trackBg = trackObj.AddComponent<ProceduralImage>();
            trackBg.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);
            trackBg.BorderWidth = 0f;
            trackObj.AddComponent<UniformModifier>().Radius = 40f;
            task.trackRect = trackRt;

            // Inner Tube Glass Trim
            GameObject tubeTrimObj = CreateUIElement("TubeTrim", trackObj.transform);
            RectTransform tubeTrimRt = tubeTrimObj.GetComponent<RectTransform>();
            tubeTrimRt.sizeDelta = new Vector2(72, 512);
            ProceduralImage tubeTrimImg = tubeTrimObj.AddComponent<ProceduralImage>();
            tubeTrimImg.color = new Color(0.15f, 0.42f, 0.68f, 0.35f);
            tubeTrimImg.BorderWidth = 0f;
            tubeTrimObj.AddComponent<UniformModifier>().Radius = 36f;

            // Dynamic Fluid Fill Tube (Filled Image)
            GameObject fluidObj = CreateUIElement("FluidFill", trackObj.transform);
            RectTransform fluidRt = fluidObj.GetComponent<RectTransform>();
            fluidRt.sizeDelta = new Vector2(58, 496);
            ProceduralImage fluidImg = fluidObj.AddComponent<ProceduralImage>();
            fluidImg.type = Image.Type.Filled;
            fluidImg.fillMethod = Image.FillMethod.Vertical;
            fluidImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            fluidImg.fillAmount = 0.5f;
            fluidImg.color = new Color(0.2f, 0.8f, 1f, 0.95f);
            fluidImg.BorderWidth = 0f;
            fluidObj.AddComponent<UniformModifier>().Radius = 29f;
            task.fluidFillImage = fluidImg;

            // Glass Reflection Sheen Stripe (vertical curved glass gleam)
            GameObject sheenObj = CreateUIElement("GlassSheen", trackObj.transform);
            RectTransform sheenRt = sheenObj.GetComponent<RectTransform>();
            sheenRt.anchorMin = new Vector2(0f, 0.03f);
            sheenRt.anchorMax = new Vector2(0f, 0.97f);
            sheenRt.pivot = new Vector2(0f, 0.5f);
            sheenRt.anchoredPosition = new Vector2(12f, 0f);
            sheenRt.sizeDelta = new Vector2(8f, 0f);
            ProceduralImage sheenImg = sheenObj.AddComponent<ProceduralImage>();
            sheenImg.color = new Color(1f, 1f, 1f, 0.16f);
            sheenImg.BorderWidth = 0f;
            sheenObj.AddComponent<UniformModifier>().Radius = 4f;

            // Sub-zero frozen blue tint zone overlay
            GameObject frozenZoneObj = CreateUIElement("FrozenZone", trackObj.transform);
            RectTransform frozenZoneRt = frozenZoneObj.GetComponent<RectTransform>();
            frozenZoneRt.anchorMin = new Vector2(0, 0);
            frozenZoneRt.anchorMax = new Vector2(1, 0.52f);
            frozenZoneRt.sizeDelta = Vector2.zero;
            Image frozenImg = frozenZoneObj.AddComponent<Image>();
            frozenImg.color = new Color(0.1f, 0.6f, 1f, 0.35f);
            task.frostVaporOverlay = frozenImg;

            // Zero Line Highlight
            GameObject zeroLineObj = CreateUIElement("ZeroLine", trackObj.transform);
            RectTransform zeroLineRt = zeroLineObj.GetComponent<RectTransform>();
            zeroLineRt.anchoredPosition = new Vector2(0, 70);
            zeroLineRt.sizeDelta = new Vector2(98, 6);
            Image zeroLineImg = zeroLineObj.AddComponent<Image>();
            zeroLineImg.color = Color.white;

            // Ticks: +2, +1, 0, -1, -2, -3, -4, -5
            int[] tickVals = new int[] { 2, 1, 0, -1, -2, -3, -4, -5 };
            float[] tickYs = new float[] { 190, 130, 70, 10, -50, -110, -170, -230 };
            task.tickLabelTexts = new TextMeshProUGUI[tickVals.Length];

            for (int i = 0; i < tickVals.Length; i++)
            {
                int val = tickVals[i];
                float y = tickYs[i];

                GameObject tickLine = CreateUIElement("Tick_" + val, trackObj.transform);
                RectTransform tlRt = tickLine.GetComponent<RectTransform>();
                tlRt.anchoredPosition = new Vector2(0, y);
                tlRt.sizeDelta = new Vector2(val == 0 ? 94 : (val == -4 ? 86 : 52), val == 0 || val == -4 ? 4 : 2);
                Image tlImg = tickLine.AddComponent<Image>();
                tlImg.color = val == 0 ? Color.white : (val == -4 ? new Color(0.2f, 0.95f, 1f, 1f) : (val < 0 ? new Color(0.3f, 0.85f, 1f, 0.8f) : new Color(1f, 0.7f, 0.3f, 0.8f)));

                GameObject lblObj = CreateUIElement("Label_" + val, trackObj.transform);
                RectTransform lblRt = lblObj.GetComponent<RectTransform>();
                lblRt.anchoredPosition = new Vector2(-70, y);
                lblRt.sizeDelta = new Vector2(65, 30);
                TextMeshProUGUI tmp = lblObj.AddComponent<TextMeshProUGUI>();
                tmp.text = val > 0 ? "+" + val : val.ToString();
                tmp.fontSize = val == 0 || val == -4 ? 20 : 16;
                tmp.fontStyle = val == 0 || val == -4 ? FontStyles.Bold : FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Right;
                tmp.color = val == 0 ? Color.white : (val == -4 ? new Color(0.2f, 0.95f, 1f) : (val < 0 ? new Color(0.4f, 0.85f, 1f) : new Color(1f, 0.8f, 0.4f)));
                task.tickLabelTexts[i] = tmp;

                // Special label for Zero and Target -4
                if (val == 0)
                {
                    GameObject zTag = CreateUIElement("ZeroTag", trackObj.transform);
                    RectTransform ztRt = zTag.GetComponent<RectTransform>();
                    ztRt.anchoredPosition = new Vector2(98, y);
                    ztRt.sizeDelta = new Vector2(148, 28);

                    ProceduralImage ztBg = zTag.AddComponent<ProceduralImage>();
                    ztBg.color = new Color(0.12f, 0.20f, 0.30f, 0.92f);
                    ztBg.BorderWidth = 0f;
                    zTag.AddComponent<UniformModifier>().Radius = 8f;

                    GameObject ztTextObj = CreateUIElement("ZeroText", zTag.transform);
                    SetStretchFull(ztTextObj.GetComponent<RectTransform>());
                    TextMeshProUGUI ztTmp = ztTextObj.AddComponent<TextMeshProUGUI>();
                    ztTmp.text = "<b><color=#FFFFFF>FREEZE (0)</color></b>";
                    ztTmp.fontSize = 13;
                    ztTmp.textWrappingMode = TextWrappingModes.NoWrap;
                    ztTmp.alignment = TextAlignmentOptions.Center;
                }
                else if (val == -4)
                {
                    GameObject tTag = CreateUIElement("TargetTag", trackObj.transform);
                    RectTransform ttRt = tTag.GetComponent<RectTransform>();
                    ttRt.anchoredPosition = new Vector2(98, y);
                    ttRt.sizeDelta = new Vector2(148, 28);

                    ProceduralImage ttBg = tTag.AddComponent<ProceduralImage>();
                    ttBg.color = new Color(0.04f, 0.22f, 0.42f, 0.95f);
                    ttBg.BorderWidth = 0f;
                    tTag.AddComponent<UniformModifier>().Radius = 8f;

                    GameObject ttTextObj = CreateUIElement("TargetText", tTag.transform);
                    SetStretchFull(ttTextObj.GetComponent<RectTransform>());
                    TextMeshProUGUI ttTmp = ttTextObj.AddComponent<TextMeshProUGUI>();
                    ttTmp.text = "<b><color=#38BDF8>&lt; GOAL (-4)</color></b>";
                    ttTmp.fontSize = 13;
                    ttTmp.textWrappingMode = TextWrappingModes.NoWrap;
                    ttTmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // Slider Gem
            GameObject gemObj = CreateUIElement("SliderGem", trackObj.transform);
            RectTransform gemRt = gemObj.GetComponent<RectTransform>();
            gemRt.sizeDelta = new Vector2(96, 96);
            gemRt.anchoredPosition = new Vector2(0, 70);
            Image gemImg = gemObj.AddComponent<Image>();
            Sprite gemSprite = LoadSprite("glowing_starter_gem");
            if (gemSprite != null) gemImg.sprite = gemSprite;
            gemObj.AddComponent<HoverScale>().hoverScale = 1.15f;
            task.sliderGem = gemRt;
            task.sliderGemGlow = gemImg;

            // Reactive Wall Rune Sockets on Right Side
            task.reactiveRuneFrostIcons = new Image[3];
            float[] runeYs = new float[] { 140f, 0f, -140f };
            string[] runeIcons = new string[] { "FIRE", "WATER", "ICE" };

            for (int r = 0; r < 3; r++)
            {
                GameObject runeObj = CreateUIElement("RuneSocket_" + r, root.transform);
                RectTransform rRt = runeObj.GetComponent<RectTransform>();
                rRt.anchoredPosition = new Vector2(105, runeYs[r]);
                rRt.sizeDelta = new Vector2(70, 70);

                ProceduralImage rBg = runeObj.AddComponent<ProceduralImage>();
                rBg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
                rBg.BorderWidth = 0f;
                runeObj.AddComponent<UniformModifier>().Radius = 35f;

                // Inner well
                GameObject wellObj = CreateUIElement("RuneWell", runeObj.transform);
                RectTransform wellRt = wellObj.GetComponent<RectTransform>();
                wellRt.sizeDelta = new Vector2(60, 60);
                ProceduralImage wellImg = wellObj.AddComponent<ProceduralImage>();
                wellImg.color = new Color(0.05f, 0.08f, 0.14f, 0.95f);
                wellImg.BorderWidth = 0f;
                wellObj.AddComponent<UniformModifier>().Radius = 30f;

                GameObject frostOverlay = CreateUIElement("FrostOverlay", runeObj.transform);
                RectTransform foRt = frostOverlay.GetComponent<RectTransform>();
                SetStretchFull(foRt);
                ProceduralImage foImg = frostOverlay.AddComponent<ProceduralImage>();
                foImg.color = new Color(0.3f, 0.85f, 1f, 0f);
                foImg.BorderWidth = 0f;
                frostOverlay.AddComponent<UniformModifier>().Radius = 35f;
                task.reactiveRuneFrostIcons[r] = foImg;

                GameObject rText = CreateUIElement("RuneText", runeObj.transform);
                RectTransform rtText = rText.GetComponent<RectTransform>();
                SetStretchFull(rtText);
                TextMeshProUGUI rtTmp = rText.AddComponent<TextMeshProUGUI>();
                rtTmp.text = $"<b>{runeIcons[r]}</b>";
                rtTmp.fontSize = 13;
                rtTmp.textWrappingMode = TextWrappingModes.NoWrap;
                rtTmp.alignment = TextAlignmentOptions.Center;
                rtTmp.color = new Color(0.8f, 0.9f, 1f);
            }

            // Real-time Readout Text Box
            GameObject readoutObj = CreateUIElement("ReadoutBadge", root.transform);
            RectTransform readoutRt = readoutObj.GetComponent<RectTransform>();
            readoutRt.anchoredPosition = new Vector2(0, -280);
            readoutRt.sizeDelta = new Vector2(330, 48);
            ProceduralImage readoutBg = readoutObj.AddComponent<ProceduralImage>();
            readoutBg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            readoutBg.BorderWidth = 0f;
            readoutObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject readoutTextObj = CreateUIElement("ReadoutText", readoutObj.transform);
            SetStretchFull(readoutTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI readoutTmp = readoutTextObj.AddComponent<TextMeshProUGUI>();
            readoutTmp.fontSize = 20;
            readoutTmp.alignment = TextAlignmentOptions.Center;
            task.currentValueText = readoutTmp;

            // Subzero Description
            GameObject descObj = CreateUIElement("SubZeroDesc", root.transform);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(0, -325);
            descRt.sizeDelta = new Vector2(340, 36);
            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.fontSize = 13;
            descTmp.alignment = TextAlignmentOptions.Center;
            task.subZeroDescText = descTmp;

            // Interactive Seal Activation Button (pops up when -4 reached!)
            GameObject sealBtnObj = CreateButton("SealButton", root.transform, new Vector2(0, -280), new Vector2(320, 54), "SHATTER ICE & ACTIVATE", 18);
            sealBtnObj.GetComponent<ProceduralImage>().color = new Color(0.1f, 0.55f, 0.85f, 0.98f);
            Button sealBtn = sealBtnObj.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(sealBtn.onClick, task.OnSealButtonTapped);
            task.targetRuneSealButton = sealBtnObj;
            task.sealButtonText = sealBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            sealBtnObj.SetActive(false);

            // Confetti Celebration Particles
            task.confettiParticles = CreateConfettiEffect(root.transform, particleMat);

            return task;
        }

        private static IceShelfTask CreateTask2IceShelf(Transform parent, Material particleMat)
        {
            GameObject root = CreateUIElement("Task2_IceShelf", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-510, -140);
            rootRt.sizeDelta = new Vector2(580, 330);

            IceShelfTask task = root.AddComponent<IceShelfTask>();

            // 1. Atmospheric Frost/Gold Aura Halo Backplate
            GameObject auraObj = CreateUIElement("AltarAura", root.transform);
            RectTransform auraRt = auraObj.GetComponent<RectTransform>();
            auraRt.sizeDelta = new Vector2(630, 380);
            ProceduralImage auraImg = auraObj.AddComponent<ProceduralImage>();
            auraImg.color = new Color(0.12f, 0.65f, 1f, 0.22f);
            auraImg.BorderWidth = 0f;
            auraObj.AddComponent<UniformModifier>().Radius = 32f;

            // 2. Heavy Basalt Altar Stone Base Slab
            ProceduralImage casingBg = root.AddComponent<ProceduralImage>();
            casingBg.color = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            casingBg.BorderWidth = 0f;
            root.AddComponent<UniformModifier>().Radius = 24f;
            task.shelfGlowImage = casingBg;

            // 3. Inner Carved Stone Well & Inset Border Trim
            GameObject trimObj = CreateUIElement("AltarTrim", root.transform);
            RectTransform trimRt = trimObj.GetComponent<RectTransform>();
            trimRt.sizeDelta = new Vector2(560, 310);
            ProceduralImage trimImg = trimObj.AddComponent<ProceduralImage>();
            trimImg.color = new Color(0.10f, 0.26f, 0.44f, 0.40f);
            trimImg.BorderWidth = 0f;
            trimObj.AddComponent<UniformModifier>().Radius = 18f;

            // 4. Ornate Header Plaque Banner
            GameObject titleObj = CreateUIElement("AltarTitle", root.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 126);
            titleRt.sizeDelta = new Vector2(530, 50);
            ProceduralImage titleBg = titleObj.AddComponent<ProceduralImage>();
            titleBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            titleBg.BorderWidth = 0f;
            titleObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject titleTextObj = CreateUIElement("TitleText", titleObj.transform);
            SetStretchFull(titleTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38BDF8>THE FROZEN KEY SHELF</color></b>\n<size=64%><color=#94A3B8>ORDER: LEAST (COLDEST) -> GREATEST (WARMEST)</color></size>";
            titleTmp.fontSize = 17;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.textWrappingMode = TextWrappingModes.Normal;

            // 5. Directional Axis Guide (Bottom Ribbon)
            GameObject axisObj = CreateUIElement("AxisGuide", root.transform);
            RectTransform axisRt = axisObj.GetComponent<RectTransform>();
            axisRt.anchoredPosition = new Vector2(0, -125);
            axisRt.sizeDelta = new Vector2(530, 34);
            TextMeshProUGUI axisTmp = axisObj.AddComponent<TextMeshProUGUI>();
            axisTmp.text = "<color=#38BDF8>&lt; <b>COLDER (-)</b></color> <color=#475569>-------- [ <b>0 ZERO</b> ] --------</color> <color=#FCD34D><b>(+) WARMER</b> &gt;</color>";
            axisTmp.fontSize = 13;
            axisTmp.alignment = TextAlignmentOptions.Center;
            axisTmp.textWrappingMode = TextWrappingModes.NoWrap;

            // 6. Three Pedestal Sockets Container
            GameObject socketsContainer = CreateUIElement("SocketsContainer", root.transform);
            RectTransform socketsRt = socketsContainer.GetComponent<RectTransform>();
            socketsRt.anchoredPosition = new Vector2(0, 0);
            socketsRt.sizeDelta = new Vector2(540, 180);

            task.slotPositions = new RectTransform[3];
            float[] slotX = new float[] { -160f, 0f, 160f };
            string[] slotLabels = new string[]
            {
                "<b>COLDEST</b>\n<size=80%><color=#38BDF8>(Least: -4)</color></size>",
                "<b>MIDDLE</b>\n<size=80%><color=#7DD3FC>(-1)</color></size>",
                "<b>WARMEST</b>\n<size=80%><color=#FCD34D>(Greatest: +2)</color></size>"
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = CreateUIElement("Slot_" + i, socketsContainer.transform);
                RectTransform slotRt = slotObj.GetComponent<RectTransform>();
                slotRt.anchoredPosition = new Vector2(slotX[i], 14);
                slotRt.sizeDelta = new Vector2(126, 126);
                task.slotPositions[i] = slotRt;

                // Outer pedestal rim
                ProceduralImage sImg = slotObj.AddComponent<ProceduralImage>();
                sImg.color = new Color(0.10f, 0.15f, 0.25f, 0.90f);
                sImg.BorderWidth = 0f;
                slotObj.AddComponent<UniformModifier>().Radius = 18f;

                // Inner recessed slot well
                GameObject wellObj = CreateUIElement("SlotWell", slotObj.transform);
                RectTransform wellRt = wellObj.GetComponent<RectTransform>();
                wellRt.sizeDelta = new Vector2(114, 114);
                ProceduralImage wellImg = wellObj.AddComponent<ProceduralImage>();
                wellImg.color = new Color(0.03f, 0.05f, 0.09f, 0.96f);
                wellImg.BorderWidth = 0f;
                wellObj.AddComponent<UniformModifier>().Radius = 14f;

                // Inner runic corner accents
                GameObject cornerAccent = CreateUIElement("CornerAccent", wellObj.transform);
                SetStretchFull(cornerAccent.GetComponent<RectTransform>());
                ProceduralImage caImg = cornerAccent.AddComponent<ProceduralImage>();
                caImg.color = new Color(0.2f, 0.6f, 0.9f, 0.22f);
                caImg.BorderWidth = 0f;
                cornerAccent.AddComponent<UniformModifier>().Radius = 14f;

                // Pedestal Plaque Badge below slot
                GameObject badgeObj = CreateUIElement("Badge", slotObj.transform);
                RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.anchoredPosition = new Vector2(0, -82);
                badgeRt.sizeDelta = new Vector2(140, 36);
                ProceduralImage badgeBg = badgeObj.AddComponent<ProceduralImage>();
                badgeBg.color = new Color(0.04f, 0.07f, 0.14f, 0.92f);
                badgeBg.BorderWidth = 0f;
                badgeObj.AddComponent<UniformModifier>().Radius = 9f;

                GameObject badgeTextObj = CreateUIElement("BadgeText", badgeObj.transform);
                SetStretchFull(badgeTextObj.GetComponent<RectTransform>());
                TextMeshProUGUI badgeTmp = badgeTextObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.text = slotLabels[i];
                badgeTmp.fontSize = 12;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = Color.white;
                badgeTmp.textWrappingMode = TextWrappingModes.Normal;
            }

            // 7. 3 Draggable Key Blocks
            task.keyBlocks = new KeyBlockItem[3];
            Sprite keyTileSprite = LoadSprite("ice_shelf_key_tile");
            if (keyTileSprite == null) keyTileSprite = LoadSprite("stone_rune_tile");

            for (int i = 0; i < 3; i++)
            {
                GameObject blockObj = CreateUIElement("BlockItem_" + i, root.transform);
                RectTransform blockRt = blockObj.GetComponent<RectTransform>();
                blockRt.sizeDelta = new Vector2(116, 116);

                KeyBlockItem block = blockObj.AddComponent<KeyBlockItem>();

                // Shadow / Depth under block
                GameObject shadowObj = CreateUIElement("DropShadow", blockObj.transform);
                RectTransform shadowRt = shadowObj.GetComponent<RectTransform>();
                shadowRt.anchoredPosition = new Vector2(0, -6);
                shadowRt.sizeDelta = new Vector2(116, 116);
                ProceduralImage sImg = shadowObj.AddComponent<ProceduralImage>();
                sImg.color = new Color(0, 0, 0, 0.45f);
                shadowObj.AddComponent<UniformModifier>().Radius = 16f;

                // Primary Tile Image (ice_shelf_key_tile with ornate carved bronze/gold frame)
                Image blockImg = blockObj.AddComponent<Image>();
                if (keyTileSprite != null) blockImg.sprite = keyTileSprite;
                block.blockImage = blockImg;

                // Recessed ultra-contrast dark plate inside tile (guarantees number is 100% visible)
                GameObject backplateObj = CreateUIElement("CenterBackplate", blockObj.transform);
                RectTransform bpRt = backplateObj.GetComponent<RectTransform>();
                bpRt.sizeDelta = new Vector2(80, 80);
                ProceduralImage bpImg = backplateObj.AddComponent<ProceduralImage>();
                bpImg.color = new Color(0.03f, 0.06f, 0.12f, 0.90f);
                bpImg.BorderWidth = 0f;
                backplateObj.AddComponent<UniformModifier>().Radius = 14f;

                // Border glow for selection/completion (outline only, never solid yellow!)
                GameObject glowObj = CreateUIElement("BorderGlow", blockObj.transform);
                RectTransform glowRt = glowObj.GetComponent<RectTransform>();
                SetStretchFull(glowRt);
                ProceduralImage glowImg = glowObj.AddComponent<ProceduralImage>();
                glowImg.color = new Color(1f, 0.88f, 0.2f, 0.95f);
                glowImg.BorderWidth = 4f;
                glowObj.AddComponent<UniformModifier>().Radius = 16f;
                glowObj.SetActive(false);
                block.borderGlow = glowImg;

                // Value Text (Huge, Bold, Crystal Clear Neon Number)
                GameObject valObj = CreateUIElement("ValueText", blockObj.transform);
                RectTransform valRt = valObj.GetComponent<RectTransform>();
                valRt.anchoredPosition = new Vector2(0, 8);
                valRt.sizeDelta = new Vector2(80, 52);
                TextMeshProUGUI valTmp = valObj.AddComponent<TextMeshProUGUI>();
                valTmp.fontSize = 46;
                valTmp.fontStyle = FontStyles.Bold;
                valTmp.alignment = TextAlignmentOptions.Center;
                valTmp.raycastTarget = false;
                block.valueText = valTmp;

                // SubLabel Text (e.g. COLDEST, COLD, WARM)
                GameObject subObj = CreateUIElement("SubLabelText", blockObj.transform);
                RectTransform subRt = subObj.GetComponent<RectTransform>();
                subRt.anchoredPosition = new Vector2(0, -25);
                subRt.sizeDelta = new Vector2(80, 22);
                TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
                subTmp.fontSize = 11;
                subTmp.fontStyle = FontStyles.Bold;
                subTmp.alignment = TextAlignmentOptions.Center;
                subTmp.raycastTarget = false;
                block.subLabelText = subTmp;

                // Hover scale for juicy micro-animation
                blockObj.AddComponent<HoverScale>().hoverScale = 1.08f;

                task.keyBlocks[i] = block;
            }

            // Confetti Celebration Particles for Task 2
            task.confettiParticles = CreateConfettiEffect(root.transform, particleMat);

            return task;
        }

        private static TwinProngTask CreateTask3TwinProng(Transform parent, Material particleMat)
        {
            GameObject root = CreateUIElement("Task3_TwinProng", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0, -140);
            rootRt.sizeDelta = new Vector2(620, 420);

            TwinProngTask task = root.AddComponent<TwinProngTask>();

            // 1. Atmospheric Electro-Cyan / Gold Aura Halo
            GameObject auraObj = CreateUIElement("ConduitAura", root.transform);
            RectTransform auraRt = auraObj.GetComponent<RectTransform>();
            auraRt.sizeDelta = new Vector2(660, 460);
            ProceduralImage auraImg = auraObj.AddComponent<ProceduralImage>();
            auraImg.color = new Color(0.12f, 0.65f, 1f, 0.20f);
            auraImg.BorderWidth = 0f;
            auraObj.AddComponent<UniformModifier>().Radius = 32f;

            // 2. Heavy Carved Basalt Conduit Altar Base
            ProceduralImage casingBg = root.AddComponent<ProceduralImage>();
            casingBg.color = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            casingBg.BorderWidth = 0f;
            root.AddComponent<UniformModifier>().Radius = 24f;

            // 3. Inner Runic & Copper Trim
            GameObject trimObj = CreateUIElement("ConduitTrim", root.transform);
            RectTransform trimRt = trimObj.GetComponent<RectTransform>();
            trimRt.sizeDelta = new Vector2(590, 390);
            ProceduralImage trimImg = trimObj.AddComponent<ProceduralImage>();
            trimImg.color = new Color(0.12f, 0.28f, 0.46f, 0.40f);
            trimImg.BorderWidth = 0f;
            trimObj.AddComponent<UniformModifier>().Radius = 18f;

            // 4. Header Plaque Banner
            GameObject titleObj = CreateUIElement("ConduitTitle", root.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 168);
            titleRt.sizeDelta = new Vector2(560, 52);
            ProceduralImage titleBg = titleObj.AddComponent<ProceduralImage>();
            titleBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            titleBg.BorderWidth = 0f;
            titleObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject titleTextObj = CreateUIElement("TitleText", titleObj.transform);
            SetStretchFull(titleTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38BDF8>THE TWIN PRONG CONDUIT</color></b>\n<size=62%><color=#94A3B8>EVEN NUMBERS SPLIT INTO TWO EQUAL PARTS (END IN 0, 2, 4, 6, 8)</color></size>";
            titleTmp.fontSize = 17;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 5. Electric Conduit Circuit Veins (Connecting Socket to Pedestals)
            GameObject wireL = CreateUIElement("WireLeft", root.transform);
            RectTransform wlRt = wireL.GetComponent<RectTransform>();
            wlRt.anchoredPosition = new Vector2(-95, -10);
            wlRt.sizeDelta = new Vector2(90, 8);
            ProceduralImage wlImg = wireL.AddComponent<ProceduralImage>();
            wlImg.color = new Color(0.2f, 0.7f, 1f, 0.6f);
            wireL.AddComponent<UniformModifier>().Radius = 4f;

            GameObject wireR = CreateUIElement("WireRight", root.transform);
            RectTransform wrRt = wireR.GetComponent<RectTransform>();
            wrRt.anchoredPosition = new Vector2(95, -10);
            wrRt.sizeDelta = new Vector2(90, 8);
            ProceduralImage wrImg = wireR.AddComponent<ProceduralImage>();
            wrImg.color = new Color(0.2f, 0.7f, 1f, 0.6f);
            wireR.AddComponent<UniformModifier>().Radius = 4f;

            // 6. Socket Target Terminal Housing
            GameObject socketObj = CreateUIElement("TwinSocketAnchor", root.transform);
            RectTransform socketRt = socketObj.GetComponent<RectTransform>();
            socketRt.anchoredPosition = new Vector2(0, 65);
            socketRt.sizeDelta = new Vector2(230, 130);
            task.socketAnchor = socketRt;

            ProceduralImage socketBg = socketObj.AddComponent<ProceduralImage>();
            socketBg.color = new Color(0.08f, 0.12f, 0.20f, 0.98f);
            socketBg.BorderWidth = 0f;
            socketObj.AddComponent<UniformModifier>().Radius = 18f;
            task.socketBlinkGlow = socketBg;

            // Double slot holes inside socket
            CreateSocketHole(socketObj.transform, new Vector2(-42, 12));
            CreateSocketHole(socketObj.transform, new Vector2(42, 12));

            // Socket hint text badge
            GameObject hintObj = CreateUIElement("SocketHint", socketObj.transform);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchoredPosition = new Vector2(0, -38);
            hintRt.sizeDelta = new Vector2(210, 32);
            TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "<b><color=#38BDF8>TWIN PRONG TERMINAL</color></b>\n<size=75%><color=#94A3B8>NEEDS EVEN PLUG</color></size>";
            hintTmp.fontSize = 13;
            hintTmp.alignment = TextAlignmentOptions.Center;

            // 7. Two Token Pedestals (Left: 17, Right: 24)
            Sprite plugSprite = LoadSprite("stone_twin_plug");

            task.token17Rect = CreateRichPlugToken(root.transform, "Token_17", new Vector2(-160, -90), "17", "ENDS IN 7 (ODD)", new Color(0.95f, 0.45f, 0.45f), plugSprite, out task.token17Button);
            task.token24Rect = CreateRichPlugToken(root.transform, "Token_24", new Vector2(160, -90), "24", "ENDS IN 4 (EVEN!)", new Color(0.35f, 0.95f, 1f), plugSprite, out task.token24Button);

            // Confetti Celebration Particles for Task 3
            task.confettiParticles = CreateConfettiEffect(root.transform, particleMat);

            return task;
        }

        private static RectTransform CreateRichPlugToken(Transform parent, string name, Vector2 pos, string val, string sublabel, Color subColor, Sprite sprite, out Button btn)
        {
            // Pedestal well
            GameObject pedObj = CreateUIElement("Pedestal_" + val, parent);
            RectTransform pedRt = pedObj.GetComponent<RectTransform>();
            pedRt.anchoredPosition = pos;
            pedRt.sizeDelta = new Vector2(150, 150);
            ProceduralImage pedImg = pedObj.AddComponent<ProceduralImage>();
            pedImg.color = new Color(0.04f, 0.07f, 0.12f, 0.9f);
            pedImg.BorderWidth = 0f;
            pedObj.AddComponent<UniformModifier>().Radius = 75f;

            // Draggable / clickable token
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(140, 140);

            // Drop shadow
            GameObject shadow = CreateUIElement("Shadow", obj.transform);
            RectTransform sRt = shadow.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, -6);
            sRt.sizeDelta = new Vector2(140, 140);
            ProceduralImage sImg = shadow.AddComponent<ProceduralImage>();
            sImg.color = new Color(0, 0, 0, 0.4f);
            shadow.AddComponent<UniformModifier>().Radius = 70f;

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();
            obj.AddComponent<HoverScale>().hoverScale = 1.10f;

            // Recessed dark readable center
            GameObject centerObj = CreateUIElement("CenterDisc", obj.transform);
            RectTransform cRt = centerObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0, -10);
            cRt.sizeDelta = new Vector2(80, 80);
            ProceduralImage cImg = centerObj.AddComponent<ProceduralImage>();
            cImg.color = new Color(0.03f, 0.06f, 0.12f, 0.88f);
            centerObj.AddComponent<UniformModifier>().Radius = 40f;

            GameObject textObj = CreateUIElement("ValText", obj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -5);
            textRt.sizeDelta = new Vector2(90, 50);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{val}</b>";
            tmp.fontSize = 42;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // Pedestal SubLabel badge
            GameObject badgeObj = CreateUIElement("Badge", obj.transform);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0, -82);
            badgeRt.sizeDelta = new Vector2(140, 30);
            ProceduralImage badgeBg = badgeObj.AddComponent<ProceduralImage>();
            badgeBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            badgeObj.AddComponent<UniformModifier>().Radius = 8f;

            GameObject bText = CreateUIElement("Text", badgeObj.transform);
            SetStretchFull(bText.GetComponent<RectTransform>());
            TextMeshProUGUI bTmp = bText.AddComponent<TextMeshProUGUI>();
            bTmp.text = $"<b>{sublabel}</b>";
            bTmp.fontSize = 11;
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.color = subColor;
            bTmp.raycastTarget = false;

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

        private static SquareTileTask CreateTask4SquareTile(Transform parent, Material particleMat)
        {
            GameObject root = CreateUIElement("Task4_SquareTile", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(510, 20);
            rootRt.sizeDelta = new Vector2(580, 560);

            SquareTileTask task = root.AddComponent<SquareTileTask>();

            // 1. Atmospheric Emerald Aura Halo
            GameObject auraObj = CreateUIElement("SquareAura", root.transform);
            RectTransform auraRt = auraObj.GetComponent<RectTransform>();
            auraRt.sizeDelta = new Vector2(620, 600);
            ProceduralImage auraImg = auraObj.AddComponent<ProceduralImage>();
            auraImg.color = new Color(0.12f, 0.75f, 0.45f, 0.20f);
            auraImg.BorderWidth = 0f;
            auraObj.AddComponent<UniformModifier>().Radius = 32f;

            // 2. Heavy Basalt Altar Frame
            ProceduralImage casingBg = root.AddComponent<ProceduralImage>();
            casingBg.color = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            casingBg.BorderWidth = 0f;
            root.AddComponent<UniformModifier>().Radius = 24f;

            // 3. Inner Emerald / Bronze Trim
            GameObject trimObj = CreateUIElement("SquareTrim", root.transform);
            RectTransform trimRt = trimObj.GetComponent<RectTransform>();
            trimRt.sizeDelta = new Vector2(550, 530);
            ProceduralImage trimImg = trimObj.AddComponent<ProceduralImage>();
            trimImg.color = new Color(0.10f, 0.35f, 0.25f, 0.40f);
            trimImg.BorderWidth = 0f;
            trimObj.AddComponent<UniformModifier>().Radius = 18f;

            // 4. Header Plaque Banner
            GameObject titleObj = CreateUIElement("SquareTitle", root.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 235);
            titleRt.sizeDelta = new Vector2(520, 50);
            ProceduralImage titleBg = titleObj.AddComponent<ProceduralImage>();
            titleBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            titleBg.BorderWidth = 0f;
            titleObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject titleTextObj = CreateUIElement("TitleText", titleObj.transform);
            SetStretchFull(titleTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#4ADE80>THE SQUARE TILE ARRAY</color></b>\n<size=62%><color=#94A3B8>WHICH BLOCK FORMS A PERFECT SQUARE (? x ? EQUAL ROWS AND COLS)?</color></size>";
            titleTmp.fontSize = 16;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 5. Large Square Slot Target
            GameObject slotObj = CreateUIElement("SquareSlotTarget", root.transform);
            RectTransform slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchoredPosition = new Vector2(0, 95);
            slotRt.sizeDelta = new Vector2(220, 220);
            task.squareSlotTarget = slotRt;

            ProceduralImage slotBg = slotObj.AddComponent<ProceduralImage>();
            slotBg.color = new Color(0.03f, 0.06f, 0.09f, 0.98f);
            slotBg.BorderWidth = 0f;
            slotObj.AddComponent<UniformModifier>().Radius = 16f;
            task.slotGreenGlow = slotBg;

            // Indentation text
            GameObject slotTextObj = CreateUIElement("IndentText", slotObj.transform);
            RectTransform slotTextRt = slotTextObj.GetComponent<RectTransform>();
            slotTextRt.sizeDelta = new Vector2(200, 80);
            TextMeshProUGUI slotTmp = slotTextObj.AddComponent<TextMeshProUGUI>();
            slotTmp.text = "<size=72%><color=#94A3B8>SQUARE SOCKET</color></size>\n<b><color=#4ADE80>? x ? = EQUAL SIDES</color></b>";
            slotTmp.fontSize = 20;
            slotTmp.alignment = TextAlignmentOptions.Center;

            // 4x4 Grid Display (revealed on solve)
            GameObject grid4x4Obj = CreateUIElement("Grid4x4Display", slotObj.transform);
            RectTransform gridRt = grid4x4Obj.GetComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(200, 200);
            task.solved4x4GridDisplay = grid4x4Obj;
            GridLayoutGroup glg = grid4x4Obj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(44, 44);
            glg.spacing = new Vector2(6, 6);
            for (int t = 0; t < 16; t++)
            {
                GameObject tile = CreateUIElement("Tile_" + t, grid4x4Obj.transform);
                ProceduralImage pImg = tile.AddComponent<ProceduralImage>();
                pImg.color = new Color(0.2f, 0.95f, 0.45f, 0.9f);
                tile.AddComponent<UniformModifier>().Radius = 6f;
            }

            // 6. Three Choice Blocks: 12, 16, 18 (using ice_shelf_key_tile for high clarity)
            Sprite tileSprite = LoadSprite("ice_shelf_key_tile");
            if (tileSprite == null) tileSprite = LoadSprite("stone_rune_tile");

            task.block12Rect = CreateRichTileChoice(root.transform, "Block_12", new Vector2(-160, -95), "12", "3 x 4 RECT", new Color(0.6f, 0.85f, 1f), tileSprite, out task.block12Button);
            task.block16Rect = CreateRichTileChoice(root.transform, "Block_16", new Vector2(0, -95), "16", "4 x 4 SQUARE!", new Color(0.35f, 0.95f, 0.45f), tileSprite, out task.block16Button);
            task.block18Rect = CreateRichTileChoice(root.transform, "Block_18", new Vector2(160, -95), "18", "3 x 6 RECT", new Color(0.95f, 0.85f, 0.4f), tileSprite, out task.block18Button);

            // Confetti Celebration Particles for Task 4
            task.confettiParticles = CreateConfettiEffect(root.transform, particleMat);

            return task;
        }

        private static RectTransform CreateRichTileChoice(Transform parent, string name, Vector2 pos, string val, string sublabel, Color subColor, Sprite sprite, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(125, 125);

            // Drop shadow
            GameObject shadow = CreateUIElement("DropShadow", obj.transform);
            RectTransform sRt = shadow.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, -5);
            sRt.sizeDelta = new Vector2(125, 125);
            ProceduralImage sImg = shadow.AddComponent<ProceduralImage>();
            sImg.color = new Color(0, 0, 0, 0.45f);
            shadow.AddComponent<UniformModifier>().Radius = 16f;

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();
            obj.AddComponent<HoverScale>().hoverScale = 1.08f;

            // Recessed dark plate for 100% text readability
            GameObject backplateObj = CreateUIElement("Backplate", obj.transform);
            RectTransform bpRt = backplateObj.GetComponent<RectTransform>();
            bpRt.sizeDelta = new Vector2(86, 86);
            ProceduralImage bpImg = backplateObj.AddComponent<ProceduralImage>();
            bpImg.color = new Color(0.03f, 0.06f, 0.12f, 0.90f);
            backplateObj.AddComponent<UniformModifier>().Radius = 14f;

            // Huge bold number
            GameObject numObj = CreateUIElement("NumText", obj.transform);
            RectTransform numRt = numObj.GetComponent<RectTransform>();
            numRt.anchoredPosition = new Vector2(0, 10);
            numRt.sizeDelta = new Vector2(86, 50);
            TextMeshProUGUI numTmp = numObj.AddComponent<TextMeshProUGUI>();
            numTmp.text = $"<b>{val}</b>";
            numTmp.fontSize = 44;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.raycastTarget = false;

            // Sublabel text
            GameObject subObj = CreateUIElement("SubText", obj.transform);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, -26);
            subRt.sizeDelta = new Vector2(110, 24);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = $"<b>{sublabel}</b>";
            subTmp.fontSize = 10;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = subColor;
            subTmp.raycastTarget = false;

            return rt;
        }

        private static FactorWheelTask CreateTask5FactorWheel(Transform parent, Material particleMat)
        {
            GameObject root = CreateUIElement("Task5_FactorWheel", parent);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0, 20);
            rootRt.sizeDelta = new Vector2(640, 680);

            FactorWheelTask task = root.AddComponent<FactorWheelTask>();

            // 1. Atmospheric Golden Wheel Aura Halo
            GameObject auraObj = CreateUIElement("WheelAura", root.transform);
            RectTransform auraRt = auraObj.GetComponent<RectTransform>();
            auraRt.sizeDelta = new Vector2(540, 540);
            ProceduralImage auraImg = auraObj.AddComponent<ProceduralImage>();
            auraImg.color = new Color(1f, 0.85f, 0.2f, 0.20f);
            auraImg.BorderWidth = 0f;
            auraObj.AddComponent<RoundModifier>();

            // 2. Header Plaque Banner
            GameObject titleObj = CreateUIElement("WheelTitle", root.transform);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 280);
            titleRt.sizeDelta = new Vector2(560, 52);
            ProceduralImage titleBg = titleObj.AddComponent<ProceduralImage>();
            titleBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            titleBg.BorderWidth = 0f;
            titleObj.AddComponent<UniformModifier>().Radius = 14f;

            GameObject titleTextObj = CreateUIElement("TitleText", titleObj.transform);
            SetStretchFull(titleTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI titleTmp = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#FDE047>THE CLEAN FIT FACTOR WHEEL</color></b>\n<size=62%><color=#94A3B8>18 SLOTS ON RING | WHICH PEG DIVIDES 18 WITH ZERO REMAINDER?</color></size>";
            titleTmp.fontSize = 17;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 3. Center Wheel
            GameObject wheelObj = CreateUIElement("StoneDialWheel", root.transform);
            RectTransform wheelRt = wheelObj.GetComponent<RectTransform>();
            wheelRt.sizeDelta = new Vector2(440, 440);
            task.wheelRect = wheelRt;

            Image wheelImg = wheelObj.AddComponent<Image>();
            Sprite wheelSprite = LoadSprite("stone_dial_wheel");
            if (wheelSprite != null) wheelImg.sprite = wheelSprite;
            task.wheelGlowImage = wheelImg;

            // 4. Factor Indicators Group: All 18 Slots around perimeter (Radius 152px, 20 deg each)
            GameObject indObj = CreateUIElement("FactorIndicators", wheelObj.transform);
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            SetStretchFull(indRt);
            task.factorIndicatorsGroup = indObj;

            // 6 equal groups of 3 slots (18 / 3 = 6)
            Color[] groupColors = new Color[]
            {
                new Color(1f, 0.88f, 0.2f, 1f),   // Group 0: Radiant Gold
                new Color(0.35f, 0.95f, 1f, 1f),  // Group 1: Cyan
                new Color(1f, 0.88f, 0.2f, 1f),   // Group 2: Radiant Gold
                new Color(0.35f, 0.95f, 1f, 1f),  // Group 3: Cyan
                new Color(1f, 0.88f, 0.2f, 1f),   // Group 4: Radiant Gold
                new Color(0.35f, 0.95f, 1f, 1f)   // Group 5: Cyan
            };

            for (int i = 0; i < 18; i++)
            {
                float angle = (i * 20f) * Mathf.Deg2Rad;
                GameObject node = CreateUIElement("FactorNode_" + i, indObj.transform);
                RectTransform nRt = node.GetComponent<RectTransform>();
                nRt.anchoredPosition = new Vector2(Mathf.Cos(angle) * 152f, Mathf.Sin(angle) * 152f);
                nRt.sizeDelta = new Vector2(22, 22);

                ProceduralImage pImg = node.AddComponent<ProceduralImage>();
                int groupIdx = i / 3;
                pImg.color = groupColors[groupIdx];
                node.AddComponent<RoundModifier>();

                // Inner bright dot
                GameObject core = CreateUIElement("Core", node.transform);
                RectTransform cRt = core.GetComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(10, 10);
                ProceduralImage cImg = core.AddComponent<ProceduralImage>();
                cImg.color = Color.white;
                core.AddComponent<RoundModifier>();
            }

            // Wheel Info Footer Plaque
            GameObject infoObj = CreateUIElement("WheelInfoText", root.transform);
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchoredPosition = new Vector2(0, -240);
            infoRt.sizeDelta = new Vector2(460, 36);
            ProceduralImage infoBg = infoObj.AddComponent<ProceduralImage>();
            infoBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            infoObj.AddComponent<UniformModifier>().Radius = 9f;

            GameObject iText = CreateUIElement("Text", infoObj.transform);
            SetStretchFull(iText.GetComponent<RectTransform>());
            TextMeshProUGUI infoTmp = iText.AddComponent<TextMeshProUGUI>();
            infoTmp.text = "<b><color=#FDE047>18 / 3 = 6 EQUAL SETS</color> <color=#94A3B8>| ZERO REMAINDER!</color></b>";
            infoTmp.fontSize = 14;
            infoTmp.alignment = TextAlignmentOptions.Center;

            // 5. Three Peg Choices: 4, 3, 8 with rich pedestals
            Sprite plugSprite = LoadSprite("stone_twin_plug");
            task.peg4Rect = CreateRichPegButton(root.transform, "Peg_4", new Vector2(-160, -315), "PEG 4", "18/4 = R2", new Color(0.9f, 0.45f, 0.45f), plugSprite, out task.peg4Button);
            task.peg3Rect = CreateRichPegButton(root.transform, "Peg_3", new Vector2(0, -315), "PEG 3", "18/3 = 6 (EXACT!)", new Color(0.35f, 0.95f, 0.45f), plugSprite, out task.peg3Button);
            task.peg8Rect = CreateRichPegButton(root.transform, "Peg_8", new Vector2(160, -315), "PEG 8", "18/8 = R2", new Color(0.9f, 0.45f, 0.45f), plugSprite, out task.peg8Button);

            // Confetti Celebration Particles for Task 5
            task.confettiParticles = CreateConfettiEffect(root.transform, particleMat);

            return task;
        }

        private static RectTransform CreateRichPegButton(Transform parent, string name, Vector2 pos, string val, string sublabel, Color subColor, Sprite sprite, out Button btn)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(120, 120);

            // Drop shadow
            GameObject shadow = CreateUIElement("Shadow", obj.transform);
            RectTransform sRt = shadow.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, -5);
            sRt.sizeDelta = new Vector2(120, 120);
            ProceduralImage sImg = shadow.AddComponent<ProceduralImage>();
            sImg.color = new Color(0, 0, 0, 0.4f);
            shadow.AddComponent<UniformModifier>().Radius = 60f;

            Image img = obj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            btn = obj.AddComponent<Button>();
            obj.AddComponent<HoverScale>().hoverScale = 1.10f;

            // Center Dark Plate
            GameObject centerObj = CreateUIElement("CenterDisc", obj.transform);
            RectTransform cRt = centerObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0, -8);
            cRt.sizeDelta = new Vector2(80, 50);
            ProceduralImage cImg = centerObj.AddComponent<ProceduralImage>();
            cImg.color = new Color(0.03f, 0.06f, 0.12f, 0.90f);
            centerObj.AddComponent<UniformModifier>().Radius = 12f;

            GameObject textObj = CreateUIElement("PegText", obj.transform);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -8);
            textRt.sizeDelta = new Vector2(90, 44);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{val}</b>";
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // Sublabel Badge
            GameObject badgeObj = CreateUIElement("Badge", obj.transform);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(0, -68);
            badgeRt.sizeDelta = new Vector2(120, 26);
            ProceduralImage badgeBg = badgeObj.AddComponent<ProceduralImage>();
            badgeBg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
            badgeObj.AddComponent<UniformModifier>().Radius = 8f;

            GameObject bText = CreateUIElement("Text", badgeObj.transform);
            SetStretchFull(bText.GetComponent<RectTransform>());
            TextMeshProUGUI bTmp = bText.AddComponent<TextMeshProUGUI>();
            bTmp.text = $"<b>{sublabel}</b>";
            bTmp.fontSize = 10;
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.color = subColor;
            bTmp.raycastTarget = false;

            return rt;
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

            // Deep Cinematic Backdrop Overlay (Replaces blinding 85% solid yellow flood!)
            GameObject goldObj = CreateUIElement("GoldenLightFlood", root.transform);
            SetStretchFull(goldObj.GetComponent<RectTransform>());
            Image gImg = goldObj.AddComponent<Image>();
            gImg.color = new Color(0.03f, 0.05f, 0.09f, 0.92f);
            vp.goldenLightFlood = gImg;

            // Radiant Golden Sunburst Center Aura behind trophy
            GameObject sunburst = CreateUIElement("SunburstHalo", root.transform);
            RectTransform sbRt = sunburst.GetComponent<RectTransform>();
            sbRt.anchoredPosition = new Vector2(0, 150);
            sbRt.sizeDelta = new Vector2(500, 500);
            ProceduralImage sbImg = sunburst.AddComponent<ProceduralImage>();
            sbImg.color = new Color(1f, 0.85f, 0.2f, 0.25f);
            sunburst.AddComponent<RoundModifier>();

            // Victory Trophy
            GameObject trophyObj = CreateUIElement("GemstoneTrophy", root.transform);
            RectTransform trRt = trophyObj.GetComponent<RectTransform>();
            trRt.anchoredPosition = new Vector2(0, 150);
            trRt.sizeDelta = new Vector2(300, 300);
            Image trImg = trophyObj.AddComponent<Image>();
            Sprite trSprite = LoadSprite("gemstone_trophy");
            if (trSprite != null) trImg.sprite = trSprite;
            vp.trophyRect = trRt;
            vp.trophyImage = trImg;

            // Victory Title
            GameObject titleObj = CreateUIElement("VictoryTitle", root.transform);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 280);
            tRt.sizeDelta = new Vector2(1200, 70);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b><color=#FDE047>THE MASTER PUZZLE WALL IS UNLOCKED!</color></b>\n<size=52%><color=#E2E8F0>YOU ARE OFFICIALLY A MASTER PATTERN SOLVER!</color></size>";
            tTmp.fontSize = 36;
            tTmp.alignment = TextAlignmentOptions.Center;
            vp.titleText = tTmp;

            // GRAND PROMINENT SCORE & STARS PLAQUE (High contrast, impossible to miss!)
            GameObject scoreBox = CreateUIElement("GrandScorePlaque", root.transform);
            RectTransform scoreBoxRt = scoreBox.GetComponent<RectTransform>();
            scoreBoxRt.anchoredPosition = new Vector2(0, -35);
            scoreBoxRt.sizeDelta = new Vector2(680, 110);
            ProceduralImage scoreBoxBg = scoreBox.AddComponent<ProceduralImage>();
            scoreBoxBg.color = new Color(0.06f, 0.10f, 0.18f, 0.98f);
            scoreBoxBg.BorderWidth = 0f;
            scoreBox.AddComponent<UniformModifier>().Radius = 18f;

            GameObject scoreObj = CreateUIElement("FinalScoreText", scoreBox.transform);
            SetStretchFull(scoreObj.GetComponent<RectTransform>());
            TextMeshProUGUI scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.text = "<size=60%><color=#94A3B8>TOTAL PUZZLE SCORE</color></size>\n<b><color=#FDE047><size=130%>24,550 POINTS</size></color></b>\n<size=80%><color=#FCD34D><b>3 / 3 STARS EARNED</b></color></size>";
            scoreTmp.fontSize = 24;
            scoreTmp.alignment = TextAlignmentOptions.Center;
            scoreTmp.color = Color.white;
            vp.finalScoreText = scoreTmp;

            // Recap Container
            GameObject recapObj = CreateUIElement("LearningRecapContainer", root.transform);
            RectTransform recapRt = recapObj.GetComponent<RectTransform>();
            recapRt.anchoredPosition = new Vector2(0, -175);
            recapRt.sizeDelta = new Vector2(1150, 140);
            vp.recapContainer = recapObj;

            ProceduralImage recapBg = recapObj.AddComponent<ProceduralImage>();
            recapBg.color = new Color(0.07f, 0.10f, 0.16f, 0.95f);
            recapBg.BorderWidth = 0f;
            recapObj.AddComponent<UniformModifier>().Radius = 16f;

            GameObject recapTextObj = CreateUIElement("RecapText", recapObj.transform);
            RectTransform rtText = recapTextObj.GetComponent<RectTransform>();
            SetStretchFull(rtText);
            TextMeshProUGUI rtTmp = recapTextObj.AddComponent<TextMeshProUGUI>();
            rtTmp.text = "<b><color=#38BDF8>MASTER PATTERN SOLVER RECAP</color></b>\n" +
                         "<size=90%>- <b>Negative Numbers:</b> Below zero use a minus sign and decrease downward.\n" +
                         "- <b>Even Numbers:</b> End in 0, 2, 4, 6, 8 and split evenly into two equal parts.\n" +
                         "- <b>Square Numbers:</b> Form perfect square arrays (4 x 4 = 16) with identical rows & columns.\n" +
                         "- <b>Divisibility:</b> Factors divide evenly with 0 remainder (18 / 3 = 6). Any number ending in 0 divides by 10!</size>";
            rtTmp.fontSize = 16;
            rtTmp.alignment = TextAlignmentOptions.Center;
            rtTmp.color = new Color(0.9f, 0.95f, 1f);

            // Play Again Button
            GameObject playAgainBtn = CreateButton("PlayAgainBtn", root.transform, new Vector2(-150, -315), new Vector2(240, 60), "PLAY AGAIN", 22);
            vp.playAgainButton = playAgainBtn.GetComponent<Button>();

            // Main Menu Button
            GameObject menuBtn = CreateButton("MainMenuBtn", root.transform, new Vector2(150, -315), new Vector2(240, 60), "MAIN MENU", 22);
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
            mm.playButton = gemBtn;

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
            pImg.raycastTarget = true;
            btnObj.AddComponent<UniformModifier>().Radius = 14f;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = pImg;
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
            tmp.raycastTarget = false;

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

        private static ParticleSystem CreateConfettiEffect(Transform parent, Material mat)
        {
            GameObject pObj = new GameObject("ConfettiCelebration");
            pObj.transform.SetParent(parent, false);
            pObj.transform.localPosition = new Vector3(0, 0, -5f);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 28f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(320f, 620f);
            main.gravityModifier = 1.5f;
            main.maxParticles = 120;
            main.loop = false;
            main.playOnAwake = false;

            // Multi-colored celebratory festive confetti!
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 0.9f, 1f), 0.0f),    // Ice Cyan
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.25f),  // Gold
                    new GradientColorKey(new Color(1f, 0.3f, 0.65f), 0.5f),   // Magenta Pink
                    new GradientColorKey(new Color(0.3f, 1f, 0.5f), 0.75f),   // Emerald Green
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 1.0f)    // Tangerine
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 75),
                new ParticleSystem.Burst(0.2f, 35)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 25f;
            shape.rotation = new Vector3(-90f, 0, 0); // upward fountain

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-360f, 360f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = grad;

            ParticleSystemRenderer renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 45;
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
