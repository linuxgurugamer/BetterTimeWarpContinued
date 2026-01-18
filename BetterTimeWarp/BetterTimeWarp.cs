using ClickThroughFix;
using CommNet;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ToolbarControl_NS;
using UnityEngine;

namespace BetterTimeWarp
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class BetterTimeWarp : MonoBehaviour
    {

        public static BetterTimeWarp Instance;

        public static TimeWarpRates StandardWarp = new TimeWarpRates("Standard Warp", new float[] { 1f, 5f, 10f, 50f, 100f, 1000f, 10000f, 100000f }, false, true, 4);
        public static TimeWarpRates StandardPhysWarp = new TimeWarpRates("Standard Physics Warp", new float[] { 1f, 2f, 3f, 4f }, true, false, 4);
        public static ConfigNode SettingsNode;

        public List<TimeWarpRates> customWarps = new List<TimeWarpRates>();
        public static bool ShowUI = true;

        public bool UIOpen
        {
            get
            {
                return windowOpen;
            }
            set
            {
                windowOpen = value;
            }
        }

        static ToolbarControl toolbarControl;

        private static bool hasAdded = false;

        static Texture2D upArrow;
        static Texture2D downArrow;

        public void Start()
        {
            DontDestroyOnLoad(this);
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.FLIGHT &&
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                return;
            Instance = this;
            this.skin = HighLogic.Skin;

            LoadCustomWarpRates();

            SetWarpRates(CurrentWarp, false);
            SetWarpRates(CurrentPhysWarp, false);
            InitW();
            windowStyle = new GUIStyle(skin.window);
            windowStyle.padding.left = 5;
            windowStyle.padding.right = 5;
            windowStyle.padding.top = 5;
            windowStyle.padding.bottom = 5;
            smallButtonStyle = new GUIStyle(skin.button);
            smallButtonStyle.stretchHeight = false;
            smallButtonStyle.fixedHeight = 20f;

            smallScrollBar = new GUIStyle(skin.verticalScrollbar);
            smallScrollBar.fixedWidth = 8f;
            hSmallScrollBar = new GUIStyle(skin.horizontalScrollbar);
            hSmallScrollBar.fixedHeight = 0f;

            upArrow = GameDatabase.Instance.GetTexture("BetterTimeWarp/Icons/up", false);
            downArrow = GameDatabase.Instance.GetTexture("BetterTimeWarp/Icons/down", false);

            upContent = new GUIContent("", upArrow, "");
            downContent = new GUIContent("", downArrow, "");
            buttonContent = downContent;
            CreateRectangles();
            GameEvents.onGameStateSave.Add(SaveSettingsAndCrap);
            GameEvents.onTimeWarpRateChanged.Add(onTimeWarpRateChanged);
            GameEvents.onPartUnpack.Add(onPartUnpack);
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);


            //add the toolbar button to non-flight scenes
            if (toolbarControl == null && !hasAdded && HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().enabled && !HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().hideButton)
            {
                if (!HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().hideButtonInFlight || HighLogic.LoadedScene != GameScenes.FLIGHT)
                {

                    toolbarControl = gameObject.AddComponent<ToolbarControl>();
                    toolbarControl.AddToAllToolbars(OnTrue, OnFalse,
                        ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION |
                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                        MODID,
                        "betterTimeWarpButton",
                        "BetterTimeWarp/Icons/application_38",
                        "BetterTimeWarp/Icons/application_24",
                        MODNAME
                    );

                    hasAdded = true;
                }
            }
        }

        internal const string MODID = "BetterTimeWarp_NS";
        internal const string MODNAME = "Better Time Warp";

        void OnTrue()
        {
            UIOpen = true;
        }
        void OnFalse()
        {
            UIOpen = false;
        }
        void onLevelWasLoadedGUIReady(GameScenes scene)
        {
            CreateRectangles();
            //currWarpIndex = 0;
            //currPhysIndex = 0;
            //SetWarpRates(warpRates[currWarpIndex]);
            //if (warpRates[currWarpIndex] != CurrentWarp)
            {
                SetWarpRates(warpRates[currWarpIndex]);
            }
            // if (physRates[currPhysIndex] != CurrentPhysWarp)
            {
                SetWarpRates(physRates[currPhysIndex]);
            }

        }

        void OnDestroy()
        {
            GameEvents.onGameStateSave.Remove(SaveSettingsAndCrap);
            GameEvents.onTimeWarpRateChanged.Remove(onTimeWarpRateChanged);
            GameEvents.onPartUnpack.Remove(onPartUnpack);
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelWasLoadedGUIReady);
            removeLauncherButtons();
        }

        void SaveSettingsAndCrap(ConfigNode node) //lel
        {
            SaveCustomWarpRates();
        }

        public void removeLauncherButtons()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarControl = null;
            }
        }

        void InitW()
        {
            w[1] = "10";
            w[2] = "100";
            w[3] = "1000";
            w[4] = "10000";
            w[5] = "100000";
            w[6] = "1000000";
            w[7] = "10000000";
        }
        List<Part> resourceConverterParts = new List<Part>();

        // When coming out of warp, fix issue with ResourceConverter
        void onPartUnpack(Part p)
        {
            Log.Info("Unpacking part: " + p.partInfo.name);
            if (resourceConverterParts.Contains(p))
            {
                resourceConverterParts.Remove(p);
                foreach (PartModule tmpPM in p.Modules)
                {

                    // Find all modules of type BaseConvertor
                    // If !IsActivated, then 
                    // set lastUpdateTime = Planetarium.GetUniversalTime();
                    // lastUpdateTime is a protected field, so Reflection was needed to fix this

                    switch (tmpPM.moduleName)
                    {
                        case "FissionReactor":
                        case "KFAPUController":
                        case "ModuleResourceConverter":
                            ModuleResourceConverter tmpGen = (ModuleResourceConverter)tmpPM;
                            Log.Info("Module: " + tmpGen.moduleName + " IsActivated: " + tmpGen.IsActivated.ToString());
                            // if (!tmpGen.IsActivated)
                            {
                                FieldInfo fi = tmpGen.GetType().GetField("lastUpdateTime", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (fi != null)
                                {
                                    Log.Info("Updating lastUpdateTime");
                                    fi.SetValue(tmpGen, Planetarium.GetUniversalTime());
                                }
                                else
                                    Log.Info("Unable to get pointer to lastUpdateTime");
                            }

                            break;

                    }
                }
            }
        }


        static int lastWarpRateIdx = 0;
        void onTimeWarpRateChanged()
        {
            if (TimeWarp.fetch != null)
            {
                if (lastWarpRateIdx > 0 && TimeWarp.fetch.warpRates[TimeWarp.fetch.current_rate_index] > 1)
                {
                    foreach (var v in FlightGlobals.fetch.vesselsLoaded)
                    {
                        foreach (var p in v.Parts)
                        {
                            foreach (PartModule tmpPM in p.Modules)
                            {

                                // Find all modules of type BaseConvertor which are inactive

                                switch (tmpPM.moduleName)
                                {
                                    case "FissionReactor":
                                    case "KFAPUController":
                                    case "ModuleResourceConverter":
                                        ModuleResourceConverter tmpGen = (ModuleResourceConverter)tmpPM;
                                        Log.Info("Module: " + tmpGen.moduleName + " IsActivated: " + tmpGen.IsActivated.ToString());
                                        if (!tmpGen.IsActivated)
                                        {
                                            if (!resourceConverterParts.Contains(p))
                                                resourceConverterParts.Add(p);
                                        }
                                        else
                                        {
                                            if (resourceConverterParts.Contains(p))
                                            {
                                                resourceConverterParts.Remove(p);
                                                FieldInfo fi = tmpGen.GetType().GetField("lastUpdateTime", BindingFlags.NonPublic | BindingFlags.Instance);
                                                if (fi != null)
                                                {
                                                    Log.Info("Updating lastUpdateTime");
                                                    fi.SetValue(tmpGen, Planetarium.GetUniversalTime());
                                                }
                                                else
                                                    Log.Info("Unable to get pointer to lastUpdateTime");
                                            }
                                        }
                                        break;

                                }
                            }
                        }
                    }
                }
                //CheatOptions.InfiniteElectricity = false;CheatOptions.InfiniteElectricity = false;
                lastWarpRateIdx = TimeWarp.fetch.current_rate_index;
                if (currentRates.DisableECAtHighWarp )
                {
                    if (TimeWarp.fetch.current_rate_index > currentRates.ecIndexDisable)
                    {
                        if (!CheatOptions.InfiniteElectricity)
                        {
                            CheatOptions.InfiniteElectricity = true;
                            autoDisable = true;
                            ScreenMessages.PostScreenMessage("Disabling EC Usage", 5);
                        }
                    }
                    else
                    {
                        if (autoDisable)
                        {
                            CheatOptions.InfiniteElectricity = false;
                            autoDisable = false;
                            ScreenMessages.PostScreenMessage("Enabling EC Usage", 5);
                        }
                    }
                }
            }
        }

        const float quickWidth = 200f;
        const float quickHeight = 410f;

        const float advWidth = 420f;
        const float advHeight = quickHeight;

        const float physWidth = 420f;
        const float physHeight = 220f;

        void CreateRectangles()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                float x = 210f;
                quikWindowRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, 35f, quickWidth, quickHeight);
                advWindowRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, 35f, advWidth, advHeight);
                physSettingsRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, 445f, physWidth, physHeight);
            }
            else
            {
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    float x = 140;
                    float y = 58f;

                    quikWindowRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, y, quickWidth, quickHeight);
                    advWindowRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, y, advWidth, advHeight);
                    physSettingsRect = new Rect(GameSettings.UI_SCALE * x /* * ScreenSafeUI.PixelRatio */, y + 410f, physWidth, physHeight);

                }
                else
                {
                    quikWindowRect = new Rect((Screen.width - 205f), 40f, quickWidth, quickHeight);
                    advWindowRect = new Rect((Screen.width - 425f), 40f, advWidth, advHeight);
                    physSettingsRect = new Rect((Screen.width - 425f), 450f, physWidth, physHeight);
                }
            }
        }

        //physics settings
        public bool ScaleCameraSpeed = true;
        public bool UseLosslessPhysics = false;
        public float LosslessUpperThreshold = 2f;

        GUISkin skin;

        Rect advWindowRect = new Rect();
        Rect physSettingsRect = new Rect();
        GUIContent buttonContent;
        GUIContent upContent;
        GUIContent downContent;

        bool advWindowOpen = false;
        bool windowOpen = false;
        Rect quikWindowRect = new Rect();
        GUIStyle windowStyle;
        GUIStyle smallButtonStyle;
        GUIStyle smallScrollBar;
        GUIStyle hSmallScrollBar;

        public void OnGUI()
        {
            if ((HighLogic.LoadedScene != GameScenes.SPACECENTER &&
                HighLogic.LoadedScene != GameScenes.FLIGHT &&
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                || !HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().enabled)
                return;

            if (ShowUI)
            {
                GUI.skin = skin;

                //flight
                if ((HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    && !HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().hideDropdownButtonInFlight)
                {
                    const float ICON_WIDTH = 28;
                    const float ICON_BASE = 203f + 80; // 32f added for KSP 1.12

                    float y = ICON_BASE;
                    float x = 0;
                    var tu = TelemetryUpdate.Instance;

                    if (tu != null && CommNetScenario.CommNetEnabled)
                    {
                        if (tu.arrow_icon != null && tu.arrow_icon.sprite != tu.BLK && tu.arrow_icon.gameObject.activeSelf) y += ICON_WIDTH;

                        if (FlightGlobals.ActiveVessel.connection != null && FlightGlobals.ActiveVessel.connection.ControlPath.First != null && FlightGlobals.ActiveVessel.connection.ControlPath.Last != null && FlightGlobals.ActiveVessel.connection.ControlPath.Last.hopType != FlightGlobals.ActiveVessel.connection.ControlPath.First.hopType)
                        {
                            if (tu.firstHop_icon != null && tu.firstHop_icon.sprite != tu.BLK && tu.firstHop_icon.gameObject.activeSelf) y += ICON_WIDTH;
                        }
                        if (tu.lastHop_icon != null && tu.lastHop_icon.sprite != tu.BLK && tu.lastHop_icon.gameObject.activeSelf) y += ICON_WIDTH;
                        if (tu.control_icon != null && tu.control_icon.sprite != tu.BLK && tu.control_icon.gameObject.activeSelf) y += ICON_WIDTH;
                        if (tu.signal_icon != null && tu.signal_icon.sprite != tu.BLK && tu.signal_icon.gameObject.activeSelf) y += ICON_WIDTH;
                        if (tu.modeButton != null && tu.modeButton.gameObject.activeSelf) y += ICON_WIDTH;
                    }
                    // if only a single icon, then adjustment needed
                    if (y == ICON_BASE + ICON_WIDTH)
                        y += 12;
                    float widthAndHeight = 20f;
                    float scale = 1f;
                    if (GameSettings.UI_SCALE != 1f || GameSettings.UI_SCALE_TIME != 1f)
                    {
                        scale = GameSettings.UI_SCALE * GameSettings.UI_SCALE_TIME;
                        widthAndHeight *= scale;
                    }
                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    {
                        x = 24f;
                        y += 56f;
                    }
                    if (buttonContent != null)
                    {
                        var b = GUI.Toggle(new Rect(scale * y, x, widthAndHeight, widthAndHeight), windowOpen, buttonContent, skin.button);
                        if (b != windowOpen)
                        {
                            windowOpen = b;
                        }
                    }
                }

                if (windowOpen)
                {
                    if (!advWindowOpen)
                        quikWindowRect = ClickThruBlocker.GUILayoutWindow(60371, quikWindowRect, QuikWarpWindow, "", windowStyle);
                    else
                    {
                        advWindowRect = ClickThruBlocker.GUILayoutWindow(60372, advWindowRect, TimeWarpWindow, "", windowStyle);
                        if (showPhysicsSettings)
                        {
                            physSettingsRect = ClickThruBlocker.GUILayoutWindow(60373, physSettingsRect, PhysicsSettingsWindow, "", windowStyle);
                        }
                    }

                    buttonContent = upContent;
                }
                else
                    buttonContent = downContent;
            }
        }

        bool editToggle = false;
        Vector2 scrollPos = new Vector2(0f, 0f);
        Vector2 scrollPos2 = new Vector2(0f, 0f);

        string warpName = "Name";
        bool physics = false;
        bool disableECAtHighWarp = false;
        int ecIndexDisable = 7;
        internal bool autoDisable = false;

        TimeWarpRates currentRates = StandardWarp;
        int selected = 0;

        TimeWarpRates CurrentWarp;
        TimeWarpRates CurrentPhysWarp;
        int currWarpIndex = 0;
        int currPhysIndex = 0;

        List<string> warpNames = new List<string>();
        TimeWarpRates[] warpRates = new TimeWarpRates[] { };
        List<string> physNames = new List<string>();
        TimeWarpRates[] physRates = new TimeWarpRates[] { };

        void Update()
        {
            warpNames.Clear();
            physNames.Clear();
            warpRates = customWarps.Where(r => !r.Physics).ToArray();
            physRates = customWarps.Where(r => r.Physics).ToArray();

            foreach (var rates in customWarps)
            {
                string sB = "";
                string sA = "";
                if (rates == CurrentWarp || rates == CurrentPhysWarp)
                {
                    sB = "<color=lime>";
                    sA = "</color>";
                }

                if (rates.Physics)
                    physNames.Add(sB + rates.Name + sA);
                else
                    warpNames.Add(sB + rates.Name + sA);
            }

            names = warpNames.Concat(physNames).ToArray();

            //make camera speed not change with time warp
            if (ScaleCameraSpeed && Time.timeScale < 1f)
                FlightCamera.fetch.SetDistanceImmediate(FlightCamera.fetch.Distance);

            var mouse = Mouse.screenPos;
            if ((windowOpen && !advWindowOpen && quikWindowRect.Contains(mouse)) ||
                (windowOpen && advWindowOpen && advWindowRect.Contains(mouse)) ||
                (windowOpen && advWindowOpen && showPhysicsSettings && physSettingsRect.Contains(mouse))
            )
            {
                InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS | ControlTypes.ALL_SHIP_CONTROLS, "BetterTimeWarp_UIHover_Lock");
            }
            else
                InputLockManager.RemoveControlLock("BetterTimeWarp_UIHover_Lock");
        }

        bool lockWindow()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().lockWindowPosInFlight)
                return true;
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams>().lockWindowPos)
                return true;
            return false;
        }

        public void QuikWarpWindow(int id)
        {
            GUILayout.BeginVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, hSmallScrollBar, smallScrollBar);

            currWarpIndex = GUILayout.SelectionGrid(currWarpIndex, warpNames.ToArray(), 1, smallButtonStyle);
            GUILayout.Space(20f);
            currPhysIndex = GUILayout.SelectionGrid(currPhysIndex, physNames.ToArray(), 1, smallButtonStyle);

            if (warpRates[currWarpIndex] != CurrentWarp)
            {
                SetWarpRates(warpRates[currWarpIndex]);
            }
            if (physRates[currPhysIndex] != CurrentPhysWarp)
            {
                SetWarpRates(physRates[currPhysIndex]);
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Advanced"))
            {
                advWindowOpen = true;
            }

            GUILayout.EndVertical();
            if (!lockWindow())
                GUI.DragWindow();
        }

        bool showPhysicsSettings = false;
        string labelColor = "#b7fe00";

        string[] w = new string[8];

        string[] names = new string[] { };

        public void TimeWarpWindow(int id)
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    scrollPos = GUILayout.BeginScrollView(scrollPos);

                    editToggle = GUILayout.Toggle(editToggle, "Create", skin.button);
                    selected = GUILayout.SelectionGrid(selected, names, 1, smallButtonStyle);

                    currentRates = customWarps.Find(r => r.Name == names[selected] ||
                        (names[selected].Contains("<") && names[selected].Split('<', '>')[2] == r.Name)
                        );

                    if (currentRates == null)
                        currentRates = StandardWarp;

                    GUILayout.EndScrollView();
                }

                using (new GUILayout.VerticalScope())
                {
                    scrollPos2 = GUILayout.BeginScrollView(scrollPos2);

                    if (editToggle)
                    {
                        bool canExport = true;

                        using (new GUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                        {
                            warpName = GUILayout.TextField(warpName);

                            for (int i = 1; i < 8; i++)
                            {
                                if (i < 4 || !physics)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label($"{i}. ", GUILayout.Width(20));
                                        w[i] = GUILayout.TextField(w[i]);
                                    }
                                }
                            }
                            physics = GUILayout.Toggle(physics, "Physics Warp?");
                            if (!physics)
                            {
                                disableECAtHighWarp = GUILayout.Toggle(disableECAtHighWarp, new GUIContent("Disable EC usage", "Disable EC usage at high warp rates"));
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label("Disable Index: ");
                                    if (GUILayout.Button("<") && ecIndexDisable > 1)
                                        ecIndexDisable--;
                                    GUILayout.Label(ecIndexDisable.ToString());
                                    if (GUILayout.Button(">") && ecIndexDisable < 7)
                                        ecIndexDisable++;
                                }
                            }
                        }
                        if (GUILayout.Button("Save"))
                        {
                            float[] rates;
                            if (physics)
                                rates = new float[4];
                            else
                                rates = new float[8];

                            rates[0] = 1f;

                            float pw;
                            for (int i = 1; i < 8; i++)
                            {
                                if (i < 4 || !physics)
                                {
                                    if (float.TryParse(w[i], out pw))
                                        rates[i] = pw;
                                    else
                                        canExport = false;
                                }
                            }

                            if (canExport)
                            {
                                TimeWarpRates timeWarpRates = new TimeWarpRates(warpName, rates, physics, disableECAtHighWarp, ecIndexDisable);
                                customWarps.Add(timeWarpRates);
                                SaveCustomWarpRates();
                                editToggle = false;
                                //SetWarpRates (timeWarpRates);
                                warpName = "Name";
                                physics = false;
                                InitW();
                            }
                            else
                            {
                                //PopupDialog.SpawnPopupDialog (new MultiOptionDialog("Cannot save because there are non-numbers in the editing fields", "Error", null), false, null);
                                var dialog = new MultiOptionDialog("btw1", "Cannot save because there are non-numbers in the editing fields", "Error", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => { })
                                                });
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                            }
                            //save the settings, so if they have been regenerated, it exsists and wont cause errors
                            BetterTimeWarp.SettingsNode.Save(BetterTimeWarpInitializer.BTW_CFG_FILE);
                        }
                        if (GUILayout.Button("Cancel", smallButtonStyle))
                        {
                            editToggle = false;
                            warpName = "Name";
                            physics = false;
                            InitW();
                        }
                    }
                    else
                    {
                        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                        for (int i = 1; i <= 8; i++)
                        {
                            if (i <= 4 || !currentRates.Physics)
                            {
                                var f = currentRates.Rates[i - 1];
                                GUILayout.Label("<b><color=lime>" + i.ToString() + ":</color></b> <color=white>" + f.ToString(TimeWarpRates.rateFmt(f)) + "x</color>");
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.Space(15f);
                        if (GUILayout.Button("Select"))
                        {
                            SetWarpRates(currentRates);
                        }
                        if (currentRates != StandardWarp && currentRates != StandardPhysWarp && GUILayout.Button("Edit", smallButtonStyle))
                        {
                            if (currentRates != StandardWarp && currentRates != StandardPhysWarp)
                            {
                                if (!currentRates.Physics)
                                    SetWarpRates(StandardWarp, false);
                                else
                                    SetWarpRates(StandardPhysWarp, false);
                                customWarps.Remove(currentRates);
                                editToggle = true;
                                warpName = currentRates.Name;
                                physics = currentRates.Physics;
                                disableECAtHighWarp = currentRates.DisableECAtHighWarp;
                                ecIndexDisable = currentRates.ecIndexDisable;

                                for (int i = 1; i < 8; i++)
                                {
                                    if (i < 4 || !physics)
                                        w[i] = currentRates.Rates[i].ToString(TimeWarpRates.rateFmt(currentRates.Rates[i]));
                                }
                                selected = 0;
                            }
                            else
                            {
                                //PopupDialog.SpawnPopupDialog (new MultiOptionDialog("Cannot edit standard warp rates", "Better Time Warp", null), true, null);

                                var dialog = new MultiOptionDialog("btw2", "Cannot edit standard warp rates", "Better Time Warp", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => { })
                                                });
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);

                            }
                        }
                        if (currentRates != StandardWarp && currentRates != StandardPhysWarp && GUILayout.Button("Delete", smallButtonStyle))
                        {
                            if (currentRates != StandardWarp && currentRates != StandardPhysWarp)
                            {
                                customWarps.Remove(currentRates);
                                selected = 0;
                                SaveCustomWarpRates();
                                //	PopupDialog.SpawnPopupDialog (new MultiOptionDialog("Deleted " + currentRates.Name + " time warp rates", "Better Time Warp", null), true, null);



                                var dialog = new MultiOptionDialog("btw3", "Deleted " + currentRates.Name + " time warp rates", "Better Time Warp", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => { 
                                                           // winState = winContent.close;
                                                        })
                                                });
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                                // winState = winContent.dialog;




                                SetWarpRates(StandardWarp, false);
                            }
                            else
                            {
                                //PopupDialog.SpawnPopupDialog (new MultiOptionDialog("Cannot delete standard warp rates", "Better Time Warp", null), true, null);

                                var dialog = new MultiOptionDialog("btw4", "Cannot delete standard warp rates", "Better Time Warp", HighLogic.UISkin, new DialogGUIBase[] {
                                                                                     new DialogGUIButton ("OK", () => { })
                                                });
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);

                            }
                        }
                    }
                    GUILayout.EndScrollView();
                }

            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Simple", GUILayout.ExpandWidth(true)))
                {
                    advWindowOpen = false;
                }
                showPhysicsSettings = GUILayout.Toggle(showPhysicsSettings, "<color=lime>Physics Settings</color>", skin.button, GUILayout.ExpandWidth(true));
            }
            if (!lockWindow())
                GUI.DragWindow();
        }

        Vector2 physSettingsScroll = new Vector2(0f, 0f);

        string[] toolbar = new string[] { "<color=red>1</color>", "<color=orange>1/2</color>", "<color=yellow>1/3</color>", "<color=lime>1/4</color>" };
        public void PhysicsSettingsWindow(int id)
        {
            physSettingsScroll = GUILayout.BeginScrollView(physSettingsScroll);

            ScaleCameraSpeed = GUILayout.Toggle(ScaleCameraSpeed, "<b><color=" + labelColor + ">Scale Camera Speed</color></b>");
            GUILayout.Label("<color=white>Removes the time based smoothing of the camera so that it doesn't lag at really low warp</color>");
            GUILayout.Space(5f);

            UseLosslessPhysics = GUILayout.Toggle(UseLosslessPhysics, "<b><color=" + labelColor + ">Use Lossless Physics</color></b> <color=red><i>Experimental!</i></color>");
            GUILayout.Label("<color=white>Increases the physics simulation rate so that you can maintain accurate physics at high warp</color>");
            GUILayout.Label("<color=#bbb><b>Note:</b> Lossless Physics causes extreme lag above 50-100x time warp, depending on your computer. <i>You have been warned!</i></color>");
            GUILayout.Space(5f);
            GUILayout.Label("<b>Lossless Physics Accuracy:</b>");
            GUILayout.Label("<color=white>1/2 is recommended, but if you have a weaker computer then 1/3 or 1/4 will be easier on your CPU.</color>");
            GUILayout.BeginHorizontal(skin.box);
            LosslessUpperThreshold = (float)(GUILayout.Toolbar((int)(LosslessUpperThreshold - 1f), toolbar, smallButtonStyle, GUILayout.ExpandWidth(true)) + 1);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            GUILayout.Label("<b>Physics Timestep:</b> <color=white>" + Time.fixedDeltaTime + "s</color>");
            GUILayout.Label("<b>Physics Timescale:</b> <color=white>" + Time.timeScale + "x</color>");
            GUILayout.Label("<b>Lossless Upper Threshold:</b> <color=white>" + LosslessUpperThreshold.ToString("N3") + "x</color>");

            GUILayout.EndScrollView();
            if (!lockWindow())
                GUI.DragWindow();
        }

        void FixedUpdate()
        {
            if (TimeWarp.fetch != null && TimeWarp.fetch.Mode == TimeWarp.Modes.HIGH && UseLosslessPhysics && Time.timeScale < 100f)
            {
                if (Time.timeScale == 1f)
                {
                    Time.fixedDeltaTime = 0.02f;

                    //ScreenMessages.PostScreenMessage("1 Setting Time.fixedDeltaTime to " + Time.fixedDeltaTime.ToString("N4"), 5);
                    Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
                    Time.maximumDeltaTime = GameSettings.PHYSICS_FRAME_DT_LIMIT;

                }
                else
                {
                    if (Time.timeScale >= LosslessUpperThreshold)
                    {
                        Time.fixedDeltaTime = LosslessUpperThreshold * 0.02f;
                        //ScreenMessages.PostScreenMessage("2 Setting Time.fixedDeltaTime to " + Time.fixedDeltaTime.ToString("N4"), 5);
                        Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
                        Time.maximumDeltaTime = Time.fixedDeltaTime;
                    }
                    else
                    {
                        Time.fixedDeltaTime = Time.timeScale * 0.02f;
                        //ScreenMessages.PostScreenMessage("3 Setting Time.fixedDeltaTime to " + Time.fixedDeltaTime.ToString("N4"), 5);

                        Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
                        Time.maximumDeltaTime = Time.fixedDeltaTime;
                    }
                }

                float v = Mathf.Clamp((Mathf.Round(Time.fixedDeltaTime * 100f) / 100f), 0.02f, 0.35f);
                if (v != GameSettings.PHYSICS_FRAME_DT_LIMIT)
                {
                    GameSettings.PHYSICS_FRAME_DT_LIMIT = v;
                    GameSettings.SaveSettings();
                }
            }
        }

        public void SetWarpRates(TimeWarpRates rates, bool message = true)
        {
            if (TimeWarp.fetch != null)
            {
                if (TimeWarp.fetch.warpRates.Length == rates.Rates.Length && !rates.Physics)
                {
                    TimeWarp.fetch.warpRates = rates.Rates;
                    CurrentWarp = rates;

                    for (var i = 0; i < warpRates.Length; i++)
                    {
                        var r = warpRates[i];
                        if (r == rates)
                        {
                            currWarpIndex = i;
                        }
                    }

                    Log.Info("Set time warp rates to " + rates.ToString());
                    if (message)
                        ScreenMessages.PostScreenMessage(new ScreenMessage("New time warp rates: " + rates.Name, 3f, ScreenMessageStyle.UPPER_CENTER));
                    return;
                }
                else if (TimeWarp.fetch.physicsWarpRates.Length == rates.Rates.Length && rates.Physics)
                {
                    TimeWarp.fetch.physicsWarpRates = rates.Rates;
                    CurrentPhysWarp = rates;

                    for (var i = 0; i < physRates.Length; i++)
                    {
                        var r = physRates[i];
                        if (r == rates)
                        {
                            currPhysIndex = i;
                        }
                    }

                    Log.Info("Set time warp rates to " + rates.ToString());
                    if (message)
                        ScreenMessages.PostScreenMessage(new ScreenMessage("New physic warp rates: " + rates.Name, 3f, ScreenMessageStyle.UPPER_CENTER));
                    return;
                }
                return;
            }
            Log.Warning("Failed to set warp rates");

        }

        private void LoadCustomWarpRates()
        {
            Log.Info("LoadCustomWarpRates A1");
            if (BetterTimeWarp.SettingsNode == null)
                BetterTimeWarp.SettingsNode = new ConfigNode();
            if (!SettingsNode.HasNode("BetterTimeWarp"))
                SettingsNode.AddNode("BetterTimeWarp");
            var node = SettingsNode.GetNode("BetterTimeWarp");

            if (!SettingsNode.HasNode("BetterTimeWarp"))
                SettingsNode.AddNode("BetterTimeWarp");

            if (node.HasValue("ScaleCameraSpeed"))
                ScaleCameraSpeed = bool.Parse(node.GetValue("ScaleCameraSpeed"));
            if (node.HasValue("UseLosslessPhysics"))
                UseLosslessPhysics = bool.Parse(node.GetValue("UseLosslessPhysics"));
            if (node.HasValue("LosslessUpperThreshold"))
                LosslessUpperThreshold = float.Parse(node.GetValue("LosslessUpperThreshold"));

            customWarps.Clear();
            customWarps.Add(StandardWarp);
            customWarps.Add(StandardPhysWarp);

            foreach (ConfigNode cNode in node.GetNodes("CustomWarpRate"))
            {
                string name = cNode.GetValue("name");
                bool physics = bool.Parse(cNode.GetValue("physics"));
                bool disable = false;
                if (node.HasValue("DisableECAtHighWarp"))
                {

                    string str = cNode.GetValue("DisableECAtHighWarp");
                    try
                    {
                        disable = bool.Parse(str);
                    }
                    catch (Exception e)
                    {
                        Log.Error("DisableECAtHighWarp: " + str);
                    }
                }
                else
                    disable = false;
                if (node.HasValue("ecIndexDisable"))
                {
                    string str = cNode.GetValue("ecIndexDisable");
                    Log.Info("ecIndexDisable: " + str);
                    try
                    {
                        ecIndexDisable = int.Parse(str);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("ecIndexDisable value: " + str);
                    }

                }
                float[] rates;
                if (physics)
                    rates = new float[4];
                else
                    rates = new float[8];
                rates[0] = 1f;
                for (int i = 1; i < 8; i++)
                {
                    if (i < 4 || !physics)
                    {
                        string str = cNode.GetValue("warpRate" + i.ToString());
                        rates[i] = float.Parse(cNode.GetValue("warpRate" + i.ToString()));
                    }
                }
                customWarps.Add(new TimeWarpRates(name, rates, physics, disable, 4));
            }

            //populate the seperate arrays
            warpRates = customWarps.Where(r => !r.Physics).ToArray();
            physRates = customWarps.Where(r => r.Physics).ToArray();

            //load selected rates
            string currentTimeWarp = StandardWarp.Name;
            string currentPhysWarp = StandardPhysWarp.Name;

            if (node.HasValue("CurrentTimeWarp"))
                currentTimeWarp = node.GetValue("CurrentTimeWarp");
            if (node.HasValue("CurrentPhysWarp"))
                currentPhysWarp = node.GetValue("CurrentPhysWarp");

            if (warpRates.Where(w => w.Name == currentTimeWarp).Count() > 0)
                CurrentWarp = warpRates.Where(w => w.Name == currentTimeWarp).First();
            if (physRates.Where(w => w.Name == currentPhysWarp).Count() > 0)
                CurrentPhysWarp = physRates.Where(w => w.Name == currentPhysWarp).First();

            if (CurrentWarp == null)
            {
                CurrentWarp = StandardWarp;
                Log.Info("CurrentWarp set to StandardWarp  2");
            }
            if (CurrentPhysWarp == null)
                CurrentPhysWarp = StandardPhysWarp;
        }

        private void SaveCustomWarpRates()
        {
            if (!SettingsNode.HasNode("BetterTimeWarp"))
                SettingsNode.AddNode("BetterTimeWarp");

            ConfigNode node = SettingsNode.GetNode("BetterTimeWarp");

            node.SetValue("ScaleCameraSpeed", ScaleCameraSpeed.ToString(), true);
            node.SetValue("UseLosslessPhysics", UseLosslessPhysics.ToString(), true);
            node.SetValue("LosslessUpperThreshold", LosslessUpperThreshold.ToString(), true);

            node.RemoveNodes("CustomWarpRate");
            foreach (var rates in customWarps)
            {
                if (rates != StandardWarp && rates != StandardPhysWarp)
                {
                    ConfigNode rateNode = new ConfigNode("CustomWarpRate");
                    rateNode.AddValue("name", rates.Name);
                    for (int i = 1; i < 8; i++)
                    {
                        if (i < 4 || !rates.Physics)
                        {
                            rateNode.AddValue("warpRate" + i.ToString(), rates.Rates[i].ToString(TimeWarpRates.rateFmt(rates.Rates[i])));
                        }
                    }
                    rateNode.AddValue("physics", rates.Physics);
                    rateNode.AddValue("DisableECAtHighWarp", rates.DisableECAtHighWarp);
                    rateNode.AddValue("ecIndexDisable", rates.ecIndexDisable);
                    node.AddNode(rateNode);
                }
            }

            if (CurrentWarp == null)
            {
                CurrentWarp = StandardWarp;
                Log.Info("CurrentWarp set to StandardWarp  3");
            }
            if (CurrentPhysWarp == null)
                CurrentPhysWarp = StandardPhysWarp;

            node.SetValue("CurrentTimeWarp", CurrentWarp.Name, true);
            node.SetValue("CurrentPhysWarp", CurrentPhysWarp.Name, true);
        }
    }
}

