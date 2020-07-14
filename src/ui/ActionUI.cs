using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class ActionUI
    {
        private VesselTriggers _vesselTriggers = null;
        public VesselTriggers VesselTriggers { set { _vesselTriggers = value; } }
        
        private Trigger _triggerToConfigure = null;
        public Trigger TriggerToConfigure { get { return _triggerToConfigure; } }

        private const int LEFT_PANEL_WIDTH = 80;
        private const float LEFT_MARGING = 20.0f;
        private const float RIGHT_MARGING = 20.0f;
        
        private readonly Vector2 _windowSize = new Vector2(530, 230);
        
        private Rect _windowRect = Rect.zero;
        private Rect _listRect = Rect.zero;
        private Rect _mainRect = Rect.zero;
        private Vector2 _scrollVectAction = Vector2.zero;
        private Rect _boxLeftPos = Rect.zero;
        private Rect _boxRightPos = Rect.zero;
        private Vector2 _scrollVectPart = Vector2.zero;
        private Vector2 _scrollVectVessel = Vector2.zero;
        
        private int _actionIndex = -1;
        private int _actionIndexType = 0;
        private TriggerActionPart _actionPart = null;
        private TriggerActionFlight _actionFlight = null;
        private TriggerActionMessage _actionMessage = null;
        private TriggerActionTimer _actionTimer = null;
        private TriggerAction _currentAction = null;
        private TriggerActions _actions = null;
        
        private PopupUI _popupUI;
        private PartSelector _partSelector;
        
        public ActionUI(Vector2 pos)
        {
            _windowRect = new Rect(pos, _windowSize);
            _listRect = new Rect(0, 0, LEFT_PANEL_WIDTH, _windowSize.y);
            _mainRect = new Rect(LEFT_PANEL_WIDTH, 0, _windowSize.x-LEFT_PANEL_WIDTH, _windowSize.y);
            _popupUI = new PopupUI(Utils.ACTION_WINDOW_ID_POP);
            _partSelector = new PartSelector(this.SelectPart);
        }

        public void Display()
        {
            if (_triggerToConfigure != null)
            {
                _windowRect = GUI.Window(Utils.ACTION_WINDOW_ID, _windowRect, DoWindow, "Configure Actions");
                _popupUI.Display();
            }
        }
        
        public void DoWindow(int windowID)
        {
            if (Event.current.isMouse && (Event.current.button == 0) && (Event.current.type == EventType.MouseUp))
            {
                _popupUI.CloseAll();
            }
            GUILayout.BeginVertical();
            // fake label to get position
            GUILayout.Label(" ");
            if (Event.current.type == EventType.Repaint)
            {
                RectOffset rctOff = GUI.skin.button.margin;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                _listRect.y = lastRect.y;
                _mainRect.y = lastRect.y;
                _listRect.height = _windowSize.y - lastRect.y - rctOff.vertical;
                _mainRect.height = _windowSize.y - 2*lastRect.y - 2*rctOff.vertical;
            }

            DisplayLeftPanel();
            
            if (_actionIndex >= 0)
            {
                DisplayRightPanel();
            }
            
            GUILayout.FlexibleSpace();
            // OK / CANCEL
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK")) // TODO
            {
                _triggerToConfigure.TriggerActions = _actions;
                Close();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            
            // Tooltip
            if ((GUI.tooltip != "") && (Event.current.type == EventType.Repaint))
            {
                GUIContent content = new GUIContent(GUI.tooltip);
                GUI.Label(new Rect(Event.current.mousePosition, GUI.skin.box.CalcSize(content)), content);
            }
        }
        
        private void DisplayLeftPanel()
        {
            // Left panel: action list
            GUILayout.BeginArea(_listRect);
            _scrollVectAction = GUILayout.BeginScrollView(_scrollVectAction, false, true);
            GUILayout.BeginVertical();
            for (int i = 0; i < _actions.Count; i++)
            {
                string tooltip;
                GUIStyle style;
                // Button to select action
                if ((i == _actionIndex) && (_currentAction != null) &&
                    (_currentAction.Modified || (_currentAction.GetType() != _actions[i].GetType())))
                {
                    tooltip = "Don't forget to apply";
                    style = Utils.BUTTON_STYLE_PENDING;
                }
                else
                {
                    if ((_actions[i] == null) || !_actions[i].IsValid())
                    {
                        tooltip = "To be configured";
                        style = Utils.BUTTON_STYLE_INVALID;
                    }
                    else
                    {
                        tooltip = _actions[i].ToString();
                        style = Utils.BUTTON_STYLE_VALID;
                    }
                }
                if (GUILayout.Button(new GUIContent(i.ToString(), tooltip), style))
                {
                    if (_actionIndex != i)
                    {
                        _partSelector.CancelSelect();
                        SelectAction(i);
                    }
                }
            }
            if (GUILayout.Button(new GUIContent("+", "Add new action")))
            {
                _actions.Add(null);
                SelectAction(_actions.Count-1);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DisplayRightPanel()
        {
            // Right panel: action config
            GUILayout.BeginArea(_mainRect, GUI.skin.GetStyle("Box"));
            GUILayout.BeginVertical();
            int newIndexActionType = GUILayout.Toolbar(_actionIndexType, Enum.GetNames(typeof(TriggerActionType)));
            if (newIndexActionType != _actionIndexType)
            {
                _partSelector.CancelSelect();
                _actionIndexType = newIndexActionType;
            }
            if (Event.current.type == EventType.Repaint)
            {
                RectOffset rctOff = GUI.skin.button.margin;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                float nextY = lastRect.y+lastRect.height+rctOff.bottom;
                // position of area computed using Toolbar position
                _boxLeftPos = new Rect(rctOff.left,
                                nextY+rctOff.top,
                                _mainRect.width/2-rctOff.horizontal,
                                _mainRect.height-(2*nextY+rctOff.vertical));
                _boxRightPos = new Rect(_mainRect.width/2+rctOff.left,
                                nextY+rctOff.top,
                                _mainRect.width/2-rctOff.horizontal,
                                _mainRect.height-(2*nextY+rctOff.vertical));
            }
            switch ((TriggerActionType) _actionIndexType)
            {
                case TriggerActionType.Part:
                    DisplayPartConf();
                    break;
                case TriggerActionType.Flight:
                    DisplayFlightConf();
                    break;
                case TriggerActionType.Message:
                    DisplayMessageConf();
                    break;
                case TriggerActionType.Timer:
                    DisplayTimerConf();
                    break;
            }
            GUILayout.FlexibleSpace();
            // APPLY / REMOVE
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply") && (_currentAction != null))
            {
                if (_currentAction.IsValid())
                {
                    _currentAction.Acquit();
                    _currentAction.UpdatePersistentData();
                    _actions[_actionIndex] = _currentAction;
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove") && (_actionIndex >= 0))
            {
                _actions.RemoveAt(_actionIndex);
                SelectAction(-1);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        public void Close()
        {
            _triggerToConfigure = null;
            _partSelector.CancelSelect();
        }
        
        private void DisplayPartConf()
        {
            if (_actionPart == null)
            {
                _actionPart = new TriggerActionPart(_vesselTriggers);
            }
            _currentAction = _actionPart;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectPart = GUILayout.BeginScrollView(_scrollVectPart, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Part
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Part to act on: ");
            GUILayout.EndHorizontal();
            // Action
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Action: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (_actionPart.Parameters != null)
            {
                for (int i = 0; i < _actionPart.Parameters.Length; i++)
                {
                    TypedData param = _actionPart.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
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
            _partSelector.DisplayLayout(_actionPart.ActionPart);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Action
            GUILayout.BeginHorizontal();
            int newActionIndex = _popupUI.GUILayoutPopup("popupPartMetho", _actionPart.ActionList, _actionPart.ActionIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (_actionPart.Parameters != null)
            {
                for (int i = 0; i < _actionPart.Parameters.Length; i++)
                {
                    TypedData param = _actionPart.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            if (Event.current.type == EventType.Repaint)
            {
                _actionPart.ActionIndex = newActionIndex;
            }
        }
        
        private void DisplayFlightConf()
        {
            if (_actionFlight == null)
            {
                _actionFlight = new TriggerActionFlight(_vesselTriggers);
            }
            _currentAction = _actionFlight;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectVessel = GUILayout.BeginScrollView(_scrollVectVessel, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Action
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Action: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (_actionFlight.Parameters != null)
            {
                for (int i = 0; i < _actionFlight.Parameters.Length; i++)
                {
                    TypedData param = _actionFlight.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            _scrollVectVessel = GUILayout.BeginScrollView(_scrollVectVessel);
            GUILayout.BeginVertical();
            // Action
            GUILayout.BeginHorizontal();
            int newActionIndex = _popupUI.GUILayoutPopup("popupFlightMetho", TriggerActionFlight.ActionList, _actionFlight.ActionIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (_actionFlight.Parameters != null)
            {
                for (int i = 0; i < _actionFlight.Parameters.Length; i++)
                {
                    TypedData param = _actionFlight.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            if (Event.current.type == EventType.Repaint)
            {
                _actionFlight.ActionIndex = newActionIndex;
            }
        }
        
        private void DisplayMessageConf()
        {
            if (_actionMessage == null)
            {
                _actionMessage = new TriggerActionMessage(_vesselTriggers);
            }
            _currentAction = _actionMessage;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            GUILayout.BeginVertical();
            // Message
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Message: ");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            GUILayout.BeginVertical();
            // Message
            GUILayout.BeginHorizontal();
            _actionMessage.Message = GUILayout.TextField(_actionMessage.Message);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void DisplayTimerConf()
        {
            if (_actionTimer == null)
            {
                _actionTimer = new TriggerActionTimer(_vesselTriggers);
            }
            _currentAction = _actionTimer;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            GUILayout.BeginVertical();
            // Name
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Name: ");
            GUILayout.EndHorizontal();
            // Action
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Action: ");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            GUILayout.BeginVertical();
            // Name
            GUILayout.BeginHorizontal();
            _actionTimer.Name = GUILayout.TextField(_actionTimer.Name, _actionTimer.TimerValid ? Utils.TF_STYLE_VALID : Utils.TF_STYLE_INVALID);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Action
            GUILayout.BeginHorizontal();
            _actionTimer.TimerAction = (TriggerActionTimer.TimerActionType) _popupUI.GUILayoutPopup("popupTimerAction", Enum.GetNames(typeof(TriggerActionTimer.TimerActionType)), (int) _actionTimer.TimerAction);
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
            _actions = new TriggerActions(_triggerToConfigure.TriggerActions);
            SelectAction(-1);
        }
        
        private void SelectAction(int actionIndex)
        {
            _actionIndex = actionIndex;
            _partSelector.CancelSelect();
            _popupUI.CloseAll();
            _actionPart = null;
            _actionFlight = null;
            _actionMessage = null;
            _actionTimer = null;
            _currentAction = null;
            if (_actionIndex < 0)
            {
                return;
            }
            if (_actions[_actionIndex] == null)
            {
                _actionIndexType = (int) TriggerActionType.Flight;
            }
            else if (_actions[_actionIndex] is TriggerActionPart)
            {
                _actionIndexType = (int) TriggerActionType.Part;
                _actionPart = new TriggerActionPart((TriggerActionPart) _actions[_actionIndex]);
            }
            else if (_actions[_actionIndex] is TriggerActionFlight)
            {
                _actionIndexType = (int) TriggerActionType.Flight;
                _actionFlight = new TriggerActionFlight((TriggerActionFlight) _actions[_actionIndex]);
            }
            else if (_actions[_actionIndex] is TriggerActionMessage)
            {
                _actionIndexType = (int) TriggerActionType.Message;
                _actionMessage = new TriggerActionMessage((TriggerActionMessage) _actions[_actionIndex]);
            }
            else if (_actions[_actionIndex] is TriggerActionTimer)
            {
                _actionIndexType = (int) TriggerActionType.Timer;
                _actionTimer = new TriggerActionTimer((TriggerActionTimer) _actions[_actionIndex]);
            }
        }
        
        public void SelectPart(Part part)
        {
            if ((_actionPart != null) && (_actionPart.ActionPart != part))
            {
                _actionPart.ActionPart = part;
            }
        }
    }
}

