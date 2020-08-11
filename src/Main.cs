using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

/* 
profil import export
breaking ground
*/

namespace KspTrigger
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MainKat : MonoBehaviour
    {
        private static Texture2D TEXTURE_BUTTON = null;
        private ApplicationLauncherButton _mainButton = null;
        private bool _displayWindows = false;
        
        // UI
        private TriggerUI _triggerUI;
        private EventUI _eventUI;
        private ConditionUI _conditionUI;
        private ActionUI _actionUI;
        private TimerUI _timerUI;
        
        // Data for current Vessel
        private VesselTriggers _vesselTriggers = null;
        
        public void Awake()
        {
            Utils.InitPath();
            
            if (TEXTURE_BUTTON == null)
            {
                TEXTURE_BUTTON = new Texture2D(1, 1);
                try {
                    byte[] bytes = File.ReadAllBytes(Utils.ICON_FILE);
                    TEXTURE_BUTTON.LoadImage(bytes);
                } catch (Exception e) {
                    Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
                    TEXTURE_BUTTON.SetPixel(0, 0, Color.blue);
                    TEXTURE_BUTTON.Apply();
                }
            }

            _mainButton = ApplicationLauncher.Instance.AddModApplication(
                                () => {_displayWindows = true;}, () => {_displayWindows = false;},
                                null, null, null, null,
                                ApplicationLauncher.AppScenes.FLIGHT, TEXTURE_BUTTON);

            LoadConfigFile();

            _vesselTriggers = null;
        }
        
        public void Start()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            foreach (VesselModule module in vessel.vesselModules)
            {
                if (module is VesselTriggers)
                {
                    _vesselTriggers = (VesselTriggers) module;
                    _vesselTriggers.LoadPersistentData();
                    _triggerUI.VesselTriggers = _vesselTriggers;
                    _eventUI.VesselTriggers = _vesselTriggers;
                    _conditionUI.VesselTriggers = _vesselTriggers;
                    _actionUI.VesselTriggers = _vesselTriggers;
                    _timerUI.VesselTriggers = _vesselTriggers;
                    break;
                }
            }
        }
        
        public void OnDestroy()
        {
            ApplicationLauncher.Instance.RemoveModApplication(_mainButton);
            SaveConfigFile();
        }
        
        public void OnGUI()
        {
            if (_vesselTriggers == null) return;
            
            if (_displayWindows)
            {
                _triggerUI.Display();
                _eventUI.Display();
                _conditionUI.Display();
                _actionUI.Display();
                _timerUI.Display();
            }
            _timerUI.DisplayTimers();
            ModalDialog.Display();
        }
        
        public void Update()
        {
            if (_vesselTriggers != null)
            {
                // Called every frame
                _vesselTriggers.Update();
            }
        }
        
        private const string KEY_WINDOW_NODE       = "WindowPos";
        private const string KEY_TRIGGER_UI_POS    = "triggerUiPos";
        private const string KEY_EVENT_UI_POS      = "eventUiPos";
        private const string KEY_CONDITION_UI_POS  = "conditionUiPos";
        private const string KEY_ACTION_UI_POS     = "actionUiPos";
        private const string KEY_TIMER_CONF_UI_POS = "timerConfUiPos";
        private const string KEY_TIMER_DISP_UI_POS = "timerDispUiPos";
        private const string KEY_MULTI_CONF_DISP   = "multiConfDisp";
        
        private void LoadConfigFile()
        {
            bool displayMultiConf = false;
            Vector2 windowDefPos = new Vector2(20, 40);
            Vector2 triggerUiPos = windowDefPos;
            Vector2 eventUiPos = windowDefPos + new Vector2(100,50);
            Vector2 conditionUiPos = windowDefPos + new Vector2(200,100);
            Vector2 actionUiPos = windowDefPos + new Vector2(300,150);
            Vector2 timerConfUiPos = windowDefPos + new Vector2(400,200);
            Vector2 timerDispUiPos = new Vector2(Screen.width-350,20);
            try {
                Vector2 readPos = Vector2.zero;
                ConfigNode addonConfig = ConfigNode.Load(Utils.CONFIG_FILE);
                ConfigNode windowConfig = addonConfig.GetNode(KEY_WINDOW_NODE);
                if (windowConfig.TryGetValue(KEY_TRIGGER_UI_POS, ref readPos))
                {
                    triggerUiPos = readPos;
                }
                if (windowConfig.TryGetValue(KEY_EVENT_UI_POS, ref readPos))
                {
                    eventUiPos = readPos;
                }
                if (windowConfig.TryGetValue(KEY_CONDITION_UI_POS, ref readPos))
                {
                    conditionUiPos = readPos;
                }
                if (windowConfig.TryGetValue(KEY_ACTION_UI_POS, ref readPos))
                {
                    actionUiPos = readPos;
                }
                if (windowConfig.TryGetValue(KEY_TIMER_CONF_UI_POS, ref readPos))
                {
                    timerConfUiPos = readPos;
                }
                if (windowConfig.TryGetValue(KEY_TIMER_DISP_UI_POS, ref readPos))
                {
                    timerDispUiPos = readPos;
                }
                windowConfig.TryGetValue(KEY_MULTI_CONF_DISP, ref displayMultiConf);
            } catch (Exception) { }
            
            _triggerUI = new TriggerUI(triggerUiPos, displayMultiConf);
            _eventUI = new EventUI(eventUiPos);
            _conditionUI = new ConditionUI(conditionUiPos);
            _actionUI = new ActionUI(actionUiPos);
            _timerUI = new TimerUI(timerConfUiPos, timerDispUiPos);
            
            _triggerUI.EventUI = _eventUI;
            _triggerUI.ConditionUI = _conditionUI;
            _triggerUI.ActionUI = _actionUI;
            _triggerUI.TimerUI = _timerUI;
        }
        
        private void SaveConfigFile()
        {
            ConfigNode addonConfig = new ConfigNode("KspTrigger");
            ConfigNode windowConfig = addonConfig.AddNode(KEY_WINDOW_NODE);
            windowConfig.SetValue(KEY_TRIGGER_UI_POS, _triggerUI.Position, true);
            windowConfig.SetValue(KEY_EVENT_UI_POS, _eventUI.Position, true);
            windowConfig.SetValue(KEY_CONDITION_UI_POS, _conditionUI.Position, true);
            windowConfig.SetValue(KEY_ACTION_UI_POS, _actionUI.Position, true);
            windowConfig.SetValue(KEY_TIMER_CONF_UI_POS, _timerUI.Position, true);
            windowConfig.SetValue(KEY_TIMER_DISP_UI_POS, _timerUI.PositionDisp, true);
            windowConfig.SetValue(KEY_MULTI_CONF_DISP, _triggerUI.DisplayMultiConf, true);

            try {
                Directory.CreateDirectory(Utils.CONFIG_FILE_DIR);
                addonConfig.Save(Utils.CONFIG_FILE);
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
    }
}

