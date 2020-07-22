using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KspTrigger
{
    public static class Utils
    {
        public const string DEBUG_PREFIX = "[KAT] ";
        
        private static bool _STATIC_GUI_INIT_DONE = false;
        private static GUIStyle _BUTTON_STYLE_VALID = null;
        public static GUIStyle BUTTON_STYLE_VALID { get {return _BUTTON_STYLE_VALID;} }
        private static GUIStyle _BUTTON_STYLE_INVALID = null;
        public static GUIStyle BUTTON_STYLE_INVALID { get {return _BUTTON_STYLE_INVALID;} }
        private static GUIStyle _BUTTON_STYLE_PENDING = null;
        public static GUIStyle BUTTON_STYLE_PENDING { get {return _BUTTON_STYLE_PENDING;} }
        private static GUIStyle _TF_STYLE_VALID = null;
        public static GUIStyle TF_STYLE_VALID { get {return _TF_STYLE_VALID;} }
        private static GUIStyle _TF_STYLE_INVALID = null;
        public static GUIStyle TF_STYLE_INVALID { get {return _TF_STYLE_INVALID;} }
        
        public const int MAIN_WINDOW_ID          = 36541;
        public const int EVENT_WINDOW_ID         = MAIN_WINDOW_ID + 1;
        public const int CONDITION_WINDOW_ID     = MAIN_WINDOW_ID + 2;
        public const int ACTION_WINDOW_ID        = MAIN_WINDOW_ID + 3;
        public const int TIMER_CONF_WINDOW_ID    = MAIN_WINDOW_ID + 4;
        public const int TIMER_DISP_WINDOW_ID    = MAIN_WINDOW_ID + 5;
        public const int MAIN_WINDOW_ID_POP      = MAIN_WINDOW_ID + 20;
        public const int EVENT_WINDOW_ID_POP     = EVENT_WINDOW_ID + 20;
        public const int CONDITION_WINDOW_ID_POP = CONDITION_WINDOW_ID + 20;
        public const int ACTION_WINDOW_ID_POP    = ACTION_WINDOW_ID + 20;
        
        public static void InitGui()
        {
            if (_STATIC_GUI_INIT_DONE) return;
            
            _STATIC_GUI_INIT_DONE = true;
            _BUTTON_STYLE_VALID = new GUIStyle(GUI.skin.button);
            _BUTTON_STYLE_VALID.normal.textColor = Color.green;
            _BUTTON_STYLE_INVALID = new GUIStyle(GUI.skin.button);
            _BUTTON_STYLE_INVALID.normal.textColor = Color.red;
            _BUTTON_STYLE_PENDING = new GUIStyle(GUI.skin.button);
            _BUTTON_STYLE_PENDING.normal.textColor = Color.blue;
            _BUTTON_STYLE_PENDING.hover.textColor = Color.blue;
            _TF_STYLE_VALID = new GUIStyle(GUI.skin.textField);
            _TF_STYLE_INVALID = new GUIStyle(GUI.skin.textField);
            _TF_STYLE_INVALID.normal.textColor = Color.red;
            _TF_STYLE_INVALID.hover.textColor = Color.red;
            _TF_STYLE_INVALID.focused.textColor = Color.red;
        }
    }
    
    // TODO move
    public class MethodWithComplement
    {
        private MethodInfo _methodInfo;
        public MethodInfo MethodInfo { get { return _methodInfo; } }
        private object[] _complement;
        public object[] Complement { get { return _complement; } }
        
        public MethodWithComplement(Type declaringType, string methodeName, int nbParam) : this(declaringType, methodeName, nbParam, null) {}
        
        public MethodWithComplement(Type declaringType, string methodeName, int nbParam, object[] complement)
        {
            _methodInfo = declaringType.GetMethods().Where(x => x.Name == methodeName).FirstOrDefault(x => (x.GetParameters().Length == nbParam));
            _complement = complement;
        }
        
        public MethodWithComplement(MethodInfo methodInfo) : this(methodInfo, null) {}
        
        public MethodWithComplement(MethodInfo methodInfo, object[] complement)
        {
            _methodInfo = methodInfo;
            _complement = complement;
        }
        
        public bool IsValid { get { return _methodInfo != null; } }
    }
    
    public enum ComparatorType
    {
        Equals,
        NotEquals,
        Less,
        LessOrEquals,
        More,
        MoreOrEquals
    }
    
    public class TypedData
    {
        private string _name;
        private Type _type;
        private Dictionary<String,object> _valueDict;
        private bool _comparable;
        private object _value;
        private string _valueStr;
        private bool _modified;
        private object _defaultValue;
        
        public TypedData(string name, Type type)
        {
            _name = name;
            _type = type;
            _valueDict = null;
            _comparable = (!_type.IsEnum && (_type != typeof(bool)) && (_type != typeof(string)) && typeof(IComparable).IsAssignableFrom(_type));
            _value = null;
            _valueStr = "";
            _modified = false;
            _defaultValue = null;
        }
        
        public TypedData(string name, Type type, object option) : this(name, type)
        {
            if (option != null)
            {
                Type optionType = option.GetType();
                if (optionType.IsGenericType && (optionType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (optionType.GetGenericArguments()[0] == typeof(string)))
                {
                    _valueDict = new Dictionary<string,object>();
                    try {
                        foreach (KeyValuePair<string,object> entry in (Dictionary<string,object>) option)
                        {
                            _valueDict.Add(entry.Key, Convert.ChangeType(entry.Value, _type));
                        }
                    } catch (Exception) {}
                }
                else if (option.GetType().IsAssignableFrom(_type))
                {
                    _defaultValue = option;
                    _value = option;
                    _valueStr = _value.ToString();
                }
            }
        }
        
        public string ValueStr
        {
            set
            {
                // Value has changed
                if ((value != _valueStr) && Configurable)
                {
                    _modified = true;
                    _valueStr = value;
                    _value = null;
                    try
                    {
                        if (_valueDict != null)
                        {
                            _value = _valueDict[_valueStr];
                        }
                        else if (_type != null)
                        {
                            if (_type.IsEnum)
                            {
                                _value = Enum.Parse(_type, _valueStr);
                            }
                            else
                            {
                                _value = Convert.ChangeType(_valueStr, _type);
                            }
                        }
                    } catch (Exception) {}

                }
            }
            
            get { return _valueStr; }
        }
        
        public string Name { get { return _name; } }
        public object Value { get { return _value; } }
        public bool Comparable { get { return _comparable; } }
        public bool IsValid { get { return _value != null; } }
        public bool Configurable { get { return _defaultValue == null; } }
        public bool Modified { get { return _modified; } }
        
        public string[] ComparatorList
        {
            get
            {
                return _comparable ? Enum.GetNames(typeof(ComparatorType)) : Enum.GetNames(typeof(ComparatorType)).Take(2).ToArray();
            }
        }
        
        public bool ComparatorValid(ComparatorType ope)
        {
            return _comparable || (ope == ComparatorType.Equals) || (ope == ComparatorType.NotEquals);
        }
        
        public bool CompareTo(object other, ComparatorType ope)
        {
            switch (ope)
            {
                case ComparatorType.Equals:
                    return _value.Equals(other);
                case ComparatorType.NotEquals:
                    return !_value.Equals(other);
                case ComparatorType.Less:
                    return _comparable ? ((_value as IComparable).CompareTo(other) < 0) : false;
                case ComparatorType.LessOrEquals:
                    return _comparable ? ((_value as IComparable).CompareTo(other) <= 0) : false;
                case ComparatorType.More:
                    return _comparable ? ((_value as IComparable).CompareTo(other) > 0) : false;
                case ComparatorType.MoreOrEquals:
                    return _comparable ? ((_value as IComparable).CompareTo(other) >= 0) : false;
                default:
                    return false;
            }
        }
        
        public bool CompareFrom(object other, ComparatorType ope)
        {
            switch (ope)
            {
                case ComparatorType.Equals:
                    return _value.Equals(other);
                case ComparatorType.NotEquals:
                    return !_value.Equals(other);
                case ComparatorType.Less:
                    return _comparable ? ((_value as IComparable).CompareTo(other) > 0) : false;
                case ComparatorType.LessOrEquals:
                    return _comparable ? ((_value as IComparable).CompareTo(other) >= 0) : false;
                case ComparatorType.More:
                    return _comparable ? ((_value as IComparable).CompareTo(other) < 0) : false;
                case ComparatorType.MoreOrEquals:
                    return _comparable ? ((_value as IComparable).CompareTo(other) <= 0) : false;
                default:
                    return false;
            }
        }
        
        public void Acquit()
        {
            _modified = false;
        }
        
        public void DisplayLayout(PopupUI popup = null)
        {
            if (_valueDict != null)
            {
                if (popup != null)
                {
                    string[] _entry = _valueDict.Keys.ToArray();
                    int index = Array.IndexOf(_entry, ValueStr);
                    int newVal = popup.GUILayoutPopup(_name+"popup", _entry, index);
                    if ((newVal >= 0) && (newVal < _entry.Length))
                    {
                        ValueStr = _entry[newVal];
                    }
                }
                else
                {
                    ValueStr = GUILayout.TextField(ValueStr, IsValid ? Utils.TF_STYLE_VALID : Utils.TF_STYLE_INVALID);
                }
            }
            else if (_type != null)
            {
                if (_type.IsEnum && (popup != null))
                {
                    int index = (_value != null) ? (int) _value : -1;
                    int newVal = popup.GUILayoutPopup(_name+"popup", Enum.GetNames(_type), index);
                    ValueStr = newVal.ToString();
                }
                else if (_type == typeof(bool))
                {
                    // in case of _value == null
                    bool val = (_value != null) && ((bool)_value);
                    bool newVal = GUILayout.Toggle(val, val ? "True" : "False");
                    ValueStr = newVal.ToString();
                }
                else
                {
                    ValueStr = GUILayout.TextField(ValueStr, IsValid ? Utils.TF_STYLE_VALID : Utils.TF_STYLE_INVALID);
                }
            }
        }
        
        public override string ToString()
        {
            return ValueStr;
        }
    }
    
    public class TypedDataArray
    {
        private TypedData[] _data;
        private bool _configurable = false;
        
        public TypedDataArray(ParameterInfo[] paramInfo, object[] defaultValue)
        {
            _data = new TypedData[paramInfo.Length];
            _configurable = false;
            int nbDefault = (defaultValue != null) ? defaultValue.Length : 0;
            for (int i = 0; i < paramInfo.Length; i++)
            {
                _data[i] = new TypedData(paramInfo[i].Name, paramInfo[i].ParameterType, (i < nbDefault) ? defaultValue[i] : null);
                _configurable = _configurable || _data[i].Configurable;
            }
        }
        
        public TypedDataArray(string name, Type type)
        {
            _data = new TypedData[1];
            _data[0] =  new TypedData(name, type);
            _configurable = true;
        }
                
        public int Length { get { return _data.Length; } }
        public bool IsConfigurable { get { return _configurable; } }
        
        public TypedData this[int i]
        {
            get { return ((i >= 0) && (i < Length)) ? _data[i] : null; }
        }

        public bool IsValid
        {
            get
            {
                bool result = true;
                for (int i = 0; i < Length; i++)
                {
                    result &= _data[i].IsValid;
                }
                return result;
            }
        }
        
        public bool Modified
        {
            get
            {
                bool result = false;
                for (int i = 0; i < Length; i++)
                {
                    result |= _data[i].Modified;
                }
                return result;
            }
        }
        
        public void Acquit()
        {
            for (int i = 0; i < Length; i++)
            {
                _data[i].Acquit();
            }
        }
        
        public object[] Array
        {
            get
            {
                object[] result = new object[Length];
                for (int i = 0; i < Length; i++)
                {
                    result[i] = _data[i].Value;
                }
                return result;
            }
        }
    }
    
    public class PartSelector
    {
        public delegate void OnSelect(Part part);
        
        private OnSelect _del;
        private bool _selecting;
        private List<Part> _partList;
        
        public PartSelector(OnSelect del)
        {
            _del = del;
            _selecting = false;
            _partList = new List<Part>();
        }
        
        private bool Selecting
        {
            set
            {
                if (_selecting != value)
                {
                    _selecting = value;
                    if (_selecting)
                    {
                        _partList.Clear();
                        foreach (Part part in FlightGlobals.ActiveVessel.parts)
                        {
                            part.AddOnMouseDown(MouseDownHandler);
                            _partList.Add(part);
                        }
                    }
                    else
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
            
            get { return _selecting; }
        }
        
        private void MouseDownHandler(Part part)
        {
            if ((_del != null) && (part != null))
            {
                if (part.HighlightActive)
                {
                    // when it work
                    _del.Invoke(part);
                }
                else
                {
                    // Some part dont handle click. parent part is handled instead
                    foreach (Part child in part.children)
                    {
                        if (child.HighlightActive)
                        {
                            _del.Invoke(child);
                            break;
                        }
                    }
                }
                Selecting = false;
            }
        }
        
        public void CancelSelect()
        {
            Selecting = false;
        }
        
        public void DisplayLayout(Part selected)
        {
            string buttonLabel = (selected != null) ? selected.ToString() : "Select";
            GUIStyle style = _selecting ? Utils.BUTTON_STYLE_PENDING : Utils.BUTTON_STYLE_VALID;
            string tooltip = "SelectPart";
            if (GUILayout.Button(new GUIContent(buttonLabel, tooltip), style))
            {
                Selecting = !Selecting;
            }
            if ((selected != null) && !_selecting)
            {
                selected.Highlight(selected.HighlightActive || (GUI.tooltip == tooltip));
            }
        }
    }
    
    public class ModaleInput
    {
        public delegate void OnValid(string input);
        
        private string _message = null;
        private OnValid _del = null;
        private string _input = "";
        
        public void Show(string message, OnValid del, string defaultInput="")
        {
            _message = message;
            _del = del;
            _input = defaultInput;
        }
        
        public void Hide()
        {
            _message = null;
        }
        
        public void Display(int windowID, Rect parentRect)
        {
            if (_message == null)
            {
                return;
            }

            Vector2 size = parentRect.size/2;
            Vector2 pos = parentRect.position+parentRect.size/2-size/2;
            if (pos.x <= 0.0) pos.x = 0;
            if (pos.y <= 0.0) pos.y = 0;
            if (pos.x >= Screen.width - size.x) pos.x = Screen.width - size.x;
            if (pos.y >= Screen.height - size.y) pos.y = Screen.height - size.y;
            
            GUI.ModalWindow(windowID, new Rect(pos, size), DoWindow, _message);
        }
        
        public void DoWindow(int windowID)
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
}
