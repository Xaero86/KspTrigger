using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class PopupUI
    {
        private const int NB_ITEM_POPUP = 8;
        private const float DEFAULT_WIDTH = 120;
        
        private int _windowID;
        // id of popup if displayed
        private string _uniqueId;
        // entries in popup if displayed
        private string[] _entries;
        // selected index of popup if displayed
        private int _selected;
        // size of popup if displayed
        private Rect _popupTotalRect;
        private Rect _popupDisplayRect;
        private Vector2 _scrollVectPopup;
        // new index when selected
        private int _newSelection;
        // delay close popup
        private string _popupToClose;
        
        public PopupUI(int windowID)
        {
            _windowID = windowID;
            _uniqueId = null;
            _entries = null;
            _selected = -1;
            _popupTotalRect = Rect.zero;
            _popupDisplayRect = Rect.zero;
            _scrollVectPopup = Vector2.zero;
            _newSelection = -1;
            _popupToClose = null;
        }
        
        public void Display()
        {
            if (_uniqueId != null)
            {
                if (_uniqueId == _popupToClose)
                {
                    Close();
                    return;
                }

                GUI.Window(_windowID, _popupDisplayRect, DoPopup, "");
            }
        }
        
        private void Close()
        {
            _uniqueId = null;
            _entries = null;
            _selected = -1;
            _popupTotalRect = Rect.zero;
            _popupDisplayRect = Rect.zero;
            _scrollVectPopup = Vector2.zero;
            _newSelection = -1;
            _popupToClose = null;
        }

        // Close when ready
        public void CloseAll()
        {
            _popupToClose = _uniqueId;
        }
        
        public bool PopupOpened(string uniqueId)
        {
            return (uniqueId == _uniqueId);
        }
        
        private void DoPopup(int windowID)
        {
            if (_uniqueId == null)
            {
                return;
            }
            GUI.BringWindowToFront(windowID);
            _scrollVectPopup = GUI.BeginScrollView(new Rect(Vector2.zero,_popupDisplayRect.size), _scrollVectPopup, new Rect(Vector2.zero,_popupTotalRect.size), GUIStyle.none, GUI.skin.verticalScrollbar);
            int newSel = GUI.SelectionGrid(new Rect(Vector2.zero,_popupTotalRect.size), _selected, _entries, 1);
            GUI.EndScrollView();
            if (GUI.changed && (newSel >= 0) && (newSel != _selected))
            {
                _newSelection = newSel;
            }
        }
        
        // Display de button of the combo. Prepare the popup
        public int GUILayoutPopup(string uniqueId, string[] entries, int selected, float popupWidth = DEFAULT_WIDTH)
        {
            if (uniqueId == null)
            {
                return -1;
            }
            
            string popupLabel = "Select";
            if ((entries != null) && (selected >= 0) && (selected < entries.Length))
            {
                popupLabel = entries[selected];
            }
            
            if (GUILayout.Button(popupLabel, GUILayout.Width(popupWidth)) && (entries != null))
            {
                // Combo button clicked
                if (_uniqueId != uniqueId)
                {
                    // Popup not already displayed => show
                    _uniqueId = uniqueId;
                    _entries = new string[entries.Length];
                    entries.CopyTo(_entries, 0);
                    _selected = selected;
                    _newSelection = -1;
                    _popupToClose = null;
                }
                else
                {
                    // Popup already display => hide
                    Close();
                }
            }
            else if (_popupToClose == uniqueId)
            {
                Close();
            }
            // Current combo has to be displayed
            if ((Event.current.type == EventType.Repaint) && (_uniqueId == uniqueId))
            {
                // calculate popup size
                float popupTotalHeight = 0.0f;            
                float popupHeight = 0.0f;            
                if (_entries != null)
                {
                    int nbItem = 0;
                    foreach (string entry in _entries)
                    {
                        float buttonHeight = GUI.skin.button.CalcSize(new GUIContent(entry)).y;
                        popupTotalHeight += buttonHeight;
                        if (nbItem < NB_ITEM_POPUP)
                        {
                            popupHeight += buttonHeight;
                        }
                        nbItem++;
                    }
                }
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                Vector2 absPos = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y+buttonRect.height));_popupTotalRect = new Rect(absPos, new Vector2(popupWidth, popupTotalHeight));
                _popupDisplayRect = new Rect(absPos, new Vector2(popupWidth, popupHeight));
                
                // Change value only on repaint
                if (_newSelection >= 0)
                {
                    selected = _newSelection;
                    Close();
                }
            }

            return selected;
        }
    }
}
