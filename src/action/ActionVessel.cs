using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KspTrigger
{
    public class TriggerActionFlight : TriggerAction
    {
        // Index of the action
        private int _actionIndex = -1;
        // Action
        private MethodWithComplement _methodInfo = null;
        private FieldInfo _fieldInfo = null;
        // Object that perform the action
        private object _actionSource = null;
        private TypedDataArray _methodParameter = null;
        
        // Persistent data
        [Persistent(name="actionName")]
        private string _actionNamePers = "";
        [Persistent(name="methodParameter")]
        private string[] _methodParameterPers;
        
        // Action list that can be performed
        public static readonly string[] ActionList;
        
        // TODO Vessel.SetReferenceTransform(Part p)
        // VesselAutopilot.VesselSAS.SetTargetOrientation (Vector3 tgtOrientation, bool reset)
        private static readonly Dictionary<string,MethodWithComplement> _actionMethods = new Dictionary<string,MethodWithComplement>()
        {
            {"NextStage",       new MethodWithComplement(typeof(KSP.UI.Screens.StageManager), "ActivateNextStage", 0)},
            {"ActiveStage",     new MethodWithComplement(typeof(KSP.UI.Screens.StageManager), "ActivateStage", 1)},
            {"SASMode",         new MethodWithComplement(typeof(VesselAutopilot), "SetMode", 1)},
            {"CustomGroup",     new MethodWithComplement(typeof(VesselWrapper), "CustomGroup", 1)},
            {"RCS",             new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.RCS})},
            {"SAS",             new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.SAS})},
            {"Brakes",          new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.Brakes})},
            {"Gear",            new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.Gear})},
            {"Light",           new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.Light})},
            {"Abort",           new MethodWithComplement(typeof(ActionGroupList), "SetGroup", 2, new object[] {KSPActionGroup.Abort, true})},
            {"Throttle",        new MethodWithComplement(typeof(Autopilot), "Throttle", 1)},
            {"Rotate",          new MethodWithComplement(typeof(Autopilot), "Rotate", 4)},
            {"Translate",       new MethodWithComplement(typeof(Autopilot), "Translate", 4)},
            //{"SetOrientation",  new MethodWithComplement(typeof(VesselWrapper), "SetOrientation", 3)}
        };
        
        private static readonly Dictionary<string,FieldInfo> _actionFields = new Dictionary<string,FieldInfo>()
        {
        };
        
        static TriggerActionFlight()
        {
            // Compute action list that can be evaluated on this part
            List<string> actions = new List<string>();
            foreach (KeyValuePair<string,MethodWithComplement> entry in _actionMethods)
            {
                if (entry.Value.IsValid)
                {
                    actions.Add(entry.Key);
                }
            }
            foreach (KeyValuePair<string,FieldInfo> entry in _actionFields)
            {
                if (entry.Value != null)
                {
                    actions.Add(entry.Key);
                }
            }
            ActionList = actions.ToArray();
        }
        
        public TriggerActionFlight(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerActionType.Flight;
            _actionIndex = -1;
            _methodInfo = null;
            _fieldInfo = null;
            _actionSource = null;
            _methodParameter = null;
        }
        
        public TriggerActionFlight(TriggerActionFlight other) : base(other)
        {
            _type = TriggerActionType.Flight;
            // Automatic call of ActionIndex_set
            ActionIndex = other._actionIndex;
            if (_methodParameter != null)
            {
                for (int i = 0; i < _methodParameter.Length; i++)
                {
                    _methodParameter[i].ValueStr = other._methodParameter[i].ValueStr;
                }
                _methodParameter.Acquit();
            }
            UpdatePersistentData();
            _modified = false;
        }
        
        public override void LoadPersistentData()
        {
            // Action name
            if (_actionFields.ContainsKey(_actionNamePers) || _actionMethods.ContainsKey(_actionNamePers))
            {
                // Name valid => to index
                ActionIndex = ActionList.IndexOf(_actionNamePers);
                
                // Parameters
                if (_methodParameter != null)
                {
                    for (int i = 0; (i < _methodParameter.Length) && (i < _methodParameterPers.Length); i++)
                    {
                        _methodParameter[i].ValueStr = _methodParameterPers[i];
                    }
                }
            }
        }
        
        public override void UpdatePersistentData()
        {
            _actionNamePers = null;
            _methodParameterPers = null;
            
            // Save action name, not index
            if (_actionIndex < ActionList.Length)
            {
                _actionNamePers = ActionList[_actionIndex];
            }
            // Parameters
            if (_methodParameter != null)
            {
                _methodParameterPers = new string[_methodParameter.Length];
                for (int i = 0; i < _methodParameter.Length; i++)
                {
                    _methodParameterPers[i] = _methodParameter[i].ValueStr;
                }
            }
        }
        
        public int ActionIndex
        {
            set
            {
                // Action has changed
                if (value != _actionIndex)
                {
                    _modified = true;
                    _actionIndex = -1;
                    _methodInfo = null;
                    _fieldInfo = null;
                    _actionSource = null;
                    _methodParameter = null;
                    // Action is valid for this part
                    if ((value >= 0) && (value < ActionList.Length))
                    {
                        if (_actionMethods.ContainsKey(ActionList[value]))
                        {
                            _actionIndex = value;
                            _methodInfo = _actionMethods[ActionList[_actionIndex]];
                            _methodParameter = new TypedDataArray(_methodInfo.MethodInfo.GetParameters(), _methodInfo.Complement);
                        }
                        else if (_actionFields.ContainsKey(ActionList[value]))
                        {
                            _actionIndex = value;
                            _fieldInfo = _actionFields[ActionList[_actionIndex]];
                            _methodParameter = new TypedDataArray(ActionList[value], _fieldInfo.FieldType);
                        }
                        ResolveSource();
                    }
                }
            }
            
            get { return _actionIndex; }
        }
        
        private void ResolveSource()
        {
            Type declaringType = null;
            if (_fieldInfo != null)
            {
                declaringType = _fieldInfo.DeclaringType;
            }
            else if (_methodInfo != null)
            {
                declaringType = _methodInfo.MethodInfo.DeclaringType;
            }
            if (declaringType == typeof(KSP.UI.Screens.StageManager))
            {
                _actionSource = null; // Static method
            }
            else if (declaringType == typeof(ActionGroupList))
            {
                _actionSource = _vesselTriggers.Vessel.ActionGroups;
            }
            else if (declaringType == typeof(VesselAutopilot))
            {
                _actionSource = _vesselTriggers.Vessel.Autopilot;
            }
            else if (declaringType == typeof(Autopilot))
            {
                _actionSource = _vesselTriggers.Autopilot;
            }
            else if (declaringType == typeof(VesselWrapper))
            {
                _actionSource = typeof(VesselWrapper).GetConstructor(new Type[] {typeof(Vessel)}).Invoke(new object[] {_vesselTriggers.Vessel});
            }
        }
        
        public TypedDataArray Parameters { get { return _methodParameter; } }
        
        public override bool Modified { get { return _modified || ((_methodParameter != null) && _methodParameter.Modified); } }
        public override void Acquit() { _modified = false; _methodParameter.Acquit(); }
        
        public bool ActionValid
        {
            get { return ((_methodInfo != null) || (_fieldInfo != null)); }
        }
        
        public bool ParameterValid
        {
            get { return ((_methodParameter != null) && _methodParameter.IsValid); }
        }
        
        public override bool IsValid()
        {
            return ActionValid && ParameterValid;
        }
        
        public override void DoAction()
        {
            if (IsValid())
            {
                if (_methodInfo != null)
                {
                    _methodInfo.MethodInfo.Invoke(_actionSource, _methodParameter.Array);
                }
                else if (_fieldInfo != null)
                {
                    _fieldInfo.SetValue(_actionSource, _methodParameter.Array[0]);
                }
            }
        }

        public override string ToString()
        {
            if (IsValid())
            {
                return "Flight action: " + ActionList[_actionIndex];
            }
            else
            {
                return "Flight action: invalid";
            }
        }
    }
}

