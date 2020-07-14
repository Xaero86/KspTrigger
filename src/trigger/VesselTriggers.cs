using System;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class VesselTriggers : VesselModule
    {
        private List<Trigger> _triggers = new List<Trigger>();
        public List<Trigger> Triggers { get { return _triggers; } }
        private Dictionary<string,AbsTimer> _timers = new Dictionary<string,AbsTimer>();
        public Dictionary<string,AbsTimer> Timers { get { return _timers; } }
        
        private Autopilot _autopilot;
        public Autopilot Autopilot { get { return _autopilot; } }
        
        public const string KEY_PREFIX     = "KAT_";
        private const string KEY_NB_TIMER   = KEY_PREFIX+"nbTimers";
        private const string KEY_PREF_TIMER = KEY_PREFIX+"timer";
        private const string KEY_NB_TRIG    = KEY_PREFIX+"nbTriggers";
        private const string KEY_PREF_TRIG  = KEY_PREFIX+"trigger";
        
        protected override void OnLoad(ConfigNode node)
        {
            try {
                _triggers = new List<Trigger>();
                _timers = new Dictionary<string,AbsTimer>();
                
                bool dataFound = false;
                ConfigNode childNode = null;
                int nbItem = 0;
                string type = "";
                
                // Timers
                dataFound = node.TryGetValue(KEY_NB_TIMER, ref nbItem);
                if (dataFound)
                {
                    for (int i = 0; i < nbItem; i++)
                    {
                        AbsTimer timer = null;
                        dataFound = node.TryGetNode(KEY_PREF_TIMER + i, ref childNode);
                        if (dataFound)
                        {
                            dataFound = childNode.TryGetValue("type", ref type);
                            if (type == "T")
                            {
                                timer = ConfigNode.CreateObjectFromConfig<Timer>(childNode);
                            }
                            else if (type == "C")
                            {
                                timer = ConfigNode.CreateObjectFromConfig<Countdown>(childNode);
                                ((Countdown) timer).InitStrDate = ((Countdown) timer).InitDate.ToString();
                            }
                        }
                        if (timer != null)
                        {
                            _timers.Add(timer.Name, timer);
                        }
                        else
                        {
                            Debug.LogError(Utils.DEBUG_PREFIX + "OnLoad: Cannot load timer: "+childNode);
                        }
                    }
                }
                
                // Triggers
                dataFound = node.TryGetValue(KEY_NB_TRIG, ref nbItem);
                if (dataFound)
                {
                    for (int i = 0; i < nbItem; i++)
                    {
                        dataFound = node.TryGetNode(KEY_PREF_TRIG + i, ref childNode);
                        
                        Trigger newTrigger = ConfigNode.CreateObjectFromConfig<Trigger>(childNode);
                        if (newTrigger != null)
                        {
                            newTrigger.OnLoad(childNode, this);
                            _triggers.Add(newTrigger);
                        }
                    }
                }
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
        
        public void LoadPersistentData()
        {
            _autopilot = new Autopilot(vessel);
            try {
                if ((_triggers != null) && (_triggers.Count > 0))
                {
                    foreach (Trigger trigger in _triggers)
                    {
                        trigger.LoadPersistentData();
                    }
                }
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
 
        protected override void OnSave(ConfigNode node)
        {
            try {
                ConfigNode childNode = null;
                // Timer
                // Remove previous timers
                node.RemoveNodesStartWith(KEY_PREF_TIMER);
                // Add new timers
                if ((_timers != null) && (_timers.Count > 0))
                {
                    int i = 0;
                    foreach (AbsTimer entry in _timers.Values)
                    {
                        childNode = ConfigNode.CreateConfigFromObject(entry);
                        if (childNode != null)
                        {
                            node.SetNode(KEY_PREF_TIMER + i, childNode, true);
                            i++;
                        }
                    }
                    node.SetValue(KEY_NB_TIMER, i, true);
                }
                
                // Triggers
                // Remove previous triggers
                node.RemoveNodesStartWith(KEY_PREF_TRIG);
                // Add new timers
                if ((_triggers != null) && (_triggers.Count > 0))
                {
                    int i = 0;
                    foreach (Trigger trigger in _triggers)
                    {
                        childNode = ConfigNode.CreateConfigFromObject(trigger);
                        if (childNode != null)
                        {
                            trigger.OnSave(childNode);
                            node.SetNode(KEY_PREF_TRIG + i, childNode, true);
                            i++;
                        }
                    }
                    node.SetValue(KEY_NB_TRIG, i, true);
                }
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
        
        public void Update()
        {
            // Called every frame
            foreach (Trigger trigger in _triggers)
            {
                trigger.DoTrigger();
            }
        }
        
        public void AddNewConfig()
        {
            _triggers.Add(new Trigger());
        }
        
        public void RemoveTrigger(Trigger trigger)
        {
            _triggers.Remove(trigger);
        }
        
        public bool AddTimer(string name)
        {
            if ((name != "") && !_timers.ContainsKey(name))
            {
                _timers.Add(name, new Timer(name));
                return true;
            }
            return false;
        }
        
        public bool AddCountdown(string name)
        {
            if ((name != "") && !_timers.ContainsKey(name))
            {
                _timers.Add(name, new Countdown(name));
                return true;
            }
            return false;
        }
        
        public AbsTimer GetTimer(string id)
        {
            AbsTimer instance;
            if (id.Equals("") || !_timers.TryGetValue(id, out instance))
            {
                return null;
            }
            return instance;
        }
    }
    
    public class Trigger
    {
        [Persistent(name="name")]
        public string Name;
        private TriggerEvent _event;
        public TriggerEvent TriggerEvent
        {
            get { return _event; }
            set { _event = value; }
        }
        private TriggerConditions _conditions;
        public TriggerConditions TriggerConditions
        {
            get { return _conditions; }
            set { _conditions = value; }
        }
        private TriggerActions _actions;
        public TriggerActions TriggerActions
        {
            get { return _actions; }
            set { _actions = value; }
        }
        
        public Trigger()
        {
            Name = "";
            _event = null;
            _conditions = new TriggerConditions();
            _actions = new TriggerActions();
        }

        public void DoTrigger()
        {
            // Execute every frame
            if ((_event == null) || (_actions.Count == 0))
            {
                return;
            }
            if (_event.EvaluateEvent())
            {
                if (_conditions.EvaluateCondition())
                {
                    _actions.DoAction();
                }
                else
                {
                    _event.Reset();
                }
            }
        }
        
        private const string KEY_EVENT          = "event";
        private const string KEY_NB_CONDITIONS  = "nbConditions";
        private const string KEY_PREF_CONDITION = "condition";
        private const string KEY_NB_ACTIONS     = "nbActions";
        private const string KEY_PREF_ACTION    = "action";
        
        public void OnLoad(ConfigNode node, VesselTriggers triggerConfig)
        {
            bool dataFound = false;
            ConfigNode childNode = null;
            int nbItem = 0;
            TriggerEventType eventType = (TriggerEventType) (-1);
            TriggerConditionType conditionType = (TriggerConditionType) (-1);
            TriggerActionType actionType = (TriggerActionType) (-1);
            // Event
            dataFound = node.TryGetNode(KEY_EVENT, ref childNode);
            if (dataFound)
            {
                dataFound = childNode.TryGetEnum<TriggerEventType>("type", ref eventType, (TriggerEventType) (-1));
                if (dataFound)
                {
                    _event = new TriggerEvent(eventType, triggerConfig);
                    if (_event != null)
                    {
                        ConfigNode.LoadObjectFromConfig(_event, childNode);
                    }
                }
            }
            // Condition
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
            // Actions
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
            // Event
            if (_event != null)
            {
                _event.LoadPersistentData();
            }
            // Condition
            _conditions.LoadPersistentData();
            // Actions
            _actions.LoadPersistentData();
        }
 
        public void OnSave(ConfigNode node)
        {
            ConfigNode childNode = null;
            // Event
            if (_event != null)
            {
                childNode = ConfigNode.CreateConfigFromObject(_event);
                if (childNode != null)
                {
                    node.SetNode(KEY_EVENT, childNode, true);
                }
            }
            // Condition
            int i = 0;
            foreach (TriggerCondition condition in _conditions.Conditions)
            {
                childNode = ConfigNode.CreateConfigFromObject(condition);
                if (childNode != null)
                {
                    node.SetNode(KEY_PREF_CONDITION + i, childNode, true);
                    i++;
                }
            }
            node.SetValue(KEY_NB_CONDITIONS, i, true);
            // Actions
            i = 0;
            foreach (TriggerAction action in _actions.Actions)
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
    }
}

