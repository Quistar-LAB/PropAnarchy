using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using EManagersLib.API;
using ICities;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using UnityEngine;

namespace PropAnarchy {
    public class PAModule : ILoadingExtension, IUserMod {
        private const string m_modName = "Prop Anarchy";
        private const string m_modDesc = "Extends the Prop Framework";
        internal const string m_modVersion = "0.3.2";
        internal const string m_AssemblyVersion = m_modVersion + ".*";
        private const string m_debugLogFile = "00PropAnarchyDebug.log";
        internal const string KeybindingConfigFile = "PropAnarchyKeyBindSetting";

        public static float PropLimitScale {
            get => EPropManager.PROP_LIMIT_SCALE;
            set => EPropManager.PROP_LIMIT_SCALE = value;
        }
        public static bool UsePropAnarchy {
            get => EPropManager.UsePropAnarchy;
            set => EPropManager.UsePropAnarchy = value;
        }
        /* Prop Snapping related */
        public static bool UsePropSnapping {
            get => EPropManager.UsePropSnapping;
            set => EPropManager.UsePropSnapping = value;
        }
        public static bool UsePropSnapToBuilding = true;
        public static bool UsePropSnapToNetwork = true;
        public static bool UsePropSnapToProp = true;
        public static bool UseDecalPropFix = true;

        public PAModule() {
            CreateDebugFile();
            try {
                if (GameSettings.FindSettingsFileByName(KeybindingConfigFile) == null) {
                    GameSettings.AddSettingsFile(new SettingsFile[] {
                        new SettingsFile() { fileName = KeybindingConfigFile }
                    });
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        #region UserMod
        public string Name => m_modName + " " + m_modVersion;
        public string Description => m_modDesc;
        public void OnEnabled() {
            LoadSettings();
        }
        public void OnDisabled() {
            SaveSettings();
        }
        public void OnSettingsUI(UIHelperBase helper) {
            SingletonLite<PALocale>.instance.Init();
            ((helper.AddGroup($"{m_modName} -- Version {m_modVersion}") as UIHelper).self as UIPanel).AddUIComponent<PAOptionPanel>();
        }
        #endregion UserMod

        #region LoadingExtension
        public void OnCreated(ILoading loading) {
            LoadSettings();
            OutputPluginsList();
        }

        public void OnReleased() {
            SaveSettings();
        }

        public void OnLevelLoaded(LoadMode mode) {
            bool nearlyEqual(float a, float b) {
                const float epsilon = 0.0001f;
                if (a == b) {
                    return true;
                } else if (Math.Abs(a - b) < epsilon) {
                    return true;
                }
                return false;
            }
            if (UseDecalPropFix) {
                const float rMarker = 12f / 255f;
                const float gMarker = 34f / 255f;
                const float bMarker = 56f / 255f;
                const float aMarker = 1f;
                uint prefabCount = (uint)PrefabCollection<PropInfo>.LoadedCount();
                for (uint i = 0; i < prefabCount; i++) {
                    PropInfo prefab = PrefabCollection<PropInfo>.GetLoaded(i);
                    if (prefab is null) continue;
                    if (!prefab.m_isDecal || prefab.m_material is null) continue;
                    Color color = prefab.m_material.GetColor("_ColorV0");
                    if (nearlyEqual(color.r, rMarker) && nearlyEqual(color.g, gMarker) && nearlyEqual(color.b, bMarker) && color.a == aMarker) {
                        Color colorV1 = prefab.m_material.GetColor("_ColorV1");
                        Color colorV2 = prefab.m_material.GetColor("_ColorV2");
                        Vector4 size = new Vector4(colorV1.r * 255, colorV1.g * 255, colorV1.b * 255, 0);
                        var tiling = new Vector4(colorV2.r * 255, 0, colorV2.b * 255, 0);
                        prefab.m_material.SetVector("_DecalSize", size);
                        prefab.m_material.SetVector("_DecalTiling", tiling);
                        prefab.m_lodMaterial.SetVector("_DecalSize", size);
                        prefab.m_lodMaterial.SetVector("_DecalTiling", tiling);
                        prefab.m_lodMaterialCombined.SetVector("_DecalSize", size);
                        prefab.m_lodMaterialCombined.SetVector("_DecalTiling", tiling);
                    }
                }
            }
            PLT.PropLineTool.InitializedPLT();
        }

        public void OnLevelUnloading() {
            PLT.PropLineTool.UnloadPLT();
        }
        #endregion LoadingExtension

        private const string SettingsFileName = "PropAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(SettingsFileName);
                PropLimitScale = float.Parse(xmlConfig.DocumentElement.GetAttribute("PropLimitScale"));
                UsePropAnarchy = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UsePropAnarchy"));
                UsePropSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UsePropSnapping"));
                UsePropSnapToBuilding = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToBuilding"));
                UsePropSnapToNetwork = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToNetwork"));
                UsePropSnapToProp = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToProp"));
                UseDecalPropFix = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseDecalPropFix"));
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        internal static void SaveSettings() {
            XmlDocument xmlConfig = new XmlDocument();
            XmlElement root = xmlConfig.CreateElement("PropAnarchyConfig");
            _ = root.Attributes.Append(AddElement(xmlConfig, "PropLimitScale", PropLimitScale));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UsePropAnarchy", UsePropAnarchy));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UsePropSnapping", UsePropSnapping));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UsePropSnapToBuilding", UsePropSnapToBuilding));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UsePropSnapToNetwork", UsePropSnapToNetwork));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UsePropSnapToProp", UsePropSnapToProp));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseDecalPropFix", UseDecalPropFix));
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
            using (FileStream debugFile = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine($"--- {m_modName} {m_modVersion} Debug File ---");
                sw.WriteLine(Environment.OSVersion);
                sw.WriteLine($"C# CLR Version {Environment.Version}");
                sw.WriteLine($"Unity Version {Application.unityVersion}");
                sw.WriteLine("-------------------------------------");
            }
        }

        private void OutputPluginsList() {
            using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine("Mods Installed are:");
                foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                    sw.WriteLine($"=> {info.name}-{(info.userModInstance as IUserMod).Name} {(info.isEnabled ? "** Enabled **" : "** Disabled **")}");
                }
                sw.WriteLine("-------------------------------------");
            }
        }

        internal static void PALog(string msg) {
            var ticks = profiler.ElapsedTicks;
            using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
            }
        }
    }
}
