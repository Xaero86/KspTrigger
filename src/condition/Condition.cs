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
    
    public enum ConditionCombination
    {
        Every,
        AtLeastOne
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
        [Persistent(name="combination")]
        public ConditionCombination Combination;
        
        public TriggerConditions()
        {
            _conditions = new List<TriggerCondition>();
            Combination = ConditionCombination.Every;
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
            Combination = other.Combination;
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
        
        private const string KEY_NB_CONDITIONS  = "nbConditions";
        private const string KEY_PREF_CONDITION = "condition";
        
        public void OnLoad(ConfigNode node, VesselTriggers triggerConfig)
        {
            bool dataFound = false;
            ConfigNode childNode = null;
            int nbItem = 0;
            TriggerConditionType conditionType = (TriggerConditionType) (-1);
            
            dataFound = node.TryGetValue(KEY_NB_CONDITIONS, ref nbItem);
            if (dataFound)
            {
                for (int i = 0; i < nbItem; i++)
                {
                    TriggerCondition condition = null;
                    dataFound = node.TryGetNode(KEY_PREF_CONDITION + i, ref childNode);
                    if (dataFound)
                    {
                        dataFound = childNode.TryGetEnum<TriggerConditionType>("type", ref conditionType, (TriggerConditionType) (-1));
                        if (dataFound)
                        {
                            switch (conditionType)
                            {
                                case TriggerConditionType.Part:
                                    condition = new TriggerConditionPart(triggerConfig);
                                    break;
                                case TriggerConditionType.Flight:
                                    condition = new TriggerConditionFlight(triggerConfig);
                                    break;
                                case TriggerConditionType.Timer:
                                    condition = new TriggerConditionTimer(triggerConfig);
                                    break;
                                default:
                                    break;
                            }
                            if (condition != null)
                            {
                                dataFound = ConfigNode.LoadObjectFromConfig(condition, childNode);
                                if (dataFound)
                                {
                                    _conditions.Add(condition);
                                }
                            }
                        }
                    }
                }
            }
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
        
        public void OnSave(ConfigNode node)
        {
            ConfigNode childNode = null;
            int i = 0;
            foreach (TriggerCondition condition in _conditions)
            {
                childNode = ConfigNode.CreateConfigFromObject(condition);
                if (childNode != null)
                {
                    node.SetNode(KEY_PREF_CONDITION + i, childNode, true);
                    i++;
                }
            }
            node.SetValue(KEY_NB_CONDITIONS, i, true);
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
            bool allTrue = true;
            foreach (TriggerCondition condition in _conditions)
            {
                if (condition != null)
                {
                    bool result = condition.EvaluateCondition();
                    if (result && (Combination == ConditionCombination.AtLeastOne))
                    {
                        return true;
                    }
                    allTrue &= result;
                }
            }
            return allTrue;
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

