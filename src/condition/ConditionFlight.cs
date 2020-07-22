using System;
using System.Reflection;
using System.Collections.Generic;

namespace KspTrigger
{
    public class TriggerConditionFlight : TriggerCondition
    {
        // Index of the property to evaluate
        private int _propertyIndex = -1;
        // Property to evaluate
        private FieldInfo _fieldInfo = null;
        private PropertyInfo _propertyInfo = null;
        private MethodWithComplement _methodInfo = null;
        // Object that contain the field. null for static
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
        [Persistent(name="propertyName")]
        private string _propertyNamePers = "";
        [Persistent(name="comparator")]
        private ComparatorType _comparatorPers = ComparatorType.Equals;
        [Persistent(name="targetValue")]
        private string _targetValuePers = "";
        [Persistent(name="methodParameter")]
        private string[] _methodParameterPers;
        
        // Property list that can be evaluated
        public static readonly string[] PropertyList;
        
        private static readonly Dictionary<string,FieldInfo> _propertyFields = new Dictionary<string,FieldInfo>()
        {
            {"Altitude",          typeof(Vessel).GetField("altitude")},
            {"RadarAltitude",     typeof(Vessel).GetField("radarAltitude")},
            {"TerrainHeight",     typeof(Vessel).GetField("terrainAltitude")},
            {"VerticalSpeed",     typeof(Vessel).GetField("verticalSpeed")},
            {"TimeToAp",          typeof(Orbit).GetField("timeToAp")},
            {"TimeToPe",          typeof(Orbit).GetField("timeToPe")},
            {"OrbitPeriod",       typeof(Orbit).GetField("period")},
            {"GeeForce",          typeof(Vessel).GetField("geeForce")},
            {"Situation",         typeof(Vessel).GetField("situation")},
            {"CurrentStage",      typeof(Vessel).GetField("currentStage")}
        };
        
        private static readonly Dictionary<string,PropertyInfo> _propertyProperties = new Dictionary<string,PropertyInfo>()
        {
            {"ApoapsisAltitude",  typeof(Orbit).GetProperty("ApA")},
            {"PeriapsisAltitude", typeof(Orbit).GetProperty("PeA")},
            {"ApoapsisRadius",    typeof(Orbit).GetProperty("ApR")},
            {"PeriapsisRadius",   typeof(Orbit).GetProperty("PeR")}
        };
        
        private static readonly Dictionary<string,MethodWithComplement> _propertyMethods = new Dictionary<string,MethodWithComplement>()
        {
            {"OrbitalSpeed",      new MethodWithComplement(typeof(VesselWrapper), "OrbitalSpeed", 0)},
            {"GroundSpeed",       new MethodWithComplement(typeof(VesselWrapper), "GroundSpeed", 0)},
            {"ResourceAmount",    new MethodWithComplement(typeof(VesselWrapper), "GetResourceAmount", 1, new object[] { VesselWrapper.ResourceDict })},
            {"ResourceAmount%",   new MethodWithComplement(typeof(VesselWrapper), "GetResourceAmountPercent", 1, new object[] { VesselWrapper.ResourceDict })},
        };
        
        static TriggerConditionFlight()
        {
            // Compute property list that can be evaluated on this part
            List<string> properties = new List<string>();
            foreach (KeyValuePair<string,FieldInfo> entry in _propertyFields)
            {
                if (entry.Value != null)
                {
                    properties.Add(entry.Key);
                }
            }
            foreach (KeyValuePair<string,PropertyInfo> entry in _propertyProperties)
            {
                if (entry.Value != null)
                {
                    properties.Add(entry.Key);
                }
            }
            foreach (KeyValuePair<string,MethodWithComplement> entry in _propertyMethods)
            {
                if (entry.Value != null)
                {
                    properties.Add(entry.Key);
                }
            }
            PropertyList = properties.ToArray();
        }
        
