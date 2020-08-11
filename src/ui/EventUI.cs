using System;
using UnityEngine;

namespace KspTrigger
{
    public class EventUI : AbstractUI
    {
        private Trigger _triggerToConfigure = null;
        public Trigger TriggerToConfigure { get { return _triggerToConfigure; } }
        
        public override Vector2 Size { get { return new Vector2(450, 200); } }
        protected override int _windowID { get { return Utils.EVENT_WINDOW_ID; } }
        protected override string _windowTitle { get { return "Configure Event"; } }
        
        private Rect _boxLeftPos = Rect.zero;
        private Rect _boxRightPos = Rect.zero;
        private Vector2 _scrollVectPart = Vector2.zero;
        private Vector2 _scrollVectVessel = Vector2.zero;
        
        private const float LEFT_MARGING = 20.0f;
        private const float RIGHT_MARGING = 20.0f;
        
        private int _eventIndexType = 0;
        private TriggerEvent _eventPart = null;
        private TriggerEvent _eventFlight = null;
        private TriggerEvent _eventTimer = null;
        private TriggerEvent _currentEvent = null;
        
        public EventUI(Vector2 pos) : base(pos)
        {
        }
        
        protected override bool _isDisplayed()
        {
            return _triggerToConfigure != null;
        }
        
