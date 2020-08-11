using System;
using UnityEngine;

namespace KspTrigger
{
    public abstract class AbstractUI
    {
        protected VesselTriggers _vesselTriggers = null;
        public VesselTriggers VesselTriggers { set { _vesselTriggers = value; } }

        protected Rect _windowRect = Rect.zero;
        public Vector2 Position { get { return _windowRect.position; } }
        public abstract Vector2 Size { get; }
        
        protected abstract int _windowID { get; }
        protected abstract string _windowTitle { get; }

        protected PopupUI _popupUI;
        
        public AbstractUI(Vector2 pos)
        {
            _windowRect = new Rect(pos, Size);
            _popupUI = new PopupUI(_windowID + Utils.WINDOW_ID_POP_OFFSET);
        }
        
        public void Display()
        {
            if (_isDisplayed())
            {
                _windowRect.size = Size;
                _windowRect = GUI.Window(_windowID, _windowRect, DoWindow, _windowTitle);
                _popupUI.Display();
            }
        }
        
        protected abstract bool _isDisplayed();
        
        private void DoWindow(int windowID)
        {
            Utils.InitGui();
            
            if (ModalDialog.IsDisplayed() && (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout))
            {
                Event.current.Use();
                GUI.BringWindowToFront(Utils.MODAL_DIAL_WINDOW_ID);
                _popupUI.CloseAll();
            }
            
            if (Event.current.isMouse && (Event.current.button == 0) && (Event.current.type == EventType.MouseUp))
            {
                _popupUI.CloseAll();
            }
            
            _doWindow();
            
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            
            // Tooltip
            if (!ModalDialog.IsDisplayed() && (GUI.tooltip != "") && (Event.current.type == EventType.Repaint))
            {
                GUIContent content = new GUIContent(GUI.tooltip);
                GUI.Label(new Rect(Event.current.mousePosition, GUI.skin.box.CalcSize(content)), content);
            }
        }
        
        protected abstract void _doWindow();
    }
}

