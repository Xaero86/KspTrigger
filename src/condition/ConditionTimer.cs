using System;

namespace KspTrigger
{
    public class TriggerConditionTimer : TriggerCondition
    {
        private AbsTimer _timer = null;
        private string _name = "";
        private TypedData _targetDate = new TypedData("Target date", typeof(double));
        
        // Persistent data
        [Persistent(name="timerName")]
        private string _timerNamePers = "";
        [Persistent(name="targetDate")]
        private string _targetDatePers = "";
        
        public TriggerConditionTimer(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerConditionType.Timer;
            _timer = null;
            _name = "";
        }
        
        public TriggerConditionTimer(TriggerConditionTimer other) : base(other)
        {
            _type = TriggerConditionType.Timer;
            _timer = null;
            // Automatic call of Name_set
            Name = other.Name;
            _targetDate.ValueStr = other._targetDate.ValueStr;
            UpdatePersistentData();
        }
        
        public override void LoadPersistentData()
        {
            Name = _timerNamePers;
            _targetDate.ValueStr = _targetDatePers;
        }
        
        public override void UpdatePersistentData()
        {
            _timerNamePers = _name;
            if (_targetDate != null)
            {
                _targetDatePers = _targetDate.ValueStr;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    _timer = _vesselTriggers.GetTimer(value);
                    if (_timer != null)
                    {
                        if ((_timer is Countdown) && !_targetDate.IsValid)
                        {
                            _targetDate.ValueStr = "0.0";
                        }
                    }
                }
            }
        }
        
        public TypedData TargetDate { get { return _targetDate; } }
        
        public bool TimerValid
        {
            get { return (_timer != null) && !_timer.Removed; }
        }
        
        public bool TargetValid
        {
            get
            {
                return TimerValid && _targetDate.IsValid &&
                    (((_timer is Countdown) && _targetDate.CompareTo(0.0, ComparatorType.MoreOrEquals) && _targetDate.CompareTo(((Countdown) _timer).InitDate, ComparatorType.Less)) || 
                    ((_timer is Timer) && _targetDate.CompareTo(0.0, ComparatorType.More)));
            }
        }

        public override bool IsValid()
        {
            return TargetValid;
        }
        
        public override bool EvaluateCondition()
        {
            if (!IsValid())
            {
                return false;
            }
            return _timer.Evaluate((double) _targetDate.Value);
        }
        
        public override string Description(bool debug = false)
        {
            if (IsValid())
            {
                string result = _timer.ToString();
                if (debug)
                {
                    result += " [" + _timer.DisplayedValue() + "/" + AbsTimer.DateToString((double) _targetDate.Value) + "]";
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
            return "Timer condition: " + Description();
        }
    }
}