        protected override void _doWindow()
        {
            GUILayout.BeginVertical();
            int newIndexEvent = GUILayout.Toolbar(_eventIndexType, Enum.GetNames(typeof(TriggerEventType)));
            if (newIndexEvent != _eventIndexType)
            {
                _eventIndexType = newIndexEvent;
            }
            if (Event.current.type == EventType.Repaint)
            {
                RectOffset rctOff = GUI.skin.button.margin;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                float nextY = lastRect.y+lastRect.height+rctOff.bottom;
                // position of area computed using Toolbar position
                _boxLeftPos = new Rect(rctOff.left,
                                nextY+rctOff.top,
                                Size.x/2-rctOff.horizontal,
                                Size.y-(2*nextY+rctOff.vertical));
                _boxRightPos = new Rect(Size.x/2+rctOff.left,
                                nextY+rctOff.top,
                                Size.x/2-rctOff.horizontal,
                                Size.y-(2*nextY+rctOff.vertical));
            }
            switch ((TriggerEventType) _eventIndexType)
            {
                case TriggerEventType.Part:
                    DisplayPartConf();
                    break;
                case TriggerEventType.Flight:
                    DisplayFlightConf();
                    break;
                case TriggerEventType.Timer:
                    DisplayTimerConf();
                    break;
            }
            GUILayout.FlexibleSpace();
            // OK / CANCEL
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK") && (_currentEvent != null))
            {
                if (_currentEvent.IsValid())
                {
                    _currentEvent.UpdatePersistentData();
                    _triggerToConfigure.TriggerEvent = _currentEvent;
                    Close();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        
        public void Close()
        {
            _triggerToConfigure = null;
        }

        private void DisplayPartConf()
        {
            if (_eventPart == null)
            {
                _eventPart = new TriggerEvent(TriggerEventType.Part, _vesselTriggers);
            }
            _currentEvent = _eventPart;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectPart = GUILayout.BeginScrollView(_scrollVectPart, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Part
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Part triggering the event: ");
            GUILayout.EndHorizontal();
            // Property
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Property to trigger: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (((TriggerConditionPart) _eventPart.Condition).Parameters != null)
            {
                for (int i = 0; i < ((TriggerConditionPart) _eventPart.Condition).Parameters.Length; i++)
                {
                    TypedData param = ((TriggerConditionPart) _eventPart.Condition).Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (((TriggerConditionPart) _eventPart.Condition).TargetValue != null)
            {
                // Comparator
                GUILayout.BeginHorizontal();
                GUILayout.Space(LEFT_MARGING);
                GUILayout.Label("Comparator: ");
                GUILayout.EndHorizontal();
                // Target
                GUILayout.BeginHorizontal();
                GUILayout.Space(LEFT_MARGING);
                GUILayout.Label("Target value: ");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            _scrollVectPart = GUILayout.BeginScrollView(_scrollVectPart);
            GUILayout.BeginVertical();
            // Part
            GUILayout.BeginHorizontal();
            string buttonLabel = (((TriggerConditionPart) _eventPart.Condition).ConditionPart != null) ? ((TriggerConditionPart) _eventPart.Condition).ConditionPart.ToString() : "Select";
            if (GUILayout.Button(new GUIContent(buttonLabel)))
            {
                ModalDialog.ModalPartSelector(_windowRect,"Select Part to test",SelectPart,((TriggerConditionPart) _eventPart.Condition).ConditionPart);
            }
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Property
            GUILayout.BeginHorizontal();
            int newPropertyIndex = _popupUI.GUILayoutPopup("popupPartParam", ((TriggerConditionPart) _eventPart.Condition).PropertyList, ((TriggerConditionPart) _eventPart.Condition).PropertyIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (((TriggerConditionPart) _eventPart.Condition).Parameters != null)
            {
                for (int i = 0; i < ((TriggerConditionPart) _eventPart.Condition).Parameters.Length; i++)
                {
                    TypedData param = ((TriggerConditionPart) _eventPart.Condition).Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (((TriggerConditionPart) _eventPart.Condition).TargetValue != null)
            {
                TypedData target = ((TriggerConditionPart) _eventPart.Condition).TargetValue;
                // Comparator
                GUILayout.BeginHorizontal();
                ((TriggerConditionPart) _eventPart.Condition).Comparator = (ComparatorType) _popupUI.GUILayoutPopup("popupPartOper", target.ComparatorList, (int) ((TriggerConditionPart) _eventPart.Condition).Comparator);
                GUILayout.Space(RIGHT_MARGING);
                GUILayout.EndHorizontal();
                // Target
                GUILayout.BeginHorizontal();
                target.DisplayLayout(_popupUI);
                GUILayout.Space(RIGHT_MARGING);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            if (Event.current.type == EventType.Repaint)
            {
                ((TriggerConditionPart) _eventPart.Condition).PropertyIndex = newPropertyIndex;
            }
        }
        
        private void DisplayFlightConf()
        {
            if (_eventFlight == null)
            {
                _eventFlight = new TriggerEvent(TriggerEventType.Flight, _vesselTriggers);
            }
            _currentEvent = _eventFlight;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectVessel = GUILayout.BeginScrollView(_scrollVectVessel, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Property
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Property to trigger: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (((TriggerConditionFlight) _eventFlight.Condition).Parameters != null)
            {
                for (int i = 0; i < ((TriggerConditionFlight) _eventFlight.Condition).Parameters.Length; i++)
                {
                    TypedData param = ((TriggerConditionFlight) _eventFlight.Condition).Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (((TriggerConditionFlight) _eventFlight.Condition).TargetValue != null)
            {
                // Comparator
                GUILayout.BeginHorizontal();
                GUILayout.Space(LEFT_MARGING);
                GUILayout.Label("Comparator: ");
                GUILayout.EndHorizontal();
                // Target
                GUILayout.BeginHorizontal();
                GUILayout.Space(LEFT_MARGING);
                GUILayout.Label("Target value: ");
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            _scrollVectVessel = GUILayout.BeginScrollView(_scrollVectVessel);
            GUILayout.BeginVertical();
            // Property
            GUILayout.BeginHorizontal();
            int newPropertyIndex = _popupUI.GUILayoutPopup("popupFlightParam", TriggerConditionFlight.PropertyList, ((TriggerConditionFlight) _eventFlight.Condition).PropertyIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (((TriggerConditionFlight) _eventFlight.Condition).Parameters != null)
            {
                for (int i = 0; i < ((TriggerConditionFlight) _eventFlight.Condition).Parameters.Length; i++)
                {
                    TypedData param = ((TriggerConditionFlight) _eventFlight.Condition).Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (((TriggerConditionFlight) _eventFlight.Condition).TargetValue != null)
            {
                TypedData target = ((TriggerConditionFlight) _eventFlight.Condition).TargetValue;
                // Comparator
                GUILayout.BeginHorizontal();
                ((TriggerConditionFlight) _eventFlight.Condition).Comparator = (ComparatorType) _popupUI.GUILayoutPopup("popupFlightOper", target.ComparatorList, (int) ((TriggerConditionFlight) _eventFlight.Condition).Comparator);
                GUILayout.Space(RIGHT_MARGING);
                GUILayout.EndHorizontal();
                // Target
                GUILayout.BeginHorizontal();
                target.DisplayLayout(_popupUI);
                GUILayout.Space(RIGHT_MARGING);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
                        
            if (Event.current.type == EventType.Repaint)
            {
                ((TriggerConditionFlight) _eventFlight.Condition).PropertyIndex = newPropertyIndex;
            }
        }

        private void DisplayTimerConf()
        {
            if (_eventTimer == null)
            {
                _eventTimer = new TriggerEvent(TriggerEventType.Timer, _vesselTriggers);
            }
            _currentEvent = _eventTimer;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            GUILayout.BeginVertical();
            // Name
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Name: ");
            GUILayout.EndHorizontal();
            // TargetDate
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Target date: ");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            GUILayout.BeginVertical();
            // Name
            GUILayout.BeginHorizontal();
            ((TriggerConditionTimer) _eventTimer.Condition).Name = GUILayout.TextField(((TriggerConditionTimer) _eventTimer.Condition).Name, ((TriggerConditionTimer) _eventTimer.Condition).TimerValid ? Utils.TF_STYLE_VALID : Utils.TF_STYLE_INVALID);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // TargetDate / InitDate
            GUILayout.BeginHorizontal();
            ((TriggerConditionTimer) _eventTimer.Condition).TargetDate.DisplayLayout(_popupUI);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        public void Configure(Trigger triggerToConfigure)
        {
            if ((triggerToConfigure == null) || (triggerToConfigure == _triggerToConfigure))
            {
                return;
            }
            _triggerToConfigure = triggerToConfigure;
            _eventPart = null;
            _eventFlight = null;
            _eventTimer = null;
            _currentEvent = null;
            if (_triggerToConfigure.TriggerEvent == null)
            {
                _eventIndexType = (int) TriggerEventType.Flight;
            }
            else
            {
                _eventIndexType = (int) _triggerToConfigure.TriggerEvent.Type;
                switch (_triggerToConfigure.TriggerEvent.Type)
                {
                    case TriggerEventType.Part:
                        _eventPart = new TriggerEvent(_triggerToConfigure.TriggerEvent);
                        break;
                    case TriggerEventType.Flight:
                        _eventFlight = new TriggerEvent(_triggerToConfigure.TriggerEvent);
                        break;
                    case TriggerEventType.Timer:
                        _eventTimer = new TriggerEvent(_triggerToConfigure.TriggerEvent);
                        break;
                }
            }
        }
        
        public void SelectPart(Part part)
        {
            if (_eventPart != null)
            {
                ((TriggerConditionPart) _eventPart.Condition).ConditionPart = part;
            }
        }
    }
}

