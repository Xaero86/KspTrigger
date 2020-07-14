using System;
using System.Collections.Generic;

namespace KspTrigger
{
    public enum TriggerActionType
    {
        Flight,
        Part,
        Message,
        Timer
    }
    
    public abstract class TriggerAction
    {
        protected VesselTriggers _vesselTriggers = null;
        [Persistent(name="type")]
        protected TriggerActionType _type;
        protected bool _modified = false;
        
        public TriggerAction(VesselTriggers vesselTriggers)
        {
            _vesselTriggers = vesselTriggers;
            _type = (TriggerActionType) (-1);
            _modified = true;
        }
        
        public TriggerAction(TriggerAction other)
        {
            _vesselTriggers = other._vesselTriggers;
            _type = (TriggerActionType) (-1);
            _modified = false;
        }
        
        public abstract void LoadPersistentData();
        public abstract void UpdatePersistentData();
        
        public virtual bool Modified { get { return _modified; } }
        public virtual void Acquit() { _modified = false; }
        
        public abstract bool IsValid();
        public abstract void DoAction();
    }
    
    public class TriggerActions
    {
        private List<TriggerAction> _actions;
        public List<TriggerAction> Actions { get { return _actions; } }
        
        public TriggerActions()
        {
            _actions = new List<TriggerAction>();
        }
        
        public TriggerActions(TriggerActions other)
        {
            _actions = new List<TriggerAction>();
            foreach(TriggerAction action in other._actions)
            {
                if (action == null)
                {
                    _actions.Add(null);
                }
                else if (action is TriggerActionPart)
                {
                    _actions.Add(new TriggerActionPart((TriggerActionPart) action));
                }
                else if (action is TriggerActionFlight)
                {
                    _actions.Add(new TriggerActionFlight((TriggerActionFlight) action));
                }
                else if (action is TriggerActionMessage)
                {
                    _actions.Add(new TriggerActionMessage((TriggerActionMessage) action));
                }
                else if (action is TriggerActionTimer)
                {
                    _actions.Add(new TriggerActionTimer((TriggerActionTimer) action));
                }
            }
        }
        
        public TriggerAction this[int i]
        {
            set { _actions[i] = value; }
            get { return _actions[i]; }
        }
        
        public int Count { get { return _actions.Count; } }
        
        public void Add(TriggerAction action)
        {
            _actions.Add(action);
        }
        
        public void RemoveAt(int index)
        {
            _actions.RemoveAt(index);
        }
        
        private const string KEY_NB_ACTIONS     = "nbActions";
        private const string KEY_PREF_ACTION    = "action";
        
        public void OnLoad(ConfigNode node, VesselTriggers triggerConfig)
        {
            bool dataFound = false;
            ConfigNode childNode = null;
            int nbItem = 0;
            TriggerActionType actionType = (TriggerActionType) (-1);
            
            dataFound = node.TryGetValue(KEY_NB_ACTIONS, ref nbItem);
            if (dataFound)
            {
                for (int i = 0; i < nbItem; i++)
                {
                    TriggerAction action = null;
                    dataFound = node.TryGetNode(KEY_PREF_ACTION + i, ref childNode);
                    if (dataFound)
                    {
                        dataFound = childNode.TryGetEnum<TriggerActionType>("type", ref actionType, (TriggerActionType) (-1));
                        if (dataFound)
                        {
                            switch (actionType)
                            {
                                case TriggerActionType.Part:
                                    action = new TriggerActionPart(triggerConfig);
                                    break;
                                case TriggerActionType.Flight:
                                    action = new TriggerActionFlight(triggerConfig);
                                    break;
                                case TriggerActionType.Message:
                                    action = new TriggerActionMessage(triggerConfig);
                                    break;
                                case TriggerActionType.Timer:
                                    action = new TriggerActionTimer(triggerConfig);
                                    break;
                                default:
                                    break;
                            }
                            if (action != null)
                            {
                                dataFound = ConfigNode.LoadObjectFromConfig(action, childNode);
                                if (dataFound)
                                {
                                    _actions.Add(action);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public void LoadPersistentData()
        {
            foreach (TriggerAction action in _actions)
            {
                if (action != null)
                {
                    action.LoadPersistentData();
                }
            }
        }
        
        public void OnSave(ConfigNode node)
        {
            ConfigNode childNode = null;
            int i = 0;
            foreach (TriggerAction action in _actions)
            {
                childNode = ConfigNode.CreateConfigFromObject(action);
                if (childNode != null)
                {
                    node.SetNode(KEY_PREF_ACTION + i, childNode, true);
                    i++;
                }
            }
            node.SetValue(KEY_NB_ACTIONS, i, true);
        }
        
        public void UpdatePersistentData()
        {
            foreach (TriggerAction action in _actions)
            {
                if (action != null)
                {
                    action.UpdatePersistentData();
                }
            }
        }
        
        public bool IsValid()
        {
            if (_actions.Count == 0)
            {
                return false;
            }
            bool result = true;
            foreach (TriggerAction action in _actions)
            {
                result &= (action != null) && action.IsValid();
            }
            return result;
        }
        
        public void DoAction()
        {
            foreach (TriggerAction action in _actions)
            {
                if (action != null)
                {
                    action.DoAction();
                }
            }
        }
        
        public override string ToString()
        {
            if (_actions.Count == 0)
            {
                return "To be configured";
            }
            else
            {
                int invalid = 0;
                foreach (TriggerAction action in _actions)
                {
                    if ((action == null) || !action.IsValid())
                    {
                        invalid++;
                    }
                }
                if (invalid > 0)
                {
                    return invalid + " invalid action(s) out of " + _actions.Count;
                }
                else
                {
                    return _actions.Count + " action(s)";
                }
            }
        }
    }
}

