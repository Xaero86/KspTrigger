using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;

/* 
KAT: Kerbal Auto Trigger
-- condition --
et/ou
-- general --
button action color
github
save window position
profil multi + import export
vessel switch ?
*/

namespace KspTrigger
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TriggerUI : MonoBehaviour
    {
        private static Texture2D TEXTURE_BUTTON = null;

        private ApplicationLauncherButton _mainButton = null;
        private bool _displayWindow = false;
        private Rect _windowRect = new Rect(20, 40, 500, 300);
        private Rect _boxPos = Rect.zero;
        private Vector2 _scrollViewVector = Vector2.zero;
        
        private EventUI _eventUI;
        private ConditionUI _conditionUI;
        private ActionUI _actionUI;
        private TimerUI _timerUI;
        
        // Data for current Vessel
        private VesselTriggers _vesselTriggers = null;
        
        public void Awake()
        {
            if (TEXTURE_BUTTON == null)
            {
                TEXTURE_BUTTON = new Texture2D(1, 1);
                try {
                    byte[] bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../button.png"));
                    TEXTURE_BUTTON.LoadImage(bytes);
                } catch (Exception e) {
                    Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
                    TEXTURE_BUTTON.SetPixel(0, 0, Color.blue);
                    TEXTURE_BUTTON.Apply();
                }
            }

            _mainButton = ApplicationLauncher.Instance.AddModApplication(
                                () => {_displayWindow = true;}, () => {_displayWindow = false;},
                                null, null, null, null,
                                ApplicationLauncher.AppScenes.FLIGHT, TEXTURE_BUTTON);

            _eventUI = new EventUI(_windowRect.position + new Vector2(100,50));
            _conditionUI = new ConditionUI(_windowRect.position + new Vector2(200,100));
            _actionUI = new ActionUI(_windowRect.position + new Vector2(300,150));
            _timerUI = new TimerUI(_windowRect.position + new Vector2(400,200));
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
        }
        
        public void OnGUI()
        {
            if (_vesselTriggers == null) return;
            
            if (_displayWindow)
            {
                _windowRect = GUI.Window(Utils.MAIN_WINDOW_ID, _windowRect, DoWindow, "Configure Trigger");
                _eventUI.Display();
                _conditionUI.Display();
                _actionUI.Display();
                _timerUI.Display();
            }
            _timerUI.DisplayTimers();
        }
        
        public void DoWindow(int windowID)
        {
            Utils.InitGui();
            
            float nameWidth = 80.0f;
            float buttonWidth =  Math.Max(GUI.skin.button.CalcSize(new GUIContent("Event")).x, 
                                 Math.Max(GUI.skin.button.CalcSize(new GUIContent("Condition")).x,
                                          GUI.skin.button.CalcSize(new GUIContent("Actions")).x));
            float resetWidth = GUI.skin.toggle.CalcSize(new GUIContent("R")).x + GUI.skin.label.CalcSize(new GUIContent("Reset auto")).x;
            
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add trigger"))
            {
                _vesselTriggers.AddNewConfig();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Configure timers"))
            {
                if (_timerUI.ToggleDisplay())
                {
                    GUI.BringWindowToFront(Utils.TIMER_CONF_WINDOW_ID);
                    GUI.FocusWindow(Utils.TIMER_CONF_WINDOW_ID);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                RectOffset rctOff = GUI.skin.button.margin;
                // position of area computed using "Add trigger" button position
                _boxPos = new Rect(rctOff.left,
                                   lastRect.y+lastRect.height+rctOff.top,
                                   _windowRect.width-rctOff.horizontal,
                                   _windowRect.height-(lastRect.y+lastRect.height+rctOff.vertical));
            }
            GUILayout.BeginArea(_boxPos, GUI.skin.GetStyle("Box"));
            _scrollViewVector = GUILayout.BeginScrollView(_scrollViewVector);
            foreach (Trigger trigger in _vesselTriggers.Triggers.ToArray())
            {
                string tooltip;
                GUIStyle style;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                // Name
                trigger.Name = GUILayout.TextField(trigger.Name, GUILayout.Width(nameWidth));
                
                GUILayout.FlexibleSpace();
                // Event
                if (trigger.TriggerEvent == null)
                {
                    tooltip = "To be configured";
                    style = Utils.BUTTON_STYLE_INVALID;
                }
                else if (!trigger.TriggerEvent.IsValid())
                {
                    tooltip = "(Invalid) "+trigger.TriggerEvent.ToString();
                    style = Utils.BUTTON_STYLE_INVALID;
                }
                else
                {
                    tooltip = trigger.TriggerEvent.ToString(Event.current.alt);
                    style = Utils.BUTTON_STYLE_VALID;
                }
                if (_eventUI.TriggerToConfigure == trigger)
                {
                    style = Utils.BUTTON_STYLE_PENDING;
                }
                if (GUILayout.Button(new GUIContent("Event", tooltip), style, GUILayout.Width(buttonWidth)))
                {
                    _eventUI.Configure(trigger);
                    GUI.BringWindowToFront(Utils.EVENT_WINDOW_ID);
                    GUI.FocusWindow(Utils.EVENT_WINDOW_ID);
                }
                
                GUILayout.FlexibleSpace();
                // Condition
                tooltip = trigger.TriggerConditions.ToString();
                style = Utils.BUTTON_STYLE_VALID;
                if (_conditionUI.TriggerToConfigure == trigger)
                {
                    style = Utils.BUTTON_STYLE_PENDING;
                }
                if (GUILayout.Button(new GUIContent("Condition", tooltip), style, GUILayout.Width(buttonWidth)))
                {
                    _conditionUI.Configure(trigger);
                    GUI.BringWindowToFront(Utils.CONDITION_WINDOW_ID);
                    GUI.FocusWindow(Utils.CONDITION_WINDOW_ID);
                }
                
                GUILayout.FlexibleSpace();
                // Action
                tooltip = trigger.TriggerActions.ToString();
                style = Utils.BUTTON_STYLE_VALID;
                if (_actionUI.TriggerToConfigure == trigger)
                {
                    style = Utils.BUTTON_STYLE_PENDING;
                }
                else if (!trigger.TriggerActions.IsValid())
                {
                    style = Utils.BUTTON_STYLE_INVALID;
                }
                if (GUILayout.Button(new GUIContent("Actions", tooltip), style, GUILayout.Width(buttonWidth)))
                {
                    _actionUI.Configure(trigger);
                    GUI.BringWindowToFront(Utils.ACTION_WINDOW_ID);
                    GUI.FocusWindow(Utils.ACTION_WINDOW_ID);
                }
                
                GUILayout.FlexibleSpace();
                // Reset
                if (trigger.TriggerEvent != null)
                {
                    if (trigger.TriggerEvent.HasBeenTriggered)
                    {
                        if (GUILayout.Button(new GUIContent("Reset"), GUILayout.Width(resetWidth)))
                        {
                            trigger.TriggerEvent.Reset();
                        }
                    }
                    else
                    {
                        trigger.TriggerEvent.AutoReset = GUILayout.Toggle(trigger.TriggerEvent.AutoReset, "Reset auto", GUILayout.Width(resetWidth));
                    }
                }
                else
                {
                    GUILayout.Toggle(false, "Reset auto", GUILayout.Width(resetWidth));
                }

                GUILayout.FlexibleSpace();
                // Remove
                if (GUILayout.Button(new GUIContent("X", "Remove")))
                {
                    _vesselTriggers.RemoveTrigger(trigger);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            
            // Tooltip
            if ((GUI.tooltip != "") && (Event.current.type == EventType.Repaint))
            {
                GUIContent content = new GUIContent(GUI.tooltip);
                GUI.Label(new Rect(Event.current.mousePosition, GUI.skin.box.CalcSize(content)), content);
            }
        }
        
        public void Update()
        {
            if (_vesselTriggers != null)
            {
                // Called every frame
                _vesselTriggers.Update();
            }
        }
    }
}

