using System;

namespace KspTrigger
{
    public class TriggerActionTimer : TriggerAction
    {
        public enum TimerActionType
        {
            Start,
            Stop,
            Reset
        }
    
        private AbsTimer _timer = null;
        private string _name = "";
        private TimerActionType _timerAction = TimerActionType.Start;
        
        // Persistent data
        [Persistent(name="timerName")]
        private string _timerNamePers = "";
        [Persistent(name="timerAction")]
        private TimerActionType _timerActionPers = TimerActionType.Start;
        
        public TriggerActionTimer(VesselTriggers vesselTriggers) : base(vesselTriggers)
        {
            _type = TriggerActionType.Timer;
            _timer = null;
            _name = "";
            _timerAction = TimerActionType.Start;
        }

        public TriggerActionTimer(TriggerActionTimer other) : base(other)
        {
            _type = TriggerActionType.Timer;
            _timer = null;
            // Automatic call of Name_set
            Name = other.Name;
            _timerAction = other._timerAction;
            _modified = false;
            UpdatePersistentData();
        }
        
        public override void LoadPersistentData()
        {
            Name = _timerNamePers;
            _timerAction = _timerActionPers;
        }
        
        public override void UpdatePersistentData()
        {
            _timerNamePers = _name;
            _timerActionPers = _timerAction;
        }
        
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _modified = true;
                    _name = value;
                    _timer = _vesselTriggers.GetTimer(value);
                }
            }
        }
        
        public TimerActionType TimerAction
        {
            set
            {
                // TimerActionIndex has changed
                if ((value != _timerAction) && (Enum.IsDefined(typeof(TimerActionType), value)))
                {
                    _modified = true;
                    _timerAction = value;
                }
            }
            
            get { return _timerAction; }
        }
        
        public bool TimerValid
        {
            get { return (_timer != null) && (!_timer.Removed); }
        }
        
        public override bool IsValid()
        {
            return TimerValid;
        }
        
        public override void DoAction()
        {
            if (IsValid())
            {
                switch (_timerAction)
                {
                    case TimerActionType.Start:
                        _timer.Start();
                        break;
                    case TimerActionType.Stop:
                        _timer.Stop();
                        break;
                    case TimerActionType.Reset:
                        _timer.Reset();
                        break;
                    default:
                        break;
                }
            }
        }
        
        public override string ToString()
        {
            if (IsValid())
            {
                return "Timer action: " + _timer;
            }
            else
            {
                return "Timer action: invalid";
            }
        }
    }
}

