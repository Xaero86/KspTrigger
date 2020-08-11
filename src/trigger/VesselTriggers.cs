using System;
using System.Collections.Generic;

namespace KspTrigger
{
    public class VesselTriggers : VesselModule
    {
        private List<TriggerConfig> _triggerConfigs = new List<TriggerConfig>();
        private int _configIndex;
        public int ConfigIndex
        {
            get { return _configIndex; }
            set { if ((value >= 0) && (value < _triggerConfigs.Count)) { _configIndex = value; } }
        }
        public string[] ConfigList
        {
            get
            {
                string[] configList = new string[_triggerConfigs.Count];
                int i = 0;
                foreach (TriggerConfig triggerConfig in _triggerConfigs)
                {
                    configList[i] = triggerConfig.Name;
                    i++;
                }
                return configList;
            }
        }
        
        private ImportExportHelper _impExpHelp = new ImportExportHelper();
        
        public List<Trigger> Triggers
        {
            get { return _triggerConfigs[ConfigIndex].Triggers; }
        }
        public Dictionary<string,AbsTimer> Timers
        {
            get { return _triggerConfigs[ConfigIndex].Timers; }
        }
        
        public string CurrentName
        {
            get { return _triggerConfigs[ConfigIndex].Name; }
            set { _triggerConfigs[ConfigIndex].Name = value; }
        }
        
        private Autopilot _autopilot;
        public Autopilot Autopilot { get { return _autopilot; } }
        
        private const string KEY_PREFIX      = "KAT_";
        private const string KEY_NB_CONFIGS  = KEY_PREFIX+"nbConfigs";
        private const string KEY_PREF_CONFIG = KEY_PREFIX+"config";
        private const string KEY_CURR_CONFIG = KEY_PREFIX+"currentConfig";
        
        protected override void OnLoad(ConfigNode node)
        {
            bool dataFound = false;
            ConfigNode childNode = null;
            int nbItem = 0;
            
            _triggerConfigs = new List<TriggerConfig>();
            _configIndex = 0;
            dataFound = node.TryGetValue(KEY_NB_CONFIGS, ref nbItem);
            if (dataFound)
            {
                for (int i = 0; i < nbItem; i++)
                {
                    TriggerConfig triggerConfig = null;
                    dataFound = node.TryGetNode(KEY_PREF_CONFIG + i, ref childNode);
                    if (dataFound)
                    {
                        triggerConfig = new TriggerConfig();
                        dataFound = ConfigNode.LoadObjectFromConfig(triggerConfig, childNode);
                        if (dataFound)
                        {
                            triggerConfig.OnLoad(childNode, this);
                            _triggerConfigs.Add(triggerConfig);
                        }
                    }
                }
            }
            if (_triggerConfigs.Count == 0)
            {
                _triggerConfigs.Add(new TriggerConfig());
            }
            dataFound = node.TryGetValue(KEY_CURR_CONFIG, ref nbItem);
            if (dataFound)
            {
                ConfigIndex = nbItem;
            }
        }
        
        public void LoadPersistentData()
        {
            _autopilot = new Autopilot(vessel);
            foreach (TriggerConfig triggerConfig in _triggerConfigs)
            {
                triggerConfig.LoadPersistentData();
            }
        }
        
        protected override void OnSave(ConfigNode node)
        {
            ConfigNode childNode = null;
            int i = 0;
            foreach (TriggerConfig triggerConfig in _triggerConfigs)
            {
                childNode = ConfigNode.CreateConfigFromObject(triggerConfig);
                if (childNode != null)
                {
                    triggerConfig.OnSave(childNode);
                    node.SetNode(KEY_PREF_CONFIG + i, childNode, true);
                    i++;
                }
            }
            node.SetValue(KEY_NB_CONFIGS, i, true);
            node.SetValue(KEY_CURR_CONFIG, ConfigIndex, true);
        }
        
        public void Update()
        {
            // Called every frame
            _triggerConfigs[ConfigIndex].Update();
        }
        
        public void AddNewConfig(string name)
        {
            _triggerConfigs.Add(new TriggerConfig(name));
            // Select new
            ConfigIndex = _triggerConfigs.Count-1;
        }
        
        public void RenameCurrent(string name)
        {
            _triggerConfigs[ConfigIndex].Name = name;
        }
        
        public void ImportConfig(string name)
        {
            TriggerConfig triggerConfig = _impExpHelp.ImportConfig(name, this);
            if (triggerConfig != null)
            {
                _triggerConfigs.Add(triggerConfig);
                // Select imported
                ConfigIndex = _triggerConfigs.Count-1;
            }
        }
        
        public bool ExportConfig(string name, bool erase)
        {
            if (!erase && _impExpHelp.Exists(name))
            {
                return false;
            }
            _impExpHelp.ExportConfig(name, _triggerConfigs[ConfigIndex]);
            return true;
        }
        
        public string[] ImportableList { get { return _impExpHelp.ImportableList;} }
        
        public void RemoveCurrent()
        {
            _triggerConfigs.RemoveAt(ConfigIndex);
            if (_triggerConfigs.Count == 0)
            {
                _triggerConfigs.Add(new TriggerConfig());
                ConfigIndex = 0;
            }
            if (_triggerConfigs.Count >= ConfigIndex)
            {
                ConfigIndex = _triggerConfigs.Count-1;
            }
        }

        public void AddNewTrigger()
        {
            _triggerConfigs[ConfigIndex].AddNewTrigger();
        }
        
        public void RemoveTrigger(Trigger trigger)
        {
            _triggerConfigs[ConfigIndex].RemoveTrigger(trigger);
        }
        
        public bool AddTimer(string name)
        {
            return _triggerConfigs[ConfigIndex].AddTimer(name);
        }
        
        public bool AddCountdown(string name)
        {
            return _triggerConfigs[ConfigIndex].AddCountdown(name);
        }
        
        public void RemoveTimer(string name)
        {
            _triggerConfigs[ConfigIndex].RemoveTimer(name);
        }
        
        public AbsTimer GetTimer(string name)
        {
            return _triggerConfigs[ConfigIndex].GetTimer(name);
        }
    }
}

