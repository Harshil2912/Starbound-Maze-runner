using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIPolishApplier
{
    const string HdDir = "Assets/UI/HD";
    const string FontDir = "Assets/Fonts";

    [MenuItem("Tools/UI Polish/Apply HD UI Polish To Scene")]
    public static void ApplyUIPolishToScene()
    {
        // 0. Ensure all generated HD assets are present
        if (!File.Exists($"{HdDir}/Starbound_Logo_CyberCyan.png") ||
            !File.Exists($"{HdDir}/UI_Panel_Modal_HD.png"))
        {
            UIPolishGenerator.GenerateAllHDAssets();
        }

        // Load HD sprites
        Sprite spModal = LoadSprite("UI_Panel_Modal_HD");
        Sprite spBanner = LoadSprite("UI_Banner_HD");
        Sprite spBtnPrimary = LoadSprite("UI_Btn_Primary_HD");
        Sprite spBtnTech = LoadSprite("UI_Btn_Tech_HD");
        Sprite spBtnTab = LoadSprite("UI_Btn_Tab_HD");
        Sprite spCardLevel = LoadSprite("UI_Card_Level_HD");
        Sprite spHudPad = LoadSprite("UI_HudPad_HD");
        Sprite spEditFrame = LoadSprite("UI_EditFrame_HD");
        Sprite spSliderTrack = LoadSprite("UI_Slider_Track_HD");
        Sprite spSliderFill = LoadSprite("UI_Slider_Fill_HD");
        Sprite spSliderThumb = LoadSprite("UI_Slider_Thumb_HD");
        Sprite spHeartFull = LoadSprite("UI_Heart_Full_HD");
        Sprite spHeartEmpty = LoadSprite("UI_Heart_Empty_HD");
        Sprite spChevron = LoadSprite("UI_Icon_Chevron_HD");
        Sprite spPauseIcon = LoadSprite("UI_Icon_Pause_HD");
        Sprite spCoreRing = LoadSprite("UI_HudCoreRing_HD");
        Sprite spCoreOrb = LoadSprite("UI_HudCoreOrb_HD");
        Sprite spScanIcon = LoadSprite("UI_Icon_Scan_HD");
        Sprite spTitleGlow = LoadSprite("UI_TitleGlow_HD");
        Sprite spEmblem = LoadSprite("Starbound_Emblem_HD");

        // Load stylized game logo sprites
        Sprite spLogoCyber = LoadSprite("Starbound_Logo_CyberCyan");
        Sprite spLogoChrome = LoadSprite("Starbound_Logo_ChromeCrimson");
        Sprite spLogoMecha = LoadSprite("Starbound_Logo_MechaSpeed");
        Sprite spLogoCosmic = LoadSprite("Starbound_Logo_CosmicMinimal");

        // Load crisp TrueType fonts
        Font fAgency = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Agency-Bold.ttf");
        Font fBahn = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Bahnschrift.ttf");
        Font fBlack = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Roboto-Black.ttf");
        Font fBold = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Roboto-Bold.ttf");
        Font fMed = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Roboto-Medium.ttf");
        Font fMono = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/Cascadia.ttf");

        if (fBlack == null) fBlack = fBold;
        if (fAgency == null) fAgency = fBlack;
        if (fBahn == null) fBahn = fBold;
        if (fMed == null) fMed = fBold;
        if (fMono == null) fMono = fBold;

        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[UIPolishApplier] No Canvas found in scene.");
            return;
        }
        Transform root = canvas.transform;

        // Normalize all Button transitions to None and attach UIButtonFeedback for uniform interaction
        foreach (var btn in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn == null) continue;
            Undo.RecordObject(btn, "Normalize Button Interaction");
            btn.transition = Selectable.Transition.None;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            var img = btn.GetComponent<Image>();
            var fb = btn.GetComponent<UIButtonFeedback>();
            if (fb == null) fb = Undo.AddComponent<UIButtonFeedback>(btn.gameObject);
            if (img != null) fb.SetOriginalColor(img.color);
            EditorUtility.SetDirty(btn);
        }

        // 1. Update GameUI heart references
        var gameUI = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (gameUI != null)
        {
            Undo.RecordObject(gameUI, "Update GameUI HD Hearts");
            if (spHeartFull != null) gameUI.heartFull = spHeartFull;
            if (spHeartEmpty != null) gameUI.heartEmpty = spHeartEmpty;
            EditorUtility.SetDirty(gameUI);
        }

        // 2. Polish HUD_SafeArea
        Transform hud = root.Find("HUD_SafeArea");
        if (hud != null)
        {
            // Mode & View pills
            StyleButton(hud.Find("Btn_Mode"), spBtnTech, new Color(0.08f, 0.14f, 0.30f, 0.86f), 324, 54, new Vector2(28, -22));
            StyleText(hud.Find("Btn_Mode/Label"), fBold, 19, FontStyle.Bold, Color.white, true, false);

            StyleButton(hud.Find("Btn_ViewMode"), spBtnTech, new Color(0.08f, 0.22f, 0.38f, 0.90f), 324, 54, new Vector2(28, -84));
            StyleText(hud.Find("Btn_ViewMode/Label"), fBold, 19, FontStyle.Bold, new Color(0.76f, 0.97f, 1f, 1f), true, false);

            // Hearts
            for (int i = 1; i <= 3; i++)
            {
                Transform h = hud.Find("Heart" + i);
                if (h != null)
                {
                    StyleImage(h, spHeartFull, Image.Type.Simple, Color.white);
                    SetRectSize(h, 46, 46);
                }
            }

            // Center Telemetry
            StyleText(hud.Find("Txt_Level"), fBlack, 27, FontStyle.Normal, new Color(0.94f, 0.98f, 1f, 1f), true, true);
            StyleText(hud.Find("Txt_Cores"), fMono, 23, FontStyle.Bold, new Color(0.40f, 1f, 0.90f, 1f), true, true);
            StyleText(hud.Find("Txt_Shield"), fMono, 22, FontStyle.Bold, new Color(0.54f, 0.90f, 1f, 1f), true, true);
            Transform msg = hud.Find("Txt_Message");
            if (msg != null)
            {
                StyleText(msg, fBlack, 48, FontStyle.Normal, new Color(1f, 0.95f, 0.62f, 1f), true, true);
                var t = msg.GetComponent<Text>();
                if (t != null)
                {
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 26;
                    t.resizeTextMaxSize = 54;
                }
            }

            // Top-right Pause + Reposition
            Transform pauseBtn = hud.Find("Btn_Pause");
            if (pauseBtn != null)
            {
                StyleButton(pauseBtn, spHudPad, new Color(0.08f, 0.14f, 0.30f, 0.88f), 80, 80, new Vector2(-28, -22));
                Transform bar0 = pauseBtn.Find("Bar0");
                Transform bar1 = pauseBtn.Find("Bar1");
                if (bar0 != null) bar0.gameObject.SetActive(false);
                if (bar1 != null) bar1.gameObject.SetActive(false);
                Transform iconPause = EnsureChildImage(pauseBtn, "Icon_Pause", spPauseIcon, new Vector2(40, 40), Vector2.zero, new Color(0.88f, 0.98f, 1f, 1f));
                iconPause.SetAsLastSibling();
            }

            Transform repoBtn = hud.Find("Btn_AR_Reposition");
            if (repoBtn != null)
            {
                StyleButton(repoBtn, spBtnTech, new Color(0.09f, 0.34f, 0.50f, 0.92f), 228, 76, new Vector2(-120, -24));
                StyleText(repoBtn.Find("Label"), fBold, 22, FontStyle.Bold, new Color(0.68f, 0.98f, 1f, 1f), true, false);
            }

            // AR Guidance Banner
            Transform guidanceHud = hud.Find("AR_GuidanceHUD");
            if (guidanceHud != null)
            {
                StyleImage(guidanceHud, spBanner, Image.Type.Sliced, Color.white);
                SetRectSize(guidanceHud, 720, 82);
                EnsureChildImage(guidanceHud, "Icon_Scan", spScanIcon, new Vector2(36, 36), new Vector2(-320, 0), new Color(0.45f, 0.96f, 1f, 0.95f));
                Transform txtG = guidanceHud.Find("Txt_Guidance");
                if (txtG != null)
                {
                    var rt = txtG as RectTransform;
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(616, 66);
                        rt.anchoredPosition = new Vector2(24, 0);
                    }
                    StyleText(txtG, fMed, 20, FontStyle.Normal, new Color(0.86f, 0.97f, 1f, 1f), true, false);
                }
            }

            // DPad Cluster - Sleek tech slate, reverts to resting color, flashes greyish light on tap
            Transform dpad = hud.Find("DPadCluster");
            if (dpad != null)
            {
                Color slateDpad = new Color(0.18f, 0.26f, 0.40f, 0.90f);
                string[] dirs = { "Btn_Forward", "Btn_Back", "Btn_Left", "Btn_Right" };
                foreach (var d in dirs)
                {
                    Transform btn = dpad.Find(d);
                    if (btn == null) continue;
                    StyleImage(btn, spHudPad, Image.Type.Sliced, slateDpad);
                    var hold = btn.GetComponent<UIHoldButton>();
                    if (hold != null) hold.SetRestingColor(slateDpad);
                    var fb = btn.GetComponent<UIButtonFeedback>();
                    if (fb != null) fb.SetOriginalColor(slateDpad);

                    Transform arr = btn.Find("Arrow");
                    if (arr != null)
                    {
                        StyleImage(arr, spChevron, Image.Type.Simple, new Color(0.86f, 0.98f, 1f, 1f));
                        SetRectSize(arr, 48, 48);
                    }
                }
                Transform core = dpad.Find("Indicator_CubeColor");
                if (core != null)
                {
                    StyleImage(core, spCoreOrb, Image.Type.Simple, new Color(0.10f, 0.86f, 1f, 0.96f));
                    SetRectSize(core, 68, 68);
                    Transform ring = core.Find("OuterRing");
                    if (ring != null)
                    {
                        StyleImage(ring, spCoreRing, Image.Type.Simple, new Color(0.35f, 0.92f, 1f, 0.55f));
                    }
                }
                StyleImage(dpad.Find("DpadEditFrame"), spEditFrame, Image.Type.Sliced, new Color(0.20f, 0.94f, 1f, 0.92f));
            }

            // Jump & Stop Cluster
            Transform jumpCluster = hud.Find("JumpCluster");
            if (jumpCluster != null)
            {
                // STOP Button: Authentically RED at rest, flashes greyish light on tap, reverts to RED
                Transform btnStop = jumpCluster.Find("Btn_Stop");
                if (btnStop != null)
                {
                    Color redStop = new Color(0.86f, 0.18f, 0.24f, 1.0f);
                    StyleImage(btnStop, spHudPad, Image.Type.Sliced, redStop);
                    var act = btnStop.GetComponent<UIActionButton>();
                    if (act != null) act.action = UIActionButton.ActionType.Stop;
                    var fb = btnStop.GetComponent<UIButtonFeedback>();
                    if (fb != null) fb.SetOriginalColor(redStop);
                    StyleText(btnStop.Find("Label"), fBlack, 26, FontStyle.Normal, Color.white, true, false);
                }

                // JUMP Button: Sleek slate tech at rest, flashes greyish light on tap, reverts to slate
                Transform btnJump = jumpCluster.Find("Btn_Jump");
                if (btnJump != null)
                {
                    Color slateJump = new Color(0.18f, 0.26f, 0.40f, 0.90f);
                    StyleImage(btnJump, spHudPad, Image.Type.Sliced, slateJump);
                    var hold = btnJump.GetComponent<UIHoldButton>();
                    if (hold != null)
                    {
                        hold.isJump = true;
                        hold.SetRestingColor(slateJump);
                    }
                    var fb = btnJump.GetComponent<UIButtonFeedback>();
                    if (fb != null) fb.SetOriginalColor(slateJump);
                    StyleText(btnJump.Find("Label"), fBlack, 34, FontStyle.Normal, Color.white, true, true);
                }
                StyleImage(jumpCluster.Find("JumpEditFrame"), spEditFrame, Image.Type.Sliced, new Color(0.20f, 0.94f, 1f, 0.92f));
            }

            // MoveControlsOverlay
            Transform overlay = hud.Find("MoveControlsOverlay");
            if (overlay != null)
            {
                StyleImage(overlay, spBanner, Image.Type.Sliced, Color.white);
                StyleText(overlay.Find("Txt_SelectedControl"), fBold, 20, FontStyle.Bold, new Color(0.55f, 0.96f, 1f, 1f), true, false);
                StyleSlider(overlay.Find("Slider_OverlayScale"), spSliderTrack, spSliderFill, spSliderThumb);
                StyleText(overlay.Find("Txt_OverlayScale"), fMono, 19, FontStyle.Bold, Color.white, true, false);
                StyleImage(overlay.Find("Btn_OverlayReset"), spBtnTech, Image.Type.Sliced, new Color(0.62f, 0.22f, 0.25f, 1f));
                StyleText(overlay.Find("Btn_OverlayReset/Label"), fBold, 20, FontStyle.Bold, Color.white, true, false);
                StyleImage(overlay.Find("Btn_OverlayDone"), spBtnTech, Image.Type.Sliced, new Color(0.11f, 0.65f, 0.54f, 1f));
                StyleText(overlay.Find("Btn_OverlayDone/Label"), fBold, 20, FontStyle.Bold, Color.white, true, false);
            }
        }

        // 3. Polish PauseMenu & SettingsPanel (Preserve iconic distinct colors, greyish light on tap)
        Transform pauseMenu = root.Find("PauseMenu");
        if (pauseMenu != null)
        {
            Transform card = pauseMenu.Find("Card");
            if (card != null)
            {
                SetRect(card, new Vector2(680, 820), Vector2.zero);
                StyleImage(card, spModal, Image.Type.Sliced, Color.white);
                Transform pTitle = card.Find("Title");
                SetRect(pTitle, new Vector2(560, 76), new Vector2(0, 336));
                StyleText(pTitle, fBlack, 56, FontStyle.Normal, new Color(0.90f, 0.98f, 1f, 1f), true, true);

                SetRect(card.Find("Btn_Resume"), new Vector2(510, 78), new Vector2(0, 224));
                SetRect(card.Find("Btn_RetryLevel"), new Vector2(510, 78), new Vector2(0, 132));
                SetRect(card.Find("Btn_RestartGame"), new Vector2(510, 78), new Vector2(0, 40));
                SetRect(card.Find("Btn_Settings"), new Vector2(510, 78), new Vector2(0, -52));
                SetRect(card.Find("Btn_MainMenu"), new Vector2(510, 78), new Vector2(0, -144));

                // Authentic distinctive button colors:
                StyleActionBtn(card.Find("Btn_Resume"), spBtnPrimary, new Color(0.10f, 0.62f, 0.50f, 1f), fBold, 32);       // Green
                StyleActionBtn(card.Find("Btn_RetryLevel"), spBtnPrimary, new Color(0.84f, 0.52f, 0.11f, 1f), fBold, 32);   // Orange
                StyleActionBtn(card.Find("Btn_RestartGame"), spBtnPrimary, new Color(0.78f, 0.20f, 0.30f, 1f), fBold, 32);  // Red
                StyleActionBtn(card.Find("Btn_Settings"), spBtnPrimary, new Color(0.13f, 0.44f, 0.78f, 1f), fBold, 32);     // Blue
                StyleActionBtn(card.Find("Btn_MainMenu"), spBtnPrimary, new Color(0.26f, 0.33f, 0.68f, 1f), fBold, 32);     // Purple

                Transform hint = card.Find("Hint");
                if (hint != null)
                {
                    SetRect(hint, new Vector2(610, 96), new Vector2(0, -296));
                    StyleText(hint, fMed, 19, FontStyle.Normal, new Color(0.72f, 0.91f, 1f, 0.95f), true, false);
                    var ht = hint.GetComponent<Text>();
                    if (ht != null)
                    {
                        ht.lineSpacing = 1.2f;
                        ht.text = "<b>TIP</b>  •  Flickering hazards & tiles are holographic decoys!\nPause anytime with <b>Esc / P</b>  •  Toggle Hold / Tap in HUD";
                    }
                }
            }

            Transform settings = pauseMenu.Find("SettingsPanel");
            if (settings != null)
            {
                StyleImage(settings, spModal, Image.Type.Sliced, Color.white);
                StyleText(settings.Find("Title"), fBlack, 40, FontStyle.Normal, new Color(0.90f, 0.98f, 1f, 1f), true, false);

                StyleActionBtn(settings.Find("Btn_TabGeneral"), spBtnTab, new Color(0.12f, 0.55f, 0.85f, 1f), fBold, 20);
                StyleActionBtn(settings.Find("Btn_TabControls"), spBtnTab, new Color(0.12f, 0.18f, 0.34f, 0.9f), fBold, 20);
                StyleActionBtn(settings.Find("Btn_TabDebug"), spBtnTab, new Color(0.12f, 0.18f, 0.34f, 0.9f), fBold, 20);
                StyleActionBtn(settings.Find("Btn_SettingsBack"), spBtnPrimary, new Color(0.18f, 0.27f, 0.46f, 1f), fBold, 24);

                Transform tabGen = settings.Find("Tab_General");
                if (tabGen != null)
                {
                    string[] lbls = { "Lbl_MoveMode", "Lbl_ViewMode", "Lbl_Music", "Lbl_Sfx", "Lbl_Haptics" };
                    foreach (var l in lbls) StyleText(tabGen.Find(l), fBold, 23, FontStyle.Bold, new Color(0.86f, 0.95f, 1f, 1f), true, false);

                    StyleActionBtn(tabGen.Find("Btn_GeneralMoveMode"), spBtnTech, new Color(0.11f, 0.45f, 0.72f, 1f), fBold, 21);
                    StyleActionBtn(tabGen.Find("Btn_GeneralViewMode"), spBtnTech, new Color(0.09f, 0.38f, 0.56f, 1f), fBold, 21);
                    StyleActionBtn(tabGen.Find("Btn_GeneralMusic"), spBtnTech, new Color(0.09f, 0.38f, 0.56f, 1f), fBold, 21);
                    StyleActionBtn(tabGen.Find("Btn_GeneralSfx"), spBtnTech, new Color(0.09f, 0.38f, 0.56f, 1f), fBold, 21);
                    StyleActionBtn(tabGen.Find("Btn_GeneralHaptics"), spBtnTech, new Color(0.09f, 0.38f, 0.56f, 1f), fBold, 21);
                    StyleText(tabGen.Find("Txt_GeneralHint"), fMed, 19, FontStyle.Normal, new Color(0.66f, 0.86f, 0.96f, 0.92f), true, false);
                }

                Transform tabCtrl = settings.Find("Tab_MoveControls");
                if (tabCtrl != null)
                {
                    StyleText(tabCtrl.Find("Txt_SelectHdr"), fBold, 21, FontStyle.Bold, new Color(0.75f, 0.92f, 1f, 1f), true, false);
                    StyleActionBtn(tabCtrl.Find("Btn_SelectDpad"), spBtnTech, new Color(0.10f, 0.65f, 0.55f, 1f), fBold, 22);
                    StyleActionBtn(tabCtrl.Find("Btn_SelectJump"), spBtnTech, new Color(0.14f, 0.20f, 0.36f, 0.9f), fBold, 22);
                    StyleText(tabCtrl.Find("Txt_ScaleValue"), fMono, 22, FontStyle.Bold, new Color(0.48f, 0.95f, 1f, 1f), true, false);
                    StyleSlider(tabCtrl.Find("Slider_Scale"), spSliderTrack, spSliderFill, spSliderThumb);
                    StyleActionBtn(tabCtrl.Find("Btn_ResetControls"), spBtnTech, new Color(0.58f, 0.22f, 0.25f, 1f), fBold, 22);
                    StyleActionBtn(tabCtrl.Find("Btn_CustomizeOnScreen"), spBtnPrimary, new Color(0.11f, 0.58f, 0.52f, 1f), fBold, 22);
                }

                Transform tabDebug = settings.Find("Tab_Debug");
                if (tabDebug != null)
                {
                    StyleText(tabDebug.Find("Lbl_DebugPlaneVisual"), fBold, 23, FontStyle.Bold, new Color(0.86f, 0.95f, 1f, 1f), true, false);
                    StyleActionBtn(tabDebug.Find("Btn_DebugPlaneVisual"), spBtnTech, new Color(0.14f, 0.36f, 0.58f, 1f), fBold, 20);
                    StyleText(tabDebug.Find("Txt_DebugHint"), fMed, 19, FontStyle.Normal, new Color(0.68f, 0.87f, 0.96f, 0.92f), true, false);
                }
            }
        }

        // 4. Polish MainMenu (Red tagline inside bottom blue UI element + stylized title typography)
        Transform mainMenu = root.Find("MainMenu");
        if (mainMenu != null)
        {
            Transform glow = EnsureChildImage(mainMenu, "Img_TitleGlow", spTitleGlow, new Vector2(1160, 380), new Vector2(0, 160), new Color(1f, 1f, 1f, 0.92f));
            glow.SetAsFirstSibling();

            Transform emblem = EnsureChildImage(mainMenu, "Img_StarboundEmblem", spEmblem, new Vector2(210, 210), new Vector2(0, 310), Color.white);
            emblem.SetSiblingIndex(1);

            // Title: Styled with futuristic Agency-Bold TrueType font (or can be swapped to graphic logo)
            Transform title = mainMenu.Find("Title");
            if (title != null)
            {
                SetRect(title, new Vector2(1400, 120), new Vector2(0, 155));
                StyleText(title, fAgency, 98, FontStyle.Normal, new Color(0.92f, 0.98f, 1f, 1f), true, true);
                var tt = title.GetComponent<Text>();
                if (tt != null) tt.text = "STARBOUND : MAZE RUNNER";
            }

            // Old subtitle above Play button is hidden to give clean breathing room
            Transform oldSubtitle = mainMenu.Find("Subtitle");
            if (oldSubtitle != null)
            {
                oldSubtitle.gameObject.SetActive(false);
            }

            Transform playBtn = mainMenu.Find("Btn_Play");
            if (playBtn != null)
            {
                SetRect(playBtn, new Vector2(480, 96), new Vector2(0, -18));
                StyleActionBtn(playBtn, spBtnPrimary, new Color(0.08f, 0.55f, 0.86f, 0.98f), fAgency, 52);
            }

            Transform modeBtn = mainMenu.Find("Btn_ModeSelect");
            if (modeBtn != null)
            {
                SetRect(modeBtn, new Vector2(480, 78), new Vector2(0, -125));
                StyleActionBtn(modeBtn, spBtnTech, new Color(0.09f, 0.36f, 0.56f, 0.94f), fBold, 26);
            }

            // Bottom Blue UI Element (Img_HintPill) containing the user-requested RED tagline
            Transform hintPill = EnsureChildImage(mainMenu, "Img_HintPill", spBanner, new Vector2(960, 68), new Vector2(0, -230), new Color(0.10f, 0.38f, 0.75f, 0.96f));
            var hpImg = hintPill.GetComponent<Image>();
            if (hpImg != null) { hpImg.type = Image.Type.Sliced; hpImg.preserveAspect = false; }

            // Red font line inside the blue UI element
            Transform redTagline = EnsureChildText(hintPill, "Txt_Tagline", fBold, 24, FontStyle.Bold, new Color(1f, 0.32f, 0.28f, 1f), new Vector2(920, 52), Vector2.zero);
            var tagText = redTagline.GetComponent<Text>();
            if (tagText != null)
            {
                tagText.alignment = TextAnchor.MiddleCenter;
                tagText.text = "★  CROSS THE VOID  •  CLAIM THE RED BEACON  ★";
            }

            // Hide old redundant keyboard hint overlapping the pill
            Transform hint = mainMenu.Find("Hint");
            if (hint != null)
            {
                hint.gameObject.SetActive(false);
            }
        }

        // 5. Polish LevelSelect (Fix card width & text overflow + HD card frame)
        Transform levelSelect = root.Find("LevelSelect");
        if (levelSelect != null)
        {
            Transform title = levelSelect.Find("Title");
            if (title != null)
            {
                SetRect(title, new Vector2(1200, 96), new Vector2(0, 382));
                StyleText(title, fBlack, 66, FontStyle.Normal, new Color(0.88f, 0.98f, 1f, 1f), true, true);
            }

            float[] xPositions = { -560f, -280f, 0f, 280f, 560f };
            for (int i = 1; i <= 5; i++)
            {
                Transform card = levelSelect.Find("Level" + i);
                if (card == null) continue;
                SetRect(card, new Vector2(252, 436), new Vector2(xPositions[i - 1], 42));
                StyleImage(card, spCardLevel, Image.Type.Sliced, new Color(0.10f, 0.42f, 0.78f, 0.96f));

                Transform word = card.Find("Word");
                if (word != null)
                {
                    SetRect(word, new Vector2(224, 40), new Vector2(0, 168));
                    StyleText(word, fBold, 24, FontStyle.Bold, new Color(0.85f, 0.95f, 1f, 1f), true, false);
                }

                Transform num = card.Find("Number");
                if (num != null)
                {
                    SetRect(num, new Vector2(224, 126), new Vector2(0, 44));
                    StyleText(num, fBlack, 116, FontStyle.Normal, Color.white, true, false);
                }

                Transform nm = card.Find("Name");
                if (nm != null)
                {
                    SetRect(nm, new Vector2(226, 56), new Vector2(0, -58));
                    StyleText(nm, fBold, 23, FontStyle.Bold, new Color(1f, 0.94f, 0.68f, 1f), true, false);
                    var nt = nm.GetComponent<Text>();
                    if (nt != null)
                    {
                        nt.resizeTextForBestFit = true;
                        nt.resizeTextMinSize = 15;
                        nt.resizeTextMaxSize = 24;
                    }
                }

                Transform status = card.Find("Status");
                if (status != null)
                {
                    SetRect(status, new Vector2(224, 84), new Vector2(0, -160));
                    StyleText(status, fBold, 22, FontStyle.Bold, Color.white, true, false);
                    var st = status.GetComponent<Text>();
                    if (st != null) st.lineSpacing = 1.15f;
                }
            }

            Transform hint = levelSelect.Find("Hint");
            if (hint != null)
            {
                SetRect(hint, new Vector2(1500, 48), new Vector2(0, -238));
                StyleText(hint, fMed, 24, FontStyle.Normal, new Color(0.76f, 0.87f, 0.98f, 0.95f), true, false);
                var ht = hint.GetComponent<Text>();
                if (ht != null) ht.text = "Complete each level to unlock the next.  Cleared levels can be replayed anytime.";
            }

            Transform backBtn = levelSelect.Find("Btn_Back");
            if (backBtn != null)
            {
                SetRect(backBtn, new Vector2(320, 84), new Vector2(-280, -356));
                StyleActionBtn(backBtn, spBtnTech, new Color(0.28f, 0.35f, 0.72f, 1f), fBold, 32);
            }

            Transform resetBtn = levelSelect.Find("Btn_ResetProgress");
            if (resetBtn != null)
            {
                SetRect(resetBtn, new Vector2(430, 84), new Vector2(280, -356));
                StyleActionBtn(resetBtn, spBtnPrimary, new Color(0.62f, 0.18f, 0.26f, 1f), fBold, 27);
            }
        }

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[UIPolishApplier] Applied uniform button interaction, red tagline in blue UI element, and HD UI polish to scene.");
    }

    // Title logo switcher helpers:
    [MenuItem("Tools/UI Polish/Title Style/Use Agency Bold Font")]
    public static void SetTitleAgencyBold() { SetTitleFont("Agency-Bold.ttf"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Bahnschrift Font")]
    public static void SetTitleBahnschrift() { SetTitleFont("Bahnschrift.ttf"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Roboto Black Font")]
    public static void SetTitleRobotoBlack() { SetTitleFont("Roboto-Black.ttf"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Cyber Cyan Logo")]
    public static void SetTitleLogoCyberCyan() { SetTitleGraphicLogo("Starbound_Logo_CyberCyan"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Chrome Crimson Logo")]
    public static void SetTitleLogoChromeCrimson() { SetTitleGraphicLogo("Starbound_Logo_ChromeCrimson"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Mecha Speed Logo")]
    public static void SetTitleLogoMechaSpeed() { SetTitleGraphicLogo("Starbound_Logo_MechaSpeed"); }

    [MenuItem("Tools/UI Polish/Title Style/Use Cosmic Minimal Logo")]
    public static void SetTitleLogoCosmicMinimal() { SetTitleGraphicLogo("Starbound_Logo_CosmicMinimal"); }

    static void SetTitleFont(string fontFile)
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return;
        Transform title = canvas.transform.Find("MainMenu/Title");
        if (title == null) return;

        Font f = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{fontFile}");
        if (f == null) return;

        Transform logoImg = title.Find("Img_TitleLogo");
        if (logoImg != null) logoImg.gameObject.SetActive(false);

        var txt = title.GetComponent<Text>();
        if (txt != null)
        {
            txt.enabled = true;
            txt.font = f;
            txt.fontSize = 98;
            txt.text = "STARBOUND : MAZE RUNNER";
        }
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[UIPolishApplier] Switched Title typography to: {fontFile}");
    }

    static void SetTitleGraphicLogo(string logoSpriteName)
    {
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return;
        Transform title = canvas.transform.Find("MainMenu/Title");
        if (title == null) return;

        Sprite spLogo = LoadSprite(logoSpriteName);
        if (spLogo == null)
        {
            Debug.LogError($"[UIPolishApplier] Logo sprite {logoSpriteName} not found.");
            return;
        }

        var txt = title.GetComponent<Text>();
        if (txt != null) txt.enabled = false;

        Transform logoImg = EnsureChildImage(title, "Img_TitleLogo", spLogo, new Vector2(860, 215), Vector2.zero, Color.white);
        logoImg.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[UIPolishApplier] Switched Title graphic logo to: {logoSpriteName}");
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{HdDir}/{name}.png");
    }

    static void StyleButton(Transform t, Sprite sp, Color col, float w, float h, Vector2 pos)
    {
        if (t == null) return;
        StyleImage(t, sp, Image.Type.Sliced, col);
        var btn = t.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            var fb = t.GetComponent<UIButtonFeedback>();
            if (fb == null) fb = Undo.AddComponent<UIButtonFeedback>(t.gameObject);
            fb.SetOriginalColor(col);
        }
        var rt = t as RectTransform;
        if (rt != null)
        {
            Undo.RecordObject(rt, "Resize Button");
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = pos;
        }
    }

    static void StyleActionBtn(Transform t, Sprite sp, Color col, Font f, int fontSize)
    {
        if (t == null) return;
        StyleImage(t, sp, Image.Type.Sliced, col);
        var btn = t.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            var fb = t.GetComponent<UIButtonFeedback>();
            if (fb == null) fb = Undo.AddComponent<UIButtonFeedback>(t.gameObject);
            fb.SetOriginalColor(col);
        }
        StyleText(t.Find("Label"), f, fontSize, f != null && f.name.Contains("Black") ? FontStyle.Normal : FontStyle.Bold, Color.white, true, false);
    }

    static void StyleImage(Transform t, Sprite sp, Image.Type type, Color col)
    {
        if (t == null) return;
        var img = t.GetComponent<Image>();
        if (img == null) return;
        Undo.RecordObject(img, "Style Image");
        if (sp != null) img.sprite = sp;
        img.type = type;
        img.color = col;
        EditorUtility.SetDirty(img);
    }

    static void StyleText(Transform t, Font font, int size, FontStyle style, Color col, bool cleanShadow, bool keepOutline)
    {
        if (t == null) return;
        var txt = t.GetComponent<Text>();
        if (txt == null) return;
        Undo.RecordObject(txt, "Style Text");
        if (font != null) txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = col;
        txt.supportRichText = true;
        EditorUtility.SetDirty(txt);

        var ol = t.GetComponent<Outline>();
        if (ol != null && !keepOutline)
        {
            Undo.RecordObject(ol, "Disable Heavy Outline");
            ol.enabled = false;
        }
        else if (ol != null && keepOutline)
        {
            Undo.RecordObject(ol, "Refine Outline");
            ol.enabled = true;
            ol.effectColor = new Color(0f, 0.04f, 0.12f, 0.78f);
            ol.effectDistance = new Vector2(1.4f, -1.4f);
        }

        if (cleanShadow)
        {
            var sh = t.GetComponent<Shadow>();
            if (sh == null) sh = Undo.AddComponent<Shadow>(t.gameObject);
            Undo.RecordObject(sh, "Refine Shadow");
            sh.enabled = true;
            sh.effectColor = new Color(0f, 0.02f, 0.08f, 0.78f);
            sh.effectDistance = new Vector2(1.8f, -1.8f);
        }
    }

    static void StyleSlider(Transform sliderRoot, Sprite track, Sprite fill, Sprite thumb)
    {
        if (sliderRoot == null) return;
        StyleImage(sliderRoot.Find("Background"), track, Image.Type.Sliced, new Color(0.10f, 0.18f, 0.34f, 1f));
        StyleImage(sliderRoot.Find("Fill Area/Fill"), fill, Image.Type.Sliced, new Color(0.16f, 0.86f, 0.96f, 1f));
        StyleImage(sliderRoot.Find("Handle Slide Area/Handle"), thumb, Image.Type.Simple, Color.white);
    }

    static void SetRectSize(Transform t, float w, float h)
    {
        var rt = t as RectTransform;
        if (rt == null) return;
        Undo.RecordObject(rt, "Set Rect Size");
        rt.sizeDelta = new Vector2(w, h);
    }

    static void SetRect(Transform t, Vector2 size, Vector2 pos)
    {
        var rt = t as RectTransform;
        if (rt == null) return;
        Undo.RecordObject(rt, "Set Rect");
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static Transform EnsureChildImage(Transform parent, string name, Sprite sp, Vector2 size, Vector2 pos, Color col)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create UI Child Image");
            go.transform.SetParent(parent, false);
        }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.sprite = sp;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = col;
        img.raycastTarget = false;
        EditorUtility.SetDirty(go);
        return go.transform;
    }

    static Transform EnsureChildText(Transform parent, string name, Font font, int size, FontStyle style, Color col, Vector2 sizeDelta, Vector2 pos)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(go, "Create UI Child Text");
            go.transform.SetParent(parent, false);
        }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;

        var txt = go.GetComponent<Text>();
        if (font != null) txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = col;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.supportRichText = true;

        var sh = go.GetComponent<Shadow>();
        if (sh == null) sh = Undo.AddComponent<Shadow>(go);
        sh.effectColor = new Color(0f, 0.02f, 0.08f, 0.90f);
        sh.effectDistance = new Vector2(1.8f, -1.8f);

        EditorUtility.SetDirty(go);
        return go.transform;
    }
}
