using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class TriggerConditionPart : TriggerCondition
    {
        // Part to evaluate
        private Part _part = null;
        // Index of the property to test
        private int _propertyIndex = -1;
        // Property to evaluate
        private FieldInfo _fieldInfo = null;
        private MethodWithComplement _methodInfo = null;
        // Object that contain the field
        private object _propertySource = null;
        // Parameter for the method
        private TypedDataArray _methodParameter = null;
        // Operator used to compare property and value
        private ComparatorType _comparator = ComparatorType.Equals;
        // Value to compare with the property
        private TypedData _targetValue = null;
        // Keep value for debug
        private object _currentValue = null;
        
        // Persistent data
        [Persistent(name="partId")]
        private uint _partIdPers = 0;
        [Persistent(name="propertyName")]
        private string _propertyNamePers = "";
        [Persistent(name="comparator")]
        private ComparatorType _comparatorPers = ComparatorType.Equals;
        [Persistent(name="targetValue")]
        private string _targetValuePers = "";
        [Persistent(name="methodParameter")]
        private string[] _methodParameterPers;
        
        // Property list that can be evaluated on this part
        private string[] _propertyList = null;
        public string[] PropertyList { get { return _propertyList; } }
        
        private static readonly Dictionary<string,FieldInfo> _propertyFields = new Dictionary<string,FieldInfo>()
        {
            {"Temperature",     typeof(Part).GetField("temperature")},
            {"Ignited",         typeof(ModuleEngines).GetField("EngineIgnited")},
            {"Shutdown",        typeof(ModuleEngines).GetField("engineShutdown")},
            {"Flameout",        typeof(ModuleEngines).GetField("flameout")},
            {"DeploymentState", typeof(ModuleParachute).GetField("deploymentState")},
            {"Efficiency",      typeof(ModuleGenerator).GetField("efficiency")},
            {"GrappleState",    typeof(ModuleGrappleNode).GetField("state")},
            {"LightOn",         typeof(ModuleLight).GetField("isOn")},
            {"LiftScalar",      typeof(ModuleLiftingSurface).GetField("liftScalar")},
            {"IntakeSpeed",     typeof(ModuleResourceIntake).GetField("intakeSpeed")},
            {"AirFlow",         typeof(ModuleResourceIntake).GetField("airFlow")},
            {"Damaged",         typeof(ModuleWheels.ModuleWheelDamage).GetField("isDamaged")}
        };
        
        private static readonly Dictionary<string,MethodWithComplement> _propertyMethods = new Dictionary<string,MethodWithComplement>()
        {
            {"CurrentThrust",   new MethodWithComplement(typeof(ModuleEngines), "GetCurrentThrust", 0)},
            {"ValidConvert",    new MethodWithComplement(typeof(BaseConverter), "IsSituationValid", 0)},
            {"CoreTemperature", new MethodWithComplement(typeof(BaseConverter), "GetCoreTemperature", 0)},
            {"Overheating",     new MethodWithComplement(typeof(BaseConverter), "IsOverheating", 0)},
            {"ValidScanner",    new MethodWithComplement(typeof(ModuleResourceScanner), "IsSituationValid", 0)},
            {"ScienceCount",    new MethodWithComplement(typeof(ModuleScienceExperiment), "GetScienceCount", 0)},
            {"ScienceCount_",   new MethodWithComplement(typeof(ModuleScienceContainer), "GetScienceCount", 0)},
            {"ResourceAmount",  new MethodWithComplement(typeof(PartWrapper), "GetResourceAmount", 1, new object[] { VesselWrapper.ResourceDict })},
            {"ResourceAmount%", new MethodWithComplement(typeof(PartWrapper), "GetResourceAmountPercent", 1, new object[] { VesselWrapper.ResourceDict })}
        };
        
        public TriggerConditionPart(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerConditionType.Part;
            _part = null;
            _propertyIndex = -1;
            _fieldInfo = null;
            _methodInfo = null;
            _propertySource = null;
            _methodParameter = null;
            _propertyList = null;
            _comparator = ComparatorType.Equals;
            _targetValue = null;
            _currentValue = null;
        }
        
        public TriggerConditionPart(TriggerConditionPart other) : base(other)
        {
            _type = TriggerConditionType.Part;
            _part = null;
            // Automatic call of Part_set
            ConditionPart = other._part;
            // Automatic call of PropertyIndex_set
            PropertyIndex = other._propertyIndex;
            _comparator = other._comparator;
            if ((_targetValue != null) && other._targetValue != null)
            {
                _targetValue.ValueStr = other._targetValue.ValueStr;
            }
            if (_methodParameter != null)
            {
                for (int i = 0; i < _methodParameter.Length; i++)
                {
                    _methodParameter[i].ValueStr = other._methodParameter[i].ValueStr;
                }
            }
            _currentValue = null;
            UpdatePersistentData();
        }
        
        public override void LoadPersistentData(TriggerConfig triggerConfig)
        {
            // Part UID
            ConditionPart = _vesselTriggers.Vessel[_partIdPers];
            
            if ((_part != null) && (_propertyList != null))
            {
                // Property name
                if (_propertyFields.ContainsKey(_propertyNamePers) || _propertyMethods.ContainsKey(_propertyNamePers))
                {
                    // Name valid => to index
                    PropertyIndex = _propertyList.IndexOf(_propertyNamePers);
                    
                    // Comparator
                    Comparator = _comparatorPers;
                    
                    // Target value
                    _targetValue.ValueStr = _targetValuePers;
                    
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
            _propertyNamePers = null;
            _comparatorPers = ComparatorType.Equals;
            _targetValuePers = null;
            _methodParameterPers = null;
            
            // Save partUID
            if (_part != null)
            {
                _partIdPers = _part.flightID;
            }
            // Save property name, not index
            if ((_propertyList != null) && (_propertyIndex < _propertyList.Length))
            {
                _propertyNamePers = _propertyList[_propertyIndex];
            }
            _comparatorPers = _comparator;
            // Target value
            if (_targetValue != null)
            {
                _targetValuePers = _targetValue.ValueStr;
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
        
        public Part ConditionPart
        {
            set
            {
                if (_part == value)
                {
                    return;
                }
                _part = value;
                _propertyIndex = -1;
                _fieldInfo = null;
                _methodInfo = null;
                _propertySource = null;
                _methodParameter = null;
                _propertyList = null;
                _comparator = ComparatorType.Equals;
                _targetValue = null;
                _currentValue = null;
                
                if (_part == null)
                {
                    return;
                }
                // Compute property list that can be evaluated on this part
                List<string> properties = new List<string>();
                foreach (KeyValuePair<string,FieldInfo> entry in _propertyFields)
                {
                    try {
                        Type declaringType = entry.Value.DeclaringType;
                        if (declaringType == typeof(Part))
                        {
                            properties.Add(entry.Key);
                        }
                        else if (declaringType.IsSubclassOf(typeof(PartModule)))
                        {
                            PartModule module = (PartModule) typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
                            if (module != null)
                            {
                                properties.Add(entry.Key);
                            }
                        }
                    } catch (Exception) {
                        Debug.LogError(Utils.DEBUG_PREFIX + "Part field initialisation: part=" + _part + ", " + entry.Key + " not found");
                    }
                }
                foreach (KeyValuePair<string,MethodWithComplement> entry in _propertyMethods)
                {
                    try {
                        Type declaringType = entry.Value.MethodInfo.DeclaringType;
                        if (declaringType == typeof(Part))
                        {
                            properties.Add(entry.Key);
                        }
                        else if (declaringType.IsSubclassOf(typeof(PartModule)))
                        {
                            PartModule module = (PartModule) typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
                            if (module != null)
                            {
                                properties.Add(entry.Key);
                            }
                        }
                        else if (declaringType == typeof(PartWrapper))
                        {
                            properties.Add(entry.Key);
                        }
                    } catch (Exception) {
                        Debug.LogError(Utils.DEBUG_PREFIX + "Part methods initialisation: part=" + _part + ", " + entry.Key + " not found");
                    }
                }
                _propertyList = properties.ToArray();
            }
            
            get { return _part; }
        }
        
        public int PropertyIndex
        {
            set
            {
                // Property has changed
                if (value != _propertyIndex)
                {
                    _propertyIndex = -1;
                    _fieldInfo = null;
                    _methodInfo = null;
                    _comparator = ComparatorType.Equals;
                    _targetValue = null;
                    _propertySource = null;
                    _methodParameter = null;
                    
                    // Property is valid for this part
                    if (PartValid && (value >= 0) && (value < _propertyList.Length))
                    {
                        if (_propertyFields.ContainsKey(_propertyList[value]))
                        {
                            _propertyIndex = value;
                            _fieldInfo = _propertyFields[_propertyList[_propertyIndex]];
                            _targetValue = new TypedData(_propertyList[_propertyIndex], _fieldInfo.FieldType);
                        }
                        else if (_propertyMethods.ContainsKey(_propertyList[value]))
                        {
                            _propertyIndex = value;
                            _methodInfo = _propertyMethods[_propertyList[_propertyIndex]];
                            _targetValue = new TypedData(_propertyList[_propertyIndex], _methodInfo.MethodInfo.ReturnType);
                            _methodParameter = new TypedDataArray(_methodInfo.MethodInfo.GetParameters(), _methodInfo.Complement);
                        }
                        ResolveSource();
                    }
                }
            }
        
            get { return _propertyIndex; }
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
                _propertySource = _part;
            }
            else if ((declaringType != null) && declaringType.IsSubclassOf(typeof(PartModule)))
            {
                _propertySource = typeof(Part).GetMethod("FindModuleImplementing").MakeGenericMethod(declaringType).Invoke(_part, null);
            }
            else if (declaringType == typeof(PartWrapper))
            {
                _propertySource = typeof(PartWrapper).GetConstructor(new Type[] { typeof(Part) }).Invoke(new object[] { _part });
            }
        }
        
        public TypedDataArray Parameters { get { return _methodParameter; } }
        
        public ComparatorType Comparator
        {
            set
            {
                // Comparator has changed
                if ((value != _comparator) && (Enum.IsDefined(typeof(ComparatorType), value)))
                {
                    _comparator = value;
                    if (!ComparatorValid)
                    {
                        _comparator = ComparatorType.Equals;
                    }
                }
            }
            
            get { return _comparator; }
        }
        
        public TypedData TargetValue { get { return _targetValue; } }
        
        public bool PartValid
        {
            get { return (_part != null); }
        }
        
        public bool PropertyValid
        {
            get { return ((_fieldInfo != null) || (_methodInfo != null)) && (_propertySource != null); }
        }
        
        public bool ComparatorValid
        {
            get { return (_targetValue != null) && _targetValue.ComparatorValid(_comparator); }
        }
        
        public bool TargetValid
        {
            get { return (_targetValue != null) && _targetValue.IsValid; }
        }
        
        public bool ParameterValid
        {
            get { return ((_methodInfo == null) || (_methodParameter != null) && _methodParameter.IsValid); }
        }
        
        public override bool IsValid()
        {
            return PartValid && PropertyValid && ComparatorValid && TargetValid && ParameterValid;
        }
        
        public override bool EvaluateCondition()
        {
            if (!IsValid())
            {
                return false;
            }
            if (_fieldInfo != null)
            {
                _currentValue = _fieldInfo.GetValue(_propertySource);
            }
            else if (_methodInfo != null)
            {
                _currentValue = _methodInfo.MethodInfo.Invoke(_propertySource, _methodParameter.Array);
            }
            if (_currentValue != null)
            {
                 return _targetValue.CompareFrom(_currentValue, _comparator);
            }
            else
            {
                return false;
            }
        }
        
        public override string Description(bool debug = false)
        {
            if (IsValid())
            {
                string result = _propertyList[_propertyIndex] + " on " + _part;
                if (debug)
                {
                    result += " [" + _currentValue + "/" + _targetValue + "]";
                }
                return result;
            }
            else
            {
                return "invalid";
            }
        }
        
        public override string ToString()
        {
            return "Part condition: " + Description();
        }
    }
}

