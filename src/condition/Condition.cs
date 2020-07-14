using System;
using System.Collections.Generic;

namespace KspTrigger
{
    public enum TriggerConditionType
    {
        Flight,
        Part,
        Timer
    }
    
    public abstract class TriggerCondition
    {
        protected VesselTriggers _vesselTriggers = null;
        [Persistent(name="type")]
        protected TriggerConditionType _type;
        protected bool _modified = false;
        
        public TriggerCondition(VesselTriggers vesselTriggers)
        {
            _vesselTriggers = vesselTriggers;
            _type = (TriggerConditionType) (-1);
            _modified = true;
        }
        
        public TriggerCondition(TriggerCondition other)
        {
            _vesselTriggers = other._vesselTriggers;
            _type = (TriggerConditionType) (-1);
            _modified = false;
        }
        
        public abstract void LoadPersistentData();
        public abstract void UpdatePersistentData();
        
        public virtual bool Modified { get { return _modified; } }
        public virtual void Acquit() { _modified = false; }
        
        public abstract bool IsValid();
        public abstract bool EvaluateCondition();
        
        public abstract string Description(bool debug = false);
    }
    
    public class TriggerConditions
    {
        private List<TriggerCondition> _conditions;
        public List<TriggerCondition> Conditions { get { return _conditions; } }
        
        public TriggerConditions()
        {
            _conditions = new List<TriggerCondition>();
        }
        
        public TriggerConditions(TriggerConditions other)
        {
            _conditions = new List<TriggerCondition>();
            foreach(TriggerCondition condition in other._conditions)
            {
                if (condition == null)
                {
                    _conditions.Add(null);
                }
                else if (condition is TriggerConditionPart)
                {
                    _conditions.Add(new TriggerConditionPart((TriggerConditionPart) condition));
                }
                else if (condition is TriggerConditionFlight)
                {
                    _conditions.Add(new TriggerConditionFlight((TriggerConditionFlight) condition));
                }
                else if (condition is TriggerConditionTimer)
                {
                    _conditions.Add(new TriggerConditionTimer((TriggerConditionTimer) condition));
                }
            }
        }
        
        public TriggerCondition this[int i]
        {
            set { _conditions[i] = value; }
            get { return _conditions[i]; }
        }
        
        public int Count { get { return _conditions.Count; } }
        
        public void Add(TriggerCondition condition)
        {
            _conditions.Add(condition);
        }
        
        public void RemoveAt(int index)
        {
            _conditions.RemoveAt(index);
        }
        
        public void LoadPersistentData()
        {
            foreach (TriggerCondition condition in _conditions)
            {
                if (condition != null)
                {
                    condition.LoadPersistentData();
                }
            }
        }
        
        public void UpdatePersistentData()
        {
            foreach (TriggerCondition condition in _conditions)
            {
                if (condition != null)
                {
                    condition.UpdatePersistentData();
                }
            }
        }
        
        public bool EvaluateCondition()
        {
            if (_conditions.Count == 0)
            {
                return true;
            }
            // TODO all true / one true
            bool result = true;
            foreach (TriggerCondition condition in _conditions)
            {
                if (condition != null)
                {
                    result &= condition.EvaluateCondition();
                }
            }
            return result;
        }
        
        public override string ToString()
        {
            if (_conditions.Count == 0)
            {
                return "Always true";
            }
            else
            {
                return _conditions.Count + " condition(s)";
            }
        }
    }
}

