using System;
using UnityEngine;

namespace BetterTimeWarp
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class BetterTimeWarpInitializer : MonoBehaviour
    {

        const string CfgPath = "BetterTimeWarp/PluginData";

        public static String ROOT_PATH;
        private static String CONFIG_BASE_FOLDER;
        private static String BTW_BASE_FOLDER;
        public static String BTW_CFG_FILE;
        private static String BTW_DEFAULT_CFG_FILE;

        static bool started = false;
        public void Start()
        {
            //only call this once at the beginning of the game
            if (!started)
            {
                ROOT_PATH = KSPUtil.ApplicationRootPath;
                CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
                BTW_BASE_FOLDER = CONFIG_BASE_FOLDER + "BetterTimeWarp/";
                BTW_CFG_FILE = BTW_BASE_FOLDER + "PluginData/BetterTimeWarp.cfg";
                BTW_DEFAULT_CFG_FILE = BTW_BASE_FOLDER + "PluginData/BetterTimeWarp_Defaults.cfg";

                DontDestroyOnLoad(this);
                ConfigNode node;
                //load the settings
                Log.Info("Cfg file: " + BTW_CFG_FILE);
                if (System.IO.File.Exists(BTW_CFG_FILE))
                {

                    BetterTimeWarp.SettingsNode = ConfigNode.Load(BTW_CFG_FILE);
                    Log.Info("Config loaded");
                }
                else
                {
                    BetterTimeWarp.SettingsNode = ConfigNode.Load(BTW_DEFAULT_CFG_FILE);
                    Log.Info("Default configs loaded");
                }

                //if the settings are not found, regenerate them
                if (BetterTimeWarp.SettingsNode == null)
                    BetterTimeWarp.SettingsNode = new ConfigNode();

                if (!BetterTimeWarp.SettingsNode.HasNode("BetterTimeWarp"))
                    BetterTimeWarp.SettingsNode.AddNode("BetterTimeWarp");

                node = BetterTimeWarp.SettingsNode.GetNode("BetterTimeWarp");

                //save the settings, so if they have been regenerated, it exsists and wont cause errors
                BetterTimeWarp.SettingsNode.Save(BTW_CFG_FILE);

                //subscribe to the events so that the settings save and the UI can hide/show
                GameEvents.onGameStateSaved.Add(SaveSettings);
                GameEvents.onGameStateLoad.Add(onGameStateLoad);
                GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
                GameEvents.onShowUI.Add(ShowUI);
                GameEvents.onHideUI.Add(HideUI);

                //make the physical time warp warning not pop up
                GameSettings.SHOW_PWARP_WARNING = false;

                //give every celestial body new time warp altitude limits
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    body.timeWarpAltitudeLimits = new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 100000f, 2000000f };
                }

                //	GameEvents.onLevelWasLoadedGUIReady.Add (OnLevelLoaded);

                started = true;
            }
        }

        void OnDestroy()
        {
            GameEvents.onGameStateSaved.Remove(SaveSettings);
            GameEvents.onGameStateLoad.Remove(onGameStateLoad);
            GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);


            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
        }

        private void onGameStateLoad(ConfigNode data)
        {
            Log.Info("onGameStateLoad: Interval = " + GameSettings.AUTOSAVE_INTERVAL / 60.0 + " min");

            HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams2>().StockAutosaveInterval =
                (int)(GameSettings.AUTOSAVE_INTERVAL / 60.0);
            HighLogic.CurrentGame.Parameters.CustomParams<BTWCustomParams2>().StockAutosaveShortInterval =
                (int)GameSettings.AUTOSAVE_SHORT_INTERVAL;
        }

#if true
        private void onLevelWasLoaded(GameScenes scene)
        {
            //call this every scene that needs BetterTimeWarp
            if (scene == GameScenes.FLIGHT || scene == GameScenes.SPACECENTER || scene == GameScenes.TRACKSTATION)
            {
                BetterTimeWarp.Instance = gameObject.AddComponent<BetterTimeWarp>();
                if (BetterTimeWarp.Instance != null && BetterTimeWarp.Instance.autoDisable)
                {
                    CheatOptions.InfiniteElectricity = false;
                    BetterTimeWarp.Instance.autoDisable = false;
                    ScreenMessages.PostScreenMessage("Enabling EC Usage", 5);
                }

            }
        }
#endif
        //called whenever the game autosaves/quicksaves
        void SaveSettings(Game game)
        {
            BetterTimeWarp.SettingsNode.Save(BTW_CFG_FILE, "BetterTimeWarp: Automatically saved at date " + System.DateTime.Now.ToString());
            Debug.Log("[BetterTimeWarp]: Settings saved");
            BetterTimeWarp.SettingsNode = ConfigNode.Load(BTW_CFG_FILE);
        }

        //these are called when F2 is pressed to hide/show the UI
        void ShowUI()
        {
            BetterTimeWarp.ShowUI = true;
        }
        void HideUI()
        {
            BetterTimeWarp.ShowUI = false;
        }
    }
}

