using System;
using UnityEngine;

namespace KspTrigger
{
    public enum TriggerEventType
    {
        Flight,
        Part,
        Timer
    }
    
    public class TriggerEvent
    {
        [Persistent(name="type")]
        private TriggerEventType _type;
        [Persistent(name="hasBeenTriggered")]
        private bool _hasBeenTriggered = false;
        [Persistent(name="autoReset")]
        private bool _autoReset = false;
        [Persistent(name="condition")]
        private TriggerCondition _condition = null;
        private bool _previousValue = true;
        
        public TriggerEvent(TriggerEventType type, VesselTriggers vesselTriggers)
        {
            _type = type;
            _hasBeenTriggered = false;
            AutoReset = false;
            switch (_type)
            {
                case TriggerEventType.Part:
                    _condition = new TriggerConditionPart(vesselTriggers);
                    break;
                case TriggerEventType.Flight:
                    _condition = new TriggerConditionFlight(vesselTriggers);
                    break;
                case TriggerEventType.Timer:
                    _condition = new TriggerConditionTimer(vesselTriggers);
                    break;
            }
            _previousValue = true;
        }
        
        public TriggerEvent(TriggerEvent other)
        {
            _type = other._type;
            _hasBeenTriggered = other._hasBeenTriggered;
            AutoReset = other.AutoReset;
            switch (_type)
            {
                case TriggerEventType.Part:
                    _condition = new TriggerConditionPart((TriggerConditionPart) other._condition);
                    break;
                case TriggerEventType.Flight:
                    _condition = new TriggerConditionFlight((TriggerConditionFlight) other._condition);
                    break;
                case TriggerEventType.Timer:
                    _condition = new TriggerConditionTimer((TriggerConditionTimer) other._condition);
                    break;
            }
            _previousValue = true;
        }
        
        public void LoadPersistentData(TriggerConfig triggerConfig)
        {
            _condition.LoadPersistentData(triggerConfig);
        }
        
        public void UpdatePersistentData()
        {
            _condition.UpdatePersistentData();
        }
        
        public TriggerEventType Type { get { return _type; } }
        
        public bool HasBeenTriggered { get { return _hasBeenTriggered; } }
        
        public bool AutoReset
        {
            set
            {
                _autoReset = value;
            }
            get { return _autoReset; }
        }
        
        public void Reset()
        {
            _hasBeenTriggered = false;
            _previousValue = true;
        }
        
        public bool IsValid()
        {
            return (_condition != null) && _condition.IsValid();
        }
        
        public bool EvaluateEvent()
        {
            bool result = false;
            bool newValue = _condition.EvaluateCondition();
            // Trigger only when condition change from false to true
            if (newValue && !_previousValue)
            {
                result = (_autoReset || !_hasBeenTriggered);
                _hasBeenTriggered = !_autoReset;
            }
            _previousValue = newValue;
            return result;
        }
        
        public TriggerCondition Condition { get { return _condition; } }
        
        public string ToString(bool debug = false)
        {
            return _type.ToString() + " event: " + _condition.Description(debug);
        }
    }
}

