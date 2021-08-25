using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CitiesHarmony.API;
using UnityEngine;
using ICities;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using System.Diagnostics;
using System.IO;

namespace PropAnarchy {
    public class PAModule : ILoadingExtension, IUserMod {
        private const string m_modName = "Prop Anarchy";
        private const string m_modDesc = "Extends the Prop Framework";
        internal const string m_modVersion = "0.1.0";
        internal const string m_AssemblyVersion = m_modVersion + ".*";
        private const string m_debugLogFile = "00PropAnarchyDebug.log";
        internal const string KeybindingConfigFile = "PropAnarchyKeyBindSetting";

        public const uint DefaultPropLimit = 65536u;
        public const int DefaultUpdatedPropLimit = 1024;
        public static bool UseCustomPropLimit = false;
        public static float PropScaleFactor = 3f;
        public static uint MaxPropLimit {
            get => (uint)(DefaultPropLimit * PropScaleFactor);
        }
        public static int MaxUpdatedPropLimit {
            get => (int)(DefaultUpdatedPropLimit * PropScaleFactor);
        }

        public PAModule() {
            CreateDebugFile();
            try {
                GameSettings.AddSettingsFile(new SettingsFile[] {
                    new SettingsFile() { fileName = KeybindingConfigFile }
                });
                /*
                if (GameSettings.FindSettingsFileByName(KeybindingConfigFile) == null) {
                    GameSettings.AddSettingsFile(new SettingsFile[] {
                        new SettingsFile() { fileName = KeybindingConfigFile }
                    });
                }*/
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        #region UserMod
        public string Name => m_modName + " " + m_modVersion;
        public string Description => m_modDesc;
        public void OnEnabled() {
        }
        public void OnDisabled() {

        }
        public void OnSettingsUI(UIHelperBase helper) {
            SingletonLite<PALocale>.instance.Init();
            ((helper.AddGroup($"{m_modName} -- Version {m_modVersion}") as UIHelper).self as UIPanel).AddUIComponent<PAOptionPanel>();
        }
        #endregion UserMod

        #region LoadingExtension
        public void OnCreated(ILoading loading) {
            OutputPluginsList();
        }

        public void OnLevelLoaded(LoadMode mode) {
            
        }

        public void OnLevelUnloading() {
            
        }

        public void OnReleased() {
            
        }
        #endregion LoadingExtension

        private const string SettingsFileName = "PropAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new();
                xmlConfig.Load(SettingsFileName);
                UseCustomPropLimit = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseCustomPropLimit"));
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        internal static void SaveSettings() {
            XmlDocument xmlConfig = new();
            XmlElement root = xmlConfig.CreateElement("PropAnarchyConfig");
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseCustomPropLimit", UseCustomPropLimit));
            xmlConfig.AppendChild(root);
            xmlConfig.Save(SettingsFileName);
        }

        private static XmlAttribute AddElement<T>(XmlDocument doc, string name, T t) {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = t.ToString();
            return attr;
        }

        private static readonly Stopwatch profiler = new Stopwatch();
        private void CreateDebugFile() {
            profiler.Start();
            /* Create Debug Log File */
            string path = Path.Combine(Application.dataPath, m_debugLogFile);
            using FileStream debugFile = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using StreamWriter sw = new(debugFile); sw.WriteLine($"--- {m_modName} {m_modVersion} Debug File ---");
            sw.WriteLine(Environment.OSVersion);
            sw.WriteLine($"C# CLR Version {Environment.Version}");
            sw.WriteLine($"Unity Version {Application.unityVersion}");
            sw.WriteLine("-------------------------------------");
        }

        private void OutputPluginsList() {
            using FileStream debugFile = new(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None);
            using StreamWriter sw = new(debugFile);
            sw.WriteLine("Mods Installed are:");
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                sw.WriteLine($"=> {info.name}-{(info.userModInstance as IUserMod).Name} {(info.isEnabled ? "** Enabled **" : "** Disabled **")}");
            }
            sw.WriteLine("-------------------------------------");
        }

        internal static void PALog(string msg) {
            var ticks = profiler.ElapsedTicks;
            using FileStream debugFile = new(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append);
            using StreamWriter sw = new(debugFile);
            sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
        }
    }
}
