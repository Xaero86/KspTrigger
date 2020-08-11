using System;
using UnityEngine;

namespace KspTrigger
{
    public class TriggerUI : AbstractUI
    {
        private EventUI _eventUI;
        public EventUI EventUI { set { _eventUI = value; } }
        private ConditionUI _conditionUI;
        public ConditionUI ConditionUI { set { _conditionUI = value; } }
        private ActionUI _actionUI;
        public ActionUI ActionUI { set { _actionUI = value; } }
        private TimerUI _timerUI;
        public TimerUI TimerUI { set { _timerUI = value; } }
        
        public override Vector2 Size
        {
            get
            {
                if (!_displayMultiConf)
                {
                    return new Vector2(_mainWidth, _windowHeight);
                }
                else
                {
                    return new Vector2(_mainWidth+_multiConfWidth, _windowHeight);
                }
            }
        }
        protected override int _windowID { get { return Utils.MAIN_WINDOW_ID; } }
        protected override string _windowTitle { get { return "Configure Triggers"; } }
        
        private readonly float _mainWidth = 500;
        private readonly float _multiConfWidth = 150;
        private readonly float _windowHeight = 300;
        private Rect _triggerArea = Rect.zero;
        private Rect _multiConfArea = Rect.zero;
        private Vector2 _scrollViewVector = Vector2.zero;
        private bool _displayMultiConf = false;
        public bool DisplayMultiConf { get { return _displayMultiConf; } }
        
        public TriggerUI(Vector2 pos, bool displayMultiConf) : base(pos)
        {
            _displayMultiConf = displayMultiConf;
        }
        
        protected override bool _isDisplayed()
        {
            return true;
        }
        
        protected override void _doWindow()
        {
            float nameWidth = 80.0f;
            float buttonWidthT = Math.Max(GUI.skin.button.CalcSize(new GUIContent("Event")).x, 
                                 Math.Max(GUI.skin.button.CalcSize(new GUIContent("Condition")).x,
                                          GUI.skin.button.CalcSize(new GUIContent("Actions")).x));
            float resetWidth = GUI.skin.toggle.CalcSize(new GUIContent("R")).x + GUI.skin.label.CalcSize(new GUIContent("Reset auto")).x;
            bool toggleMultiConf = false;
            
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add trigger"))
            {
                _vesselTriggers.AddNewTrigger();
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
            if (GUILayout.Button(_displayMultiConf?"Multi configuration <<":"Multi configuration >>"))
            {
                toggleMultiConf = true;
            }
            GUILayout.FlexibleSpace();
            if (_displayMultiConf)
            {
                GUILayout.Space(_multiConfWidth);
            }
            GUILayout.EndHorizontal();
            
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                RectOffset rctOff = GUI.skin.button.margin;
                // position of area computed using "Add trigger" button position
                _triggerArea = new Rect(rctOff.left,
                                        lastRect.y+lastRect.height+rctOff.top,
                                        _mainWidth-rctOff.horizontal,
                                        _windowHeight-(lastRect.y+lastRect.height+rctOff.vertical));
                _multiConfArea = new Rect(_mainWidth+rctOff.left,
                                        lastRect.y+rctOff.top,
                                        _multiConfWidth-rctOff.horizontal,
                                        _windowHeight-(lastRect.y+rctOff.vertical));
            }
            GUILayout.BeginArea(_triggerArea, GUI.skin.GetStyle("Box"));
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
                if (GUILayout.Button(new GUIContent("Event", tooltip), style, GUILayout.Width(buttonWidthT)))
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
                if (GUILayout.Button(new GUIContent("Condition", tooltip), style, GUILayout.Width(buttonWidthT)))
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
                if (GUILayout.Button(new GUIContent("Actions", tooltip), style, GUILayout.Width(buttonWidthT)))
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
            
            if (_displayMultiConf)
            {
                // Multi configuration
                GUILayout.BeginArea(_multiConfArea, GUI.skin.GetStyle("Box"));
                GUILayout.BeginVertical();
                GUILayout.Label("Current configuration:");
                // Select configuration
                GUILayout.FlexibleSpace();
                int newConfigIndex = _popupUI.GUILayoutPopup("popupConfiguration", _vesselTriggers.ConfigList, _vesselTriggers.ConfigIndex);
                // New configuration
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("New", GUILayout.Width(80.0f)))
                {
                    ModalDialog.ModalInput(_windowRect,"New configuration name",NewConfig);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                // Rename configuration
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Rename", GUILayout.Width(80.0f)))
                {
                    ModalDialog.ModalInput(_windowRect,"New configuration name",RenameConfig,_vesselTriggers.CurrentName);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                // Delete configuration
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete", GUILayout.Width(80.0f)))
                {
                    _vesselTriggers.RemoveCurrent();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                // Import configuration
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Import", GUILayout.Width(80.0f)))
                {
                    ModalDialog.ModalCombo(_windowRect,"Configuration file name",ImportConfig, _vesselTriggers.ImportableList);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                // Export configuration
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Export", GUILayout.Width(80.0f)))
                {
                    ModalDialog.ModalInput(_windowRect,"Configuration file name",ExportConfig,_vesselTriggers.CurrentName);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndArea();
                
                if (Event.current.type == EventType.Repaint)
                {
                    _vesselTriggers.ConfigIndex = newConfigIndex;
                }
            }
            
            if (toggleMultiConf)
            {
                _displayMultiConf = !_displayMultiConf;
            }
        }
        
        public void NewConfig(string name)
        {
            _vesselTriggers.AddNewConfig(name);
        }
        
        public void RenameConfig(string name)
        {
            _vesselTriggers.RenameCurrent(name);
        }
        
        public void ImportConfig(string name)
        {
            _vesselTriggers.ImportConfig(name);
        }
        
        public void ExportConfig(string name)
        {
            bool result = _vesselTriggers.ExportConfig(name, false);
            if (!result)
            {
                ModalDialog.ModalQuestion(_windowRect,"Configuration \""+name+"\" already exists. Erase ?",() => {_vesselTriggers.ExportConfig(name, true);});
            }
        }
    }
}

