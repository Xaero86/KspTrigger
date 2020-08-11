using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class ConditionUI : AbstractUI
    {
        private Trigger _triggerToConfigure = null;
        public Trigger TriggerToConfigure { get { return _triggerToConfigure; } }
        
        public override Vector2 Size { get { return new Vector2(530, 260); } }
        protected override int _windowID { get { return Utils.CONDITION_WINDOW_ID; } }
        protected override string _windowTitle { get { return "Configure Conditions"; } }

        private const int LEFT_PANEL_WIDTH = 80;
        private const float LEFT_MARGING = 20.0f;
        private const float RIGHT_MARGING = 20.0f;
        
        private Rect _listRect = Rect.zero;
        private Rect _mainRect = Rect.zero;
        private Vector2 _scrollVectCondition = Vector2.zero;
        private Rect _boxLeftPos = Rect.zero;
        private Rect _boxRightPos = Rect.zero;
        private Vector2 _scrollVectPart = Vector2.zero;
        private Vector2 _scrollVectVessel = Vector2.zero;
        
        private int _conditionIndex = -1;
        private int _conditionIndexType = 0;
        private TriggerConditionPart _conditionPart = null;
        private TriggerConditionFlight _conditionFlight = null;
        private TriggerConditionTimer _conditionTimer = null;
        private TriggerCondition _currentCondition = null;
        private TriggerConditions _conditions = null;
        
        public ConditionUI(Vector2 pos) : base(pos)
        {
            _listRect = new Rect(0, 0, LEFT_PANEL_WIDTH, Size.y);
            _mainRect = new Rect(LEFT_PANEL_WIDTH, 0, Size.x-LEFT_PANEL_WIDTH, Size.y);
        }
        
        protected override bool _isDisplayed()
        {
            return _triggerToConfigure != null;
        }
        
        protected override void _doWindow()
        {
            GUILayout.BeginVertical();
            // fake label to get position
            GUILayout.Label(" ");
            if (Event.current.type == EventType.Repaint)
            {
                RectOffset rctOff = GUI.skin.button.margin;
                Rect lastRect = GUILayoutUtility.GetLastRect();
                _listRect.y = lastRect.y;
                _mainRect.y = lastRect.y;
                _listRect.height = Size.y - 3*lastRect.y - 3*rctOff.vertical;
                _mainRect.height = Size.y - 3*lastRect.y - 3*rctOff.vertical;
            }

            DisplayLeftPanel();
            
            if (_conditionIndex >= 0)
            {
                DisplayRightPanel();
            }
            
            GUILayout.FlexibleSpace();
            // Condition combination
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _conditions.Combination = (ConditionCombination) _popupUI.GUILayoutPopup("CondCombination", Enum.GetNames(typeof(ConditionCombination)), (int) _conditions.Combination);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            // OK / CANCEL
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK")) // TODO
            {
                _triggerToConfigure.TriggerConditions = _conditions;
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
        }
        
        private void DisplayLeftPanel()
        {
            // Left panel: condition list
            GUILayout.BeginArea(_listRect);
            _scrollVectCondition = GUILayout.BeginScrollView(_scrollVectCondition, false, true);
            GUILayout.BeginVertical();
            for (int i = 0; i < _conditions.Count; i++)
            {
                string tooltip;
                GUIStyle style;
                // Button to select condition
                if ((i == _conditionIndex) && (_currentCondition != null) &&
                    (_currentCondition.Modified || (_currentCondition.GetType() != _conditions[i].GetType())))
                {
                    tooltip = "Don't forget to apply";
                    style = Utils.BUTTON_STYLE_PENDING;
                }
                else
                {
                    if ((_conditions[i] == null) || !_conditions[i].IsValid())
                    {
                        tooltip = "To be configured";
                        style = Utils.BUTTON_STYLE_INVALID;
                    }
                    else
                    {
                        tooltip = _conditions[i].ToString();
                        style = Utils.BUTTON_STYLE_VALID;
                    }
                }
                if (GUILayout.Button(new GUIContent(i.ToString(), tooltip), style))
                {
                    if (_conditionIndex != i)
                    {
                        SelectCondition(i);
                    }
                }
            }
            if (GUILayout.Button(new GUIContent("+", "Add new condition")))
            {
                _conditions.Add(null);
                SelectCondition(_conditions.Count-1);
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DisplayRightPanel()
        {
            // Right panel: condition config
            GUILayout.BeginArea(_mainRect, GUI.skin.GetStyle("Box"));
            GUILayout.BeginVertical();
            int newIndexConditionType = GUILayout.Toolbar(_conditionIndexType, Enum.GetNames(typeof(TriggerConditionType)));
            if (newIndexConditionType != _conditionIndexType)
            {
                _conditionIndexType = newIndexConditionType;
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
            switch ((TriggerConditionType) _conditionIndexType)
            {
                case TriggerConditionType.Part:
                    DisplayPartConf();
                    break;
                case TriggerConditionType.Flight:
                    DisplayFlightConf();
                    break;
                case TriggerConditionType.Timer:
                    DisplayTimerConf();
                    break;
            }
            GUILayout.FlexibleSpace();
            // APPLY / REMOVE
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply") && (_currentCondition != null))
            {
                if (_currentCondition.IsValid())
                {
                    _currentCondition.Acquit();
                    _currentCondition.UpdatePersistentData();
                    _conditions[_conditionIndex] = _currentCondition;
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove") && (_conditionIndex >= 0))
            {
                _conditions.RemoveAt(_conditionIndex);
                SelectCondition(-1);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        public void Close()
        {
            _triggerToConfigure = null;
        }
        
        private void DisplayPartConf()
        {
            if (_conditionPart == null)
            {
                _conditionPart = new TriggerConditionPart(_vesselTriggers);
            }
            _currentCondition = _conditionPart;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectPart = GUILayout.BeginScrollView(_scrollVectPart, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Part
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Part to evaluate: ");
            GUILayout.EndHorizontal();
            // Property
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Property to evaluate: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (_conditionPart.Parameters != null)
            {
                for (int i = 0; i < _conditionPart.Parameters.Length; i++)
                {
                    TypedData param = _conditionPart.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (_conditionPart.TargetValue != null)
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
            string buttonLabel = (_conditionPart.ConditionPart != null) ? _conditionPart.ConditionPart.ToString() : "Select";
            if (GUILayout.Button(new GUIContent(buttonLabel)))
            {
                ModalDialog.ModalPartSelector(_windowRect,"Select Part to test",SelectPart,_conditionPart.ConditionPart);
            }
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Property
            GUILayout.BeginHorizontal();
            int newPropertyIndex = _popupUI.GUILayoutPopup("popupPartParam", _conditionPart.PropertyList, _conditionPart.PropertyIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (_conditionPart.Parameters != null)
            {
                for (int i = 0; i < _conditionPart.Parameters.Length; i++)
                {
                    TypedData param = _conditionPart.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (_conditionPart.TargetValue != null)
            {
                TypedData target = _conditionPart.TargetValue;
                // Comparator
                GUILayout.BeginHorizontal();
                _conditionPart.Comparator = (ComparatorType) _popupUI.GUILayoutPopup("popupPartOper", target.ComparatorList, (int) _conditionPart.Comparator);
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
                _conditionPart.PropertyIndex = newPropertyIndex;
            }
        }
        
        private void DisplayFlightConf()
        {
            if (_conditionFlight == null)
            {
                _conditionFlight = new TriggerConditionFlight(_vesselTriggers);
            }
            _currentCondition = _conditionFlight;
            // Left column
            GUILayout.BeginArea(_boxLeftPos);
            _scrollVectVessel = GUILayout.BeginScrollView(_scrollVectVessel, GUIStyle.none, GUIStyle.none);
            GUILayout.BeginVertical();
            // Property
            GUILayout.BeginHorizontal();
            GUILayout.Space(LEFT_MARGING);
            GUILayout.Label("Property to evaluate: ");
            GUILayout.EndHorizontal();
            // Parameters
            if (_conditionFlight.Parameters != null)
            {
                for (int i = 0; i < _conditionFlight.Parameters.Length; i++)
                {
                    TypedData param = _conditionFlight.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(LEFT_MARGING);
                        GUILayout.Label(param.Name);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (_conditionFlight.TargetValue != null)
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
            int newPropertyIndex = _popupUI.GUILayoutPopup("popupFlightParam", TriggerConditionFlight.PropertyList, _conditionFlight.PropertyIndex);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // Parameters
            if (_conditionFlight.Parameters != null)
            {
                for (int i = 0; i < _conditionFlight.Parameters.Length; i++)
                {
                    TypedData param = _conditionFlight.Parameters[i];
                    if ((param != null) && param.Configurable)
                    {
                        GUILayout.BeginHorizontal();
                        param.DisplayLayout(_popupUI);
                        GUILayout.Space(RIGHT_MARGING);
                        GUILayout.EndHorizontal();
                    }
                }
            }
            if (_conditionFlight.TargetValue != null)
            {
                TypedData target = _conditionFlight.TargetValue;
                // Comparator
                GUILayout.BeginHorizontal();
                _conditionFlight.Comparator = (ComparatorType) _popupUI.GUILayoutPopup("popupFlightOper", target.ComparatorList, (int) _conditionFlight.Comparator);
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
                _conditionFlight.PropertyIndex = newPropertyIndex;
            }
        }
        
        private void DisplayTimerConf()
        {
            if (_conditionTimer == null)
            {
                _conditionTimer = new TriggerConditionTimer(_vesselTriggers);
            }
            _currentCondition = _conditionTimer;
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
            GUILayout.Label("Date reached: ");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // Right column
            GUILayout.BeginArea(_boxRightPos);
            GUILayout.BeginVertical();
            // Name
            GUILayout.BeginHorizontal();
            _conditionTimer.Name = GUILayout.TextField(_conditionTimer.Name, _conditionTimer.TimerValid ? Utils.TF_STYLE_VALID : Utils.TF_STYLE_INVALID);
            GUILayout.Space(RIGHT_MARGING);
            GUILayout.EndHorizontal();
            // TargetDate / InitDate
            GUILayout.BeginHorizontal();
            _conditionTimer.TargetDate.DisplayLayout(_popupUI);
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
            _conditions = new TriggerConditions(_triggerToConfigure.TriggerConditions);
            SelectCondition(-1);
        }
        
        private void SelectCondition(int conditionIndex)
        {
            _conditionIndex = conditionIndex;
            _popupUI.CloseAll();
            _conditionPart = null;
            _conditionFlight = null;
            _conditionTimer = null;
            _currentCondition = null;
            if ((_conditionIndex < 0) || (_conditionIndex >= _conditions.Count))
            {
                return;
            }
            if (_conditions[_conditionIndex] == null)
            {
                _conditionIndexType = (int) TriggerConditionType.Flight;
            }
            else if (_conditions[_conditionIndex] is TriggerConditionPart)
            {
                _conditionIndexType = (int) TriggerConditionType.Part;
                _conditionPart = new TriggerConditionPart((TriggerConditionPart) _conditions[_conditionIndex]);
            }
            else if (_conditions[_conditionIndex] is TriggerConditionFlight)
            {
                _conditionIndexType = (int) TriggerConditionType.Flight;
                _conditionFlight = new TriggerConditionFlight((TriggerConditionFlight) _conditions[_conditionIndex]);
            }
            else if (_conditions[_conditionIndex] is TriggerConditionTimer)
            {
                _conditionIndexType = (int) TriggerConditionType.Timer;
                _conditionTimer = new TriggerConditionTimer((TriggerConditionTimer) _conditions[_conditionIndex]);
            }
        }
        
        public void SelectPart(Part part)
        {
            if (_conditionPart != null)
            {
                _conditionPart.ConditionPart = part;
            }
        }
    }
}