        public TriggerConditionFlight(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerConditionType.Flight;
            _propertyIndex = -1;
            _fieldInfo = null;
            _propertyInfo = null;
            _methodInfo = null;
            _propertySource = null;
            _methodParameter = null;
            _comparator = ComparatorType.Equals;
            _targetValue = null;
        }
        
        public TriggerConditionFlight(TriggerConditionFlight other) : base(other)
        {
            _type = TriggerConditionType.Flight;
            // Automatic call of PropertyIndex_set
            PropertyIndex = other._propertyIndex;
            _comparator = other._comparator;
            if (other._targetValue != null)
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
            UpdatePersistentData();
        }
        
        public override void LoadPersistentData(TriggerConfig triggerConfig)
        {
            // Property name
            if (_propertyFields.ContainsKey(_propertyNamePers) || _propertyProperties.ContainsKey(_propertyNamePers) || _propertyMethods.ContainsKey(_propertyNamePers))
            {
                // Name valid => to index
                PropertyIndex = PropertyList.IndexOf(_propertyNamePers);
                
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
        
        public override void UpdatePersistentData()
        {
            _propertyNamePers = null;
            _comparatorPers = ComparatorType.Equals;
            _targetValuePers = null;
            _methodParameterPers = null;
            
            // Save property name, not index
            if (_propertyIndex < PropertyList.Length)
            {
                _propertyNamePers = PropertyList[_propertyIndex];
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
        
        public int PropertyIndex
        {
            set
            {
                // Property has changed
                if (value != _propertyIndex)
                {
                    _propertyIndex = -1;
                    _fieldInfo = null;
                    _propertyInfo = null;
                    _methodInfo = null;
                    _propertySource = null;
                    _methodParameter = null;
                    _comparator = ComparatorType.Equals;
                    _targetValue = null;
                    // Property is valid
                    if ((value >= 0) && (value < PropertyList.Length))
                    {
                        if (_propertyFields.ContainsKey(PropertyList[value]))
                        {
                            _propertyIndex = value;
                            _fieldInfo = _propertyFields[PropertyList[_propertyIndex]];
                            _targetValue = new TypedData(PropertyList[_propertyIndex], _fieldInfo.FieldType);
                        }
                        else if (_propertyProperties.ContainsKey(PropertyList[value]))
                        {
                            _propertyIndex = value;
                            _propertyInfo = _propertyProperties[PropertyList[_propertyIndex]];
                            _targetValue = new TypedData(PropertyList[_propertyIndex], _propertyInfo.PropertyType);
                        }
                        else if (_propertyMethods.ContainsKey(PropertyList[value]))
                        {
                            _propertyIndex = value;
                            _methodInfo = _propertyMethods[PropertyList[_propertyIndex]];
                            _targetValue = new TypedData(PropertyList[_propertyIndex], _methodInfo.MethodInfo.ReturnType);
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
            else if (_propertyInfo != null)
            {
                declaringType = _propertyInfo.DeclaringType;
            }
            else if (_methodInfo != null)
            {
                declaringType = _methodInfo.MethodInfo.DeclaringType;
            }
            if (declaringType == typeof(Orbit))
            {
                _propertySource = typeof(Vessel).GetMethod("GetOrbit").Invoke(_vesselTriggers.Vessel, null);
            }
            else if (declaringType == typeof(Vessel))
            {
                _propertySource = _vesselTriggers.Vessel;
            }
            else if (declaringType == typeof(VesselWrapper))
            {
                _propertySource = typeof(VesselWrapper).GetConstructor(new Type[] {typeof(Vessel)}).Invoke(new object[] {_vesselTriggers.Vessel});
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
        
        public bool PropertyValid
        {
            get { return ((_fieldInfo != null) || (_propertyInfo != null) || (_methodInfo != null)) && (_propertySource != null); }
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
            return PropertyValid && ComparatorValid && TargetValid;
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
            else if (_propertyInfo != null)
            {
                _currentValue = _propertyInfo.GetValue(_propertySource);
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
                string result = PropertyList[_propertyIndex];
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
            return "Flight condition: " + Description();
        }
    }
}

