using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KspTrigger
{
    public class TriggerActionPart : TriggerAction
    {
        // Part to act on
        private Part _part = null;
        // Index of the action
        private int _actionIndex = -1;
        // Action
        private MethodWithComplement _methodInfo = null;
        private FieldInfo _fieldInfo = null;
        // Object that contain the method/field
        private object _actionSource = null;
        // Parameter for the method
        private TypedDataArray _methodParameter = null;
        // Action list that can be performed on this part
        private string[] _actionList = null;
        public string[] ActionList { get { return _actionList; } }
        
        // Persistent data
        [Persistent(name="partId")]
        private uint _partIdPers = 0;
        [Persistent(name="actionName")]
        private string _actionNamePers = "";
        [Persistent(name="methodParameter")]
        private string[] _methodParameterPers;
        
        // TODO add ModuleScienceExperiment TransferToContainer (param part)
        private static readonly Dictionary<string,MethodWithComplement> _actionMethods = new Dictionary<string,MethodWithComplement>()
        {
            {"Explode",             new MethodWithComplement(typeof(Part), "explode", 0)},
            {"ActivateEngine",      new MethodWithComplement(typeof(ModuleEngines), "Activate", 0)},
            {"ShutdownEngine",      new MethodWithComplement(typeof(ModuleEngines), "Shutdown", 0)},
            {"ToggleMode",          new MethodWithComplement(typeof(MultiModeEngine), "ToggleMode", 0)},
            {"Decouple",            new MethodWithComplement(typeof(ModuleDecouplerBase), "Decouple", 0)},
            {"DecoupleDock",        new MethodWithComplement(typeof(ModuleDockingNode), "Decouple", 0)},
            {"DecoupleGrapple",     new MethodWithComplement(typeof(ModuleGrappleNode), "Decouple", 0)}, //
            {"ReleaseGrapple",      new MethodWithComplement(typeof(ModuleGrappleNode), "Release", 0)}, //
            {"LockPivot",           new MethodWithComplement(typeof(ModuleGrappleNode), "LockPivot", 0)}, //
            {"Extend",              new MethodWithComplement(typeof(ModuleDeployablePart), "Extend", 0)},
            {"Retract",             new MethodWithComplement(typeof(ModuleDeployablePart), "Retract", 0)},
            {"Cut",                 new MethodWithComplement(typeof(ModuleParachute), "CutParachute", 0)},
            {"Deploy",              new MethodWithComplement(typeof(ModuleParachute), "Deploy", 0)},
            {"Disarm",              new MethodWithComplement(typeof(ModuleParachute), "Disarm", 0)},
            {"ActivateRadiator",    new MethodWithComplement(typeof(ModuleActiveRadiator), "Activate", 0)},
            {"ShutdownRadiator",    new MethodWithComplement(typeof(ModuleActiveRadiator), "Shutdown", 0)},
            {"StartTransmission",   new MethodWithComplement(typeof(ModuleDataTransmitter), "StartTransmission", 0)},
            {"StopTransmission",    new MethodWithComplement(typeof(ModuleDataTransmitter), "StopTransmission", 0)},
            {"StartDrain",          new MethodWithComplement(typeof(ModuleResourceDrain), "StartResourceDrainAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"StopDrain",           new MethodWithComplement(typeof(ModuleResourceDrain), "StopResourceDrainAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"ActivateGenerator",   new MethodWithComplement(typeof(ModuleGenerator), "Activate", 0)},
            {"ShutdownGenerator",   new MethodWithComplement(typeof(ModuleGenerator), "Shutdown", 0)},
            {"LightsOn",            new MethodWithComplement(typeof(ModuleLight), "LightsOn", 0)},
            {"LightsOff",           new MethodWithComplement(typeof(ModuleLight), "LightsOff", 0)},
            {"ExtendLadder",        new MethodWithComplement(typeof(RetractableLadder), "Extend", 0)},
            {"RetractLadder",       new MethodWithComplement(typeof(RetractableLadder), "Retract", 0)},
            {"StartConverter",      new MethodWithComplement(typeof(BaseConverter), "StartResourceConverter", 0)},
            {"StopConverter",       new MethodWithComplement(typeof(BaseConverter), "StopResourceConverter", 0)},
            {"Experiment",          new MethodWithComplement(typeof(ModuleScienceExperiment), "DeployExperiment", 0)},
            {"ResetExperiment",     new MethodWithComplement(typeof(ModuleScienceExperiment), "ResetExperiment", 0)},
            {"CollectAll",          new MethodWithComplement(typeof(ModuleScienceContainer), "CollectAllEvent", 0)},
            {"TransmitScience",     new MethodWithComplement(typeof(ModuleScienceLab), "TransmitScience", 0)},
            {"DeployWheel",         new MethodWithComplement(typeof(ModuleWheels.ModuleWheelDeployment).GetMethods().Where(x => x.Name == "ActionToggle").FirstOrDefault(x => (x.GetParameters().Length == 1) && (x.GetParameters()[0].ParameterType == typeof(KSPActionType))), new object[] {KSPActionType.Activate})},
            {"RetractWheel",        new MethodWithComplement(typeof(ModuleWheels.ModuleWheelDeployment).GetMethods().Where(x => x.Name == "ActionToggle").FirstOrDefault(x => (x.GetParameters().Length == 1) && (x.GetParameters()[0].ParameterType == typeof(KSPActionType))), new object[] {KSPActionType.Deactivate})},
            {"ActivateYaw",         new MethodWithComplement(typeof(ModuleGimbal), "ToggleYawAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"DeactivateYaw",       new MethodWithComplement(typeof(ModuleGimbal), "ToggleYawAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"ActivatePitch",       new MethodWithComplement(typeof(ModuleGimbal), "TogglePitchAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"DeactivatePitch",     new MethodWithComplement(typeof(ModuleGimbal), "TogglePitchAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"ActivateRoll",        new MethodWithComplement(typeof(ModuleGimbal), "ToggleRollAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"DeactivateRoll",      new MethodWithComplement(typeof(ModuleGimbal), "ToggleRollAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"LockGimbal",          new MethodWithComplement(typeof(ModuleGimbal), "LockAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"FreeGimbal",          new MethodWithComplement(typeof(ModuleGimbal), "FreeAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"ActivateBrakes",      new MethodWithComplement(typeof(ModuleAeroSurface), "ActionToggleBrakes", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"DeactivateBrakes",    new MethodWithComplement(typeof(ModuleAeroSurface), "ActionToggleBrakes", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"ExtendControl",       new MethodWithComplement(typeof(ModuleControlSurface), "ActionExtend", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"RetractControl",      new MethodWithComplement(typeof(ModuleControlSurface), "ActionRetract", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"ActivateBrake",       new MethodWithComplement(typeof(ModuleWheels.ModuleWheelBrakes), "BrakeAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"DeactivateBrake",     new MethodWithComplement(typeof(ModuleWheels.ModuleWheelBrakes), "BrakeAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"MotorEnable",         new MethodWithComplement(typeof(ModuleWheels.ModuleWheelMotor), "MotorEnable", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"MotorDisable",        new MethodWithComplement(typeof(ModuleWheels.ModuleWheelMotor), "MotorDisable", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"TorqueCtrlMan",       new MethodWithComplement(typeof(ModuleWheels.ModuleWheelMotor), "ActAutoTorqueToggle", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"TorqueCtrlAuto",      new MethodWithComplement(typeof(ModuleWheels.ModuleWheelMotor), "ActAutoTorqueToggle", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"FrictionCtrlMan",     new MethodWithComplement(typeof(ModuleWheelBase), "ActAutoFrictionToggle", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"FrictionCtrlAuto",    new MethodWithComplement(typeof(ModuleWheelBase), "ActAutoFrictionToggle", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate)})},
            {"EnableSuspension",    new MethodWithComplement(typeof(ModuleWheelBase), "EnableSuspension", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})}, //
            {"DisableSuspension",   new MethodWithComplement(typeof(ModuleWheelBase), "DisableSuspension", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})}, //
            {"DeployFairing",       new MethodWithComplement(typeof(ModuleProceduralFairing), "DeployFairingAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})},
            {"Inflate",             new MethodWithComplement(typeof(ModuleJettison), "JettisonAction", 1, new object[] {new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate)})}
        };
        
        // TODO add ModuleWheels.ModuleWheelSteering property
        // TODO add ModuleWheels.ModuleWheelSuspension property
        // TODO add ModuleWheels.ModuleWheelBrakes property
        // TODO add ModuleWheels.ModuleWheelMotor property
        // TODO add ModuleWheelBase property
        private static readonly Dictionary<string,FieldInfo> _actionFields = new Dictionary<string,FieldInfo>()
        {
            {"DeployAngle",         typeof(ModuleAeroSurface).GetField("aeroDeployAngle")},
            {"IgnoreYaw",           typeof(ModuleControlSurface).GetField("ignoreYaw")},
            {"IgnorePitch",         typeof(ModuleControlSurface).GetField("ignorePitch")},
            {"IgnoreRoll",          typeof(ModuleControlSurface).GetField("ignoreRoll")},
            {"DrainRate",           typeof(ModuleResourceDrain).GetField("drainRate")},
            {"WholeVessel",         typeof(ModuleResourceDrain).GetField("flowMode")}
        };
        
        public TriggerActionPart(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerActionType.Part;
            _part = null;
            _actionIndex = -1;
            _methodInfo = null;
            _fieldInfo = null;
            _actionSource = null;
            _methodParameter = null;
            _actionList = null;
        }
        
        public TriggerActionPart(TriggerActionPart other) : base(other)
        {
            _type = TriggerActionType.Part;
            _part = null;
            // Automatic call of Part_set
            ActionPart = other._part;
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
        
        public override void LoadPersistentData(TriggerConfig triggerConfig)
        {
            // Part UID
            ActionPart = _vesselTriggers.Vessel[_partIdPers];
            
            if ((_part != null) && (_actionList != null))
            {
                // Action name
                if (_actionFields.ContainsKey(_actionNamePers) || _actionMethods.ContainsKey(_actionNamePers))
                {
                    // Name valid => to index
                    ActionIndex = _actionList.IndexOf(_actionNamePers);

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
        }
        
        public override void UpdatePersistentData()
        {
            _partIdPers = 0;
            _actionNamePers = null;
            _methodParameterPers = null;
            
            // Save partUID
            if (_part != null)
            {
                _partIdPers = _part.flightID;
            }
            // Save action name, not index
            if ((_actionList != null) && (_actionIndex < _actionList.Length))
            {
                _actionNamePers = _actionList[_actionIndex];
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
        
        public Part ActionPart
        {
            set
            {
                if (_part == value)
                {
                    return;
                }
                _modified = true;
                _part = value;
                _actionIndex = -1;
                _methodInfo = null;
                _fieldInfo = null;
                _actionSource = null;
                _methodParameter = null;
                
                if (_part == null)
                {
                    return;
                }
                
                // Compute action list that can be performed on this part
                List<string> actions = new List<string>();
                foreach (KeyValuePair<string,MethodWithComplement> entry in _actionMethods)
                {
                    try {
                        Type declaringType = entry.Value.MethodInfo.DeclaringType;
                        if (declaringType == typeof(Part))
                        {
                            actions.Add(entry.Key);
                        }
                        else if (declaringType.IsSubclassOf(typeof(PartModule)))
                        {
                            PartModule module = (PartModule) typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
                            if (module != null)
                            {
                                actions.Add(entry.Key);
                            }
                        }
                    } catch (Exception) {
                        Debug.LogError(Utils.DEBUG_PREFIX + entry.Key + " not found");
                    }
                }
                foreach (KeyValuePair<string,FieldInfo> entry in _actionFields)
                {
                    try {
                        Type declaringType = entry.Value.DeclaringType;
                        if (declaringType == typeof(Part))
                        {
                            actions.Add(entry.Key);
                        }
                        else if (declaringType.IsSubclassOf(typeof(PartModule)))
                        {
                            PartModule module = (PartModule) typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
                            if (module != null)
                            {
                                actions.Add(entry.Key);
                            }
                        }
                    } catch (Exception) {
                        Debug.LogError(Utils.DEBUG_PREFIX + entry.Key + " not found");
                    }
                }
                _actionList = actions.ToArray();
            }
        
            get { return _part; }
        }
        
        public int ActionIndex
        {
            set
            {
                // Action has changed
                if (PartValid && (value != _actionIndex))
                {
                    _modified = true;
                    _actionIndex = -1;
                    _methodInfo = null;
                    _fieldInfo = null;
                    _actionSource = null;
                    _methodParameter = null;
                    // Action is valid for this part
                    if ((value >= 0) && (value < _actionList.Length))
                    {
                        if (_actionMethods.ContainsKey(_actionList[value]))
                        {
                            _actionIndex = value;
                            _methodInfo = _actionMethods[_actionList[_actionIndex]];
                            _methodParameter = new TypedDataArray(_methodInfo.MethodInfo.GetParameters(), _methodInfo.Complement);
                        }
                        else if (_actionFields.ContainsKey(_actionList[value]))
                        {
                            _actionIndex = value;
                            _fieldInfo = _actionFields[_actionList[_actionIndex]];
                            _methodParameter = new TypedDataArray(_actionList[value], _fieldInfo.FieldType);
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
            if (declaringType == typeof(Part))
            {
                _actionSource = _part;
            }
            else if ((declaringType != null) && declaringType.IsSubclassOf(typeof(PartModule)))
            {
                _actionSource = typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
            }
        }
        
        public TypedDataArray Parameters { get { return _methodParameter; } }
        
        public override bool Modified { get { return _modified || ((_methodParameter != null) && _methodParameter.Modified); } }
        public override void Acquit() { _modified = false; _methodParameter.Acquit(); }
        
        public bool PartValid
        {
            get { return (_part != null); }
        }
        
        public bool ActionValid
        {
            get { return (_methodInfo != null) || (_fieldInfo != null); }
        }
                
        public bool ParameterValid
        {
            get { return ((_methodParameter != null) && _methodParameter.IsValid); }
        }
        
        public override bool IsValid()
        {
            return PartValid && ActionValid && ParameterValid;
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
                return "Part action: " + _actionList[_actionIndex] + " on " + _part;
            }
            else
            {
                return "Part action: invalid";
            }
        }
    }
}

