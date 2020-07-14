using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public enum TimerState
    {
        Pending,
        Running,
        Stopped
    }
        
    public abstract class AbsTimer
    {
        [Persistent(name="name")]
        protected string _name;
        [Persistent(name="type")]
        protected string _type;
        public string Name { get { return _name; } }
        [Persistent(name="startUT")]
        protected double _startUT;
        [Persistent(name="stopUT")]
        protected double _stopUT;
        [Persistent(name="state")]
        protected TimerState _state;
        public TimerState State
        {
            get { return _state; }
        }
        [Persistent(name="displayed")]
        public bool Displayed;
        public bool Removed;
        
        public AbsTimer() : this("") {}
        
        public AbsTimer(string name)
        {
            _name = name;
            _type = "";
            _startUT = -1.0f;
            _stopUT = -1.0f;
            _state = TimerState.Pending;
            Displayed = false;
            Removed = false;
        }
        
        protected double CurrentDate()
        {
            if (_state == TimerState.Running)
            {
                return Planetarium.GetUniversalTime();
            }
            else
            {
                return _stopUT;
            }
        }
        
        public TimerState ChangeState()
        {
            switch (_state)
            {
                case TimerState.Pending:
                    _startUT = Planetarium.GetUniversalTime();
                    _stopUT = -1.0f;
                    _state = TimerState.Running;
                    break;
                case TimerState.Running:
                    _stopUT = Planetarium.GetUniversalTime();
                    _state = TimerState.Stopped;
                    break;
                case TimerState.Stopped:
                    _startUT = -1.0f;
                    _stopUT = -1.0f;
                    _state = TimerState.Pending;
                    break;
            }
            return _state;
        }
        
        public void Start()
        {
            if ((_state == TimerState.Pending) || (_state == TimerState.Stopped))
            {
                _startUT = Planetarium.GetUniversalTime();
                _stopUT = -1.0f;
                _state = TimerState.Running;
            }
        }
        
        public void Stop()
        {
            if (_state == TimerState.Running)
            {
                _stopUT = Planetarium.GetUniversalTime();
                _state = TimerState.Stopped;
            }
        }
        
        public void Reset()
        {
            if ((_state == TimerState.Running) || (_state == TimerState.Stopped))
            {
                _startUT = -1.0f;
                _stopUT = -1.0f;
                _state = TimerState.Pending;
            }
        }
        
        public abstract bool Evaluate(double targetDate);
        
        public abstract string DisplayedValue();
        
        public static string DateToString(double date)
        {
            TimeSpan ts = TimeSpan.FromSeconds(date);
            return ts.ToString(@"hh\:mm\:ss\.ff");
        }
    }
    
    public class Timer : AbsTimer
    {
        public Timer() : base()
        {
            _type = "T";
        }
        
        public Timer(string name) : base(name)
        {
            _type = "T";
        }
        
        public override bool Evaluate(double targetDate)
        {
            return (_state == TimerState.Running) && (CurrentDate() - _startUT) > targetDate;
        }
        
        public override string DisplayedValue()
        {
            return DateToString(CurrentDate() - _startUT);
        }
        
        public override string ToString()
        {
            return "Timer " + _name;
        }
    }
        
    public class Countdown : AbsTimer
    {
        [Persistent(name="initDate")]
        private double _initDate = 10.0; // TODO typedData
        public double InitDate
        {
            set { _initDate = value; }
            get { return _initDate; }
        }
        public string InitStrDate;
        
        public void SetInitDate()
        {
            if (_state == TimerState.Pending)
            {
                double prevDate = _initDate;
                try
                {
                    _initDate = Convert.ToDouble(InitStrDate);
                }
                catch (Exception) {}
                if (_initDate <= 0.0)
                {
                    _initDate = prevDate;
                }
            }
            InitStrDate = _initDate.ToString();
        }
        
        public Countdown() : base()
        {
            _type = "C";
        }
        
        public Countdown(string name) : base(name)
        {
            _type = "C";
            SetInitDate();
        }
        
        public override bool Evaluate(double targetDate)
        {
            double date = _initDate - (CurrentDate() - _startUT);
            if (date < 0.0)
            {
                // Stop if running
                if (_state == TimerState.Running)
                {
                    ChangeState();
                }
            }
            // If stopped => trigger
            if (_state != TimerState.Pending)
            {
                return (date < targetDate);
            }
            return false;
        }
        
        public override string DisplayedValue()
        {
            Evaluate(0.0);
            double date = _initDate - (CurrentDate() - _startUT);
            if (date < 0.0)
            {
                return DateToString(0.0);
            }
            else
            {
                return DateToString(date);
            }
        }
        
        public override string ToString()
        {
            return "Countdown " + _name;
        }
    }
}
