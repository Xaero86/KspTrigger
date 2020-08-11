using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace KspTrigger
{
    public class ImportExportHelper
    {
        private const string KEY_EXP_CONFIG        = "Config";
        private const string PREFIX_CONFIG_FILE    = "triggers_";
        private const string EXTENSION_CONFIG_FILE = ".cfg";
        
        public ImportExportHelper()
        {
            try {
                Directory.CreateDirectory(Utils.IMPORT_EXPORT_DIR);
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
        
        public string[] ImportableList
        {
            get {
                FileInfo[] importableFiles = null;
                List<string> result = new List<string>();
                try
                {
                    DirectoryInfo directory = new DirectoryInfo(Utils.IMPORT_EXPORT_DIR);
                    importableFiles = directory.GetFiles(PREFIX_CONFIG_FILE+"*"+EXTENSION_CONFIG_FILE);
                    foreach(FileInfo file in importableFiles)
                    {
                        string confName = file.Name;
                        confName = confName.Substring(PREFIX_CONFIG_FILE.Length);
                        confName = confName.Substring(0, confName.Length - EXTENSION_CONFIG_FILE.Length);
                        result.Add(confName);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
                }
                return result.ToArray();
            }
        }
        
        public bool Exists(string name)
        {
            return File.Exists(NameToPath(name));
        }
        
        private string NameToPath(string name)
        {
            return Path.Combine(Utils.IMPORT_EXPORT_DIR, PREFIX_CONFIG_FILE+name+EXTENSION_CONFIG_FILE);
        }
        
        public TriggerConfig ImportConfig(string name, VesselTriggers vesselTriggers)
        {
            try {
                TriggerConfig triggerConfig = null;
                bool dataFound = false;
                ConfigNode childNode = null;
                
                ConfigNode importedNode = ConfigNode.Load(NameToPath(name));
                if (importedNode != null)
                {
                    dataFound = importedNode.TryGetNode(KEY_EXP_CONFIG, ref childNode);
                    if (dataFound)
                    {
                        triggerConfig = new TriggerConfig();
                        dataFound = ConfigNode.LoadObjectFromConfig(triggerConfig, childNode);
                        if (dataFound)
                        {
                            triggerConfig.OnLoad(childNode, vesselTriggers);
                            triggerConfig.LoadPersistentData();
                            return triggerConfig;
                        }
                    }
                }
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
            return null;
        }
        
        public void ExportConfig(string name, TriggerConfig exportedConfig)
        {
            try {
                ConfigNode exportedNode = new ConfigNode("KspConfig");
                ConfigNode childNode = ConfigNode.CreateConfigFromObject(exportedConfig);
                if (childNode != null)
                {
                    exportedConfig.OnSave(childNode);
                    exportedNode.SetNode(KEY_EXP_CONFIG, childNode, true);
                    exportedNode.Save(NameToPath(name));
                }
            } catch (Exception e) {
                Debug.LogError(Utils.DEBUG_PREFIX + e.Message);
            }
        }
    }
}

