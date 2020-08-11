using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public abstract class ModalDialog
    {
        private static Vector2 MIN_SIZE = new Vector2(120.0f, 80.0f);
        
        protected string _message = "";
        
        protected ModalDialog(string message)
        {
            _message = message;
        }
        
        protected void Hide()
        {
            _INSTANCE = null;
        }
        
        protected virtual void _display() {}
        
        public abstract void DoWindow(int windowID);

        private static ModalDialog _INSTANCE = null;
        private static Rect _DIALOG_RECT = Rect.zero;
        
        public delegate void OnValidVoid();
        public delegate void OnValidString(string selected);
        public delegate void OnValidPart(Part selected);
        
        public static void ModalQuestion(string message, OnValidVoid del)
        {
            _INSTANCE = new ModalQuestion(message, del);
            _DIALOG_RECT = CenterRect();
        }
        
        public static void ModalQuestion(Rect parentRect, string message, OnValidVoid del)
        {
            _INSTANCE = new ModalQuestion(message, del);
            _DIALOG_RECT = ComputeRect(parentRect);
        }
        
        public static void ModalInput(string message, OnValidString del, string defaultInput="")
        {
            _INSTANCE = new ModalInput(message, del, defaultInput);
            _DIALOG_RECT = CenterRect();
        }
        
        public static void ModalInput(Rect parentRect, string message, OnValidString del, string defaultInput="")
        {
            _INSTANCE = new ModalInput(message, del, defaultInput);
            _DIALOG_RECT = ComputeRect(parentRect);
        }
        
        public static void ModalCombo(string message, OnValidString del, string[] entries, int selected=-1)
        {
            _INSTANCE = new ModalCombo(message, del, entries, selected);
            _DIALOG_RECT = CenterRect();
        }
        
        public static void ModalCombo(Rect parentRect, string message, OnValidString del, string[] entries, int selected=-1)
        {
            _INSTANCE = new ModalCombo(message, del, entries, selected);
            _DIALOG_RECT = ComputeRect(parentRect);
        }
        
        public static void ModalPartSelector(string message, OnValidPart del, Part defaultPart=null)
        {
            _INSTANCE = new ModalPartSelector(message, del, defaultPart);
            _DIALOG_RECT = CenterRect();
        }
        
        public static void ModalPartSelector(Rect parentRect, string message, OnValidPart del, Part defaultPart=null)
        {
            _INSTANCE = new ModalPartSelector(message, del, defaultPart);
            _DIALOG_RECT = ComputeRect(parentRect);
        }
        
        private static Rect CenterRect()
        {
            Vector2 size = MIN_SIZE;
            Vector2 pos = new Vector2(Screen.width,Screen.height)/2-size/2;
            
            return new Rect(pos, size);
        }
        
        private static Rect ComputeRect(Rect parentRect)
        {
            Vector2 size = Vector2.Max(parentRect.size/2, MIN_SIZE);
            Vector2 pos = parentRect.position+parentRect.size/2-size/2;
            if (pos.x <= 0.0f) pos.x = 0.0f;
            if (pos.y <= 0.0f) pos.y = 0.0f;
            if (pos.x >= Screen.width - size.x) pos.x = Screen.width - size.x;
            if (pos.y >= Screen.height - size.y) pos.y = Screen.height - size.y;
            
            return new Rect(pos, size);
        }
        
        public static void Display()
        {
            if (_INSTANCE != null)
            {
                GUI.Window(Utils.MODAL_DIAL_WINDOW_ID, _DIALOG_RECT, _INSTANCE.DoWindow, _INSTANCE._message);
                _INSTANCE._display();
            }
        }
        
        public static bool IsDisplayed()
        {
            return _INSTANCE != null;
        }
    }
    
    public class ModalQuestion : ModalDialog
    {
        private OnValidVoid _del = null;
        
        public ModalQuestion(string message, OnValidVoid del) : base(message)
        {
            _del = del;
        }
        
        public override void DoWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK"))
            {
                if (_del != null)
                {
                    _del.Invoke();
                }
                Hide();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
    
    public class ModalInput : ModalDialog
    {
        private OnValidString _del = null;
        private string _input = "";
        
        public ModalInput(string message, OnValidString del, string defaultInput="") : base(message)
        {
            _del = del;
            _input = defaultInput;
        }
        
        public override void DoWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            _input = GUILayout.TextField(_input);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK"))
            {
                if (_del != null)
                {
                    _del.Invoke(_input);
                }
                Hide();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
    
    public class ModalCombo : ModalDialog
    {
        private static string MODAL_POPUP_ID = "popupModal";

        private OnValidString _del = null;
        private string[] _entries;
        private int _selected;
        private PopupUI _popupUI;
        
        public ModalCombo(string message, OnValidString del, string[] entries, int selected=-1) : base(message)
        {
            _del = del;
            _entries = entries;
            _selected = selected;
            _popupUI = new PopupUI(Utils.MODAL_DIAL_WINDOW_ID + Utils.WINDOW_ID_POP_OFFSET);
        }
        
        protected override void _display()
        {
            _popupUI.Display();
        }
        
        public override void DoWindow(int windowID)
        {
            if (Event.current.isMouse && (Event.current.button == 0) && (Event.current.type == EventType.MouseUp))
            {
                _popupUI.CloseAll();
            }
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _selected = _popupUI.GUILayoutPopup(MODAL_POPUP_ID, _entries, _selected);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK"))
            {
                if (_del != null)
                {
                    if ((_entries != null) && (_selected >= 0) && (_selected < _entries.Length))
                    {
                        _del.Invoke(_entries[_selected]);
                    }
                }
                Hide();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
    
    public class ModalPartSelector : ModalDialog
    {
        private OnValidPart _del = null;
        private Part _part = null;
        private List<Part> _partList;
        
        public ModalPartSelector(string message, OnValidPart del, Part defaultPart=null) : base(message)
        {
            _del = del;
            _part = defaultPart;
            _partList = new List<Part>();
            foreach (Part part in FlightGlobals.ActiveVessel.parts)
            {
                part.AddOnMouseDown(MouseDownHandler);
                _partList.Add(part);
            }
        }
        
        public override void DoWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label((_part != null) ? _part.ToString() : "Select");
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK"))
            {
                if (_del != null)
                {
                    _del.Invoke(_part);
                }
                _hide();
                Hide();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                _hide();
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        
        private void MouseDownHandler(Part part)
        {
            if (part != null)
            {
                if (part.HighlightActive)
                {
                    // when it work
                    _part = part;
                }
                else
                {
                    // Some part dont handle click. parent part is handled instead
                    foreach (Part child in part.children)
                    {
                        if (child.HighlightActive)
                        {
                            part = child;
                            break;
                        }
                    }
                }
            }
        }
        
        private void _hide()
        {
            foreach (Part part in _partList)
            {
                if (part != null)
                {
                    part.RemoveOnMouseDown(MouseDownHandler);
                }
            }
            _partList.Clear();
        }
    }
}
