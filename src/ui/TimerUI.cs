using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class TimerUI
    {
        private VesselTriggers _vesselTriggers = null;
        public VesselTriggers VesselTriggers { set { _vesselTriggers = value; } }
        
        private readonly Vector2 _windowConfSize = new Vector2(400, 200);
        private readonly Vector2 _windowDispSize = new Vector2(300, 150);
        
        private Rect _windowConfRect = Rect.zero;
        private Rect _boxPos = Rect.zero;
        private Vector2 _scrollConfVector = Vector2.zero;
        private bool _displayed = false;
        
        private Rect _windowDispRect = Rect.zero;
        private Vector2 _scrollDispVector = Vector2.zero;
        
        private string _newName = "";
        
        public TimerUI(Vector2 posConf, Vector2 posDisp)
        {
            _windowConfRect = new Rect(posConf, _windowConfSize);
            _windowDispRect = new Rect(posDisp, _windowDispSize);
        }
        
        public Vector2 PositionConf
        {
            get { return _windowConfRect.position; }
        }
        
        public Vector2 PositionDisp
        {
            get { return _windowDispRect.position; }
        }
        
        public bool ToggleDisplay()
        {
            _displayed = !_displayed;
            return _displayed;
        }

        public void Display()
        {
            if ((_vesselTriggers != null) && _displayed)
            {
                _windowConfRect = GUI.Window(Utils.TIMER_CONF_WINDOW_ID, _windowConfRect, DoConfWindow, "Timers configuration");
            }
        }
        
        public void DisplayTimers()
        {
            if (_vesselTriggers == null) return;
            
            int nbToDisplay = 0;
            foreach (KeyValuePair<string,AbsTimer> entry in _vesselTriggers.Timers)
            {
                if (entry.Value.Displayed)
                {
                    nbToDisplay++;
                }
            }
            if (nbToDisplay > 0)
            {
                _windowDispRect = GUI.Window(Utils.TIMER_DISP_WINDOW_ID, _windowDispRect, DoTimerWindow, "Timers");
            }
        }

        public void DoConfWindow(int windowID)
        {
            float labelWidth = GUI.skin.label.CalcSize(new GUIContent("XXXXXXXX")).x;
            float init2Width = GUI.skin.label.CalcSize(new GUIContent("Start at ")).x;
            float initWidth = GUI.skin.textField.CalcSize(new GUIContent("XXXX")).x;
            List<string> removed = new List<string>();
            
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add timer"))
            {
                if (_vesselTriggers.AddTimer(_newName))
                {
                    _newName = "";
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add countdown"))
            {
                if (_vesselTriggers.AddCountdown(_newName))
                {
                    _newName = "";
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X"))
            {
                _displayed = false;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("New timer name: ");
            GUILayout.FlexibleSpace();
            _newName = GUILayout.TextField(_newName, GUILayout.Width(labelWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            if (Event.current.type == EventType.Repaint)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                RectOffset rctOff = GUI.skin.button.margin;
                // position of area computed using "Add trigger" button position
                _boxPos = new Rect(rctOff.left,
                                lastRect.y+lastRect.height+rctOff.top,
                                _windowConfRect.width-rctOff.horizontal,
                                _windowConfRect.height-(lastRect.y+lastRect.height+rctOff.vertical));
            }
            GUILayout.BeginArea(_boxPos, GUI.skin.GetStyle("Box"));
            _scrollConfVector = GUILayout.BeginScrollView(_scrollConfVector);
            foreach (KeyValuePair<string, AbsTimer> entry in _vesselTriggers.Timers)
            {
                GUILayout.BeginHorizontal();
                // Name
                GUILayout.Label(entry.Key, GUILayout.Width(labelWidth));
                GUILayout.FlexibleSpace();
                // Init value
                if (entry.Value is Countdown)
                {
                    GUILayout.Label("Start at ", GUILayout.Width(init2Width)); // TODO
                    GUI.SetNextControlName("tfInit"+entry.Key);
                    ((Countdown) entry.Value).InitStrDate = GUILayout.TextField(((Countdown) entry.Value).InitStrDate, GUILayout.Width(initWidth));
                    if (Event.current.isKey && 
                        ((Event.current.keyCode == KeyCode.Return) || (Event.current.keyCode == KeyCode.KeypadEnter)) &&
                        (GUI.GetNameOfFocusedControl() == "tfInit"+entry.Key))
                    {
                        ((Countdown) entry.Value).SetInitDate();
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    GUILayout.Label("         ", GUILayout.Width(init2Width));
                    GUILayout.Label("   ", GUILayout.Width(initWidth));
                }
                GUILayout.FlexibleSpace();
                // Displayed
                entry.Value.Displayed = GUILayout.Toggle(entry.Value.Displayed, "Displayed");
                GUILayout.FlexibleSpace();
                // Remove
                if (GUILayout.Button("Remove"))
                {
                    removed.Add(entry.Key);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            
            foreach (string key in removed)
            {
                AbsTimer instance;
                if (_vesselTriggers.Timers.TryGetValue(key, out instance))
                {
                    instance.Removed = true;
                    _vesselTriggers.Timers.Remove(key);
                }
            }
        }

        public void DoTimerWindow(int windowID)
        {
            float labelWidth = GUI.skin.label.CalcSize(new GUIContent("XXXXXXXX")).x;
            float buttonWidth = GUI.skin.button.CalcSize(new GUIContent("XXXXXX")).x;
            
            GUILayout.BeginVertical();
            _scrollDispVector = GUILayout.BeginScrollView(_scrollDispVector);
            foreach (KeyValuePair<string, AbsTimer> entry in _vesselTriggers.Timers)
            {
                if (entry.Value.Displayed)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    // Name
                    GUILayout.Label(entry.Key+": ", GUILayout.Width(labelWidth));
                    GUILayout.FlexibleSpace();
                    // Value
                    GUILayout.Label(entry.Value.DisplayedValue());
                    GUILayout.FlexibleSpace();
                    // Change state
                    string actionLabel = "";
                    switch (entry.Value.State)
                    {
                        case TimerState.Pending:
                            actionLabel = "Start";
                            break;
                        case TimerState.Running:
                            actionLabel = "Stop";
                            break;
                        case TimerState.Stopped:
                            actionLabel = "Reset";
                            break;
                    }
                    if (GUILayout.Button(actionLabel, GUILayout.Width(buttonWidth)))
                    {
                        entry.Value.ChangeState();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
