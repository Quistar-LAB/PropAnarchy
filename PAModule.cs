using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using EManagersLib;
using ICities;
using PropAnarchy.AdditiveShader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using UI;
using UnityEngine;

namespace PropAnarchy {
    public class PAModule : ILoadingExtension, IUserMod {
        private const string m_modName = @"Prop Anarchy";
        private const string m_modDesc = @"Extends the Prop Framework";
        internal const string m_modVersion = @"0.4.9";
        internal const string m_AssemblyVersion = m_modVersion + @".*";
        private const string m_debugLogFile = @"00PropAnarchyDebug.log";
        internal const string KeybindingConfigFile = @"PropAnarchyKeyBindSetting";

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
        public static bool UseAdditiveShader = true;

        public static bool ShowIndicators = true;

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
        public string Name => m_modName + ' ' + m_modVersion;
        public string Description => m_modDesc;
        public void OnEnabled() {
            LoadSettings();
            PALocale.Init();
        }
        public void OnDisabled() {
            SaveSettings();
        }
        public void OnSettingsUI(UIHelperBase helper) {
            PALocale.OnLocaleChanged();
            LocaleManager.eventLocaleChanged += PALocale.OnLocaleChanged;
            ((helper.AddGroup(m_modName + @" -- Version " + m_modVersion) as UIHelper).self as UIPanel).AddUIComponent<PAOptionPanel>();
        }
        #endregion UserMod

        #region LoadingExtension
        public void OnCreated(ILoading loading) {
            OutputPluginsList();
        }

        public void OnReleased() { }

        public void UpdateCustomPrefabs(object _) {
            List<ManagedAsset> assets = new List<ManagedAsset>();
            try {
                PrefabInfo[] prefabs = Resources.FindObjectsOfTypeAll<PrefabInfo>();
                int prefabLen = prefabs.Length;
                for (int i = 0; i < prefabLen; i++) {
                    PrefabInfo prefab = prefabs[i];
                    if (prefab is PropInfo prop) {
                        DecalPropFix.AssignFix(prop);
                        if (prop.m_mesh && prop.m_mesh.name is string propData && AdditiveShaderManager.HasValidData(propData)) {
                            assets.Add(new ManagedAsset(prop));
                            PALog(@"[AdditiveShader] : Loaded a prop - " + prop.name + @" marked as having the AdditiveShader");
                        }
                    } else if (prefab is BuildingInfo building) {
                        if (building.m_mesh && AdditiveShaderManager.HasValidData(building.m_mesh.name)) {
                            assets.Add(new ManagedAsset(building));
                            PALog(@"[AdditiveShader] : Loaded a building - " + building.name + @" marked as having the AdditiveShader");
                        }
                        if (!(building.m_props is null) && AdditiveShaderManager.ContainsShaderProps(building)) {
                            assets.Add(new ManagedAsset(building, true));
                        }
                    } else if (prefab is BuildingInfoSub buildingSub && buildingSub.m_mesh &&
                               buildingSub.m_mesh.name is string buildingSubData && AdditiveShaderManager.HasValidData(buildingSubData)) {
                        assets.Add(new ManagedAsset(buildingSub));
                        PALog(@"[AdditiveShader] : Loaded a building sub - " + buildingSub.name + @" marked as having the AdditiveShader");
                    } else if (prefab is VehicleInfoSub vehicleSub && vehicleSub.m_mesh &&
                               vehicleSub.m_mesh.name is string vehicleSubData && AdditiveShaderManager.HasValidData(vehicleSubData)) {
                        assets.Add(new ManagedAsset(vehicleSub));
                        PALog(@"[AdditiveShader] : Loaded a vehicle sub - " + vehicleSub.name + @" mesh marked as having the AdditiveShader");
                    }
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            } finally {
                if (assets.Count > 0) {
                    AdditiveShaderManager.m_managedAssets = assets.ToArray();
                    foreach (var asset in assets) {
                        if (asset.IsContainer || asset.Profile.IsStatic) {
                            asset.SetVisible(true);
                        } else {
                            asset.SetVisible(false);
                        }
                    }
                    Singleton<AdditiveShaderManager>.instance.StartCoroutine(@"AdditiveShaderThread");
                    AdditiveShaderManager.RefreshRenderGroups();
                }
            }
        }

        public void OnLevelLoaded(LoadMode mode) {
            if (ShowIndicators) {
                UIIndicator indicatorPanel = UIIndicator.Setup();
                if (indicatorPanel) {
                    UIIndicator.UIIcon propSnap = default;
                    propSnap = indicatorPanel.AddSnappingIcon(PALocale.GetLocale(@"PropSnapIsOn"), PALocale.GetLocale(@"PropSnapIsOff"), UsePropSnapping, (_, p) => {
                        bool state = UsePropSnapping = !UsePropSnapping;
                        propSnap.State = state;
                        PAOptionPanel.SetPropSnapState(state);
                        ThreadPool.QueueUserWorkItem(SaveSettings);
                    }, out bool finalState);
                    if (finalState != UsePropSnapping) {
                        UsePropSnapping = finalState;
                        propSnap.State = finalState;
                    }
                    UIIndicator.UIIcon propAnarchy = default;
                    propAnarchy = indicatorPanel.AddAnarchyIcon(PALocale.GetLocale(@"PropAnarchyIsOn"), PALocale.GetLocale(@"PropAnarchyIsOff"), UsePropAnarchy, (_, p) => {
                        bool state = UsePropAnarchy = !UsePropAnarchy;
                        propAnarchy.State = state;
                        PAOptionPanel.SetPropAnarchyState(state);
                        ThreadPool.QueueUserWorkItem(SaveSettings);
                    }, out finalState);
                    if (finalState != UsePropAnarchy) {
                        UsePropAnarchy = finalState;
                        propAnarchy.State = finalState;
                    }
                }
            }
            PAOptionPanel.UpdateState(true);
            // Initialize Additive Shader Manager
            Singleton<AdditiveShaderManager>.Ensure();
            ThreadPool.QueueUserWorkItem(UpdateCustomPrefabs); // This thread handles initialization of Additive Shader asset and Decal Prop Fix

            PLT.PropLineTool.InitializedPLT();
            // The original mods created a new GameObject for running additive shader routines. I'm opting
            // to just use existing GameObject and add a coroutine so it doesn't stress Update()
        }

        public void OnLevelUnloading() {
            PLT.PropLineTool.UnloadPLT();
            Singleton<AdditiveShaderManager>.instance.StopCoroutine(@"AdditiveShaderThread");
            AdditiveShaderManager.m_managedAssets = null;
        }
        #endregion LoadingExtension

        private const string SettingsFileName = @"PropAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(SettingsFileName);
                PropLimitScale = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"PropLimitScale"));
                UsePropAnarchy = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UsePropAnarchy"));
                UsePropSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UsePropSnapping"));
                UsePropSnapToBuilding = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToBuilding"));
                UsePropSnapToNetwork = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToNetwork"));
                UsePropSnapToProp = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToProp"));
                UseDecalPropFix = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseDecalPropFix"));
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        internal static void SaveSettings(object _ = null) {
            XmlDocument xmlConfig = new XmlDocument();
            XmlElement root = xmlConfig.CreateElement(@"PropAnarchyConfig");
            _ = root.Attributes.Append(AddElement(xmlConfig, @"PropLimitScale", PropLimitScale));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UsePropAnarchy", UsePropAnarchy));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UsePropSnapping", UsePropSnapping));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UsePropSnapToBuilding", UsePropSnapToBuilding));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UsePropSnapToNetwork", UsePropSnapToNetwork));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UsePropSnapToProp", UsePropSnapToProp));
            _ = root.Attributes.Append(AddElement(xmlConfig, @"UseDecalPropFix", UseDecalPropFix));
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
                sw.WriteLine(@"--- " + m_modName + ' ' + m_modVersion + @" Debug File ---");
                sw.WriteLine(Environment.OSVersion);
                sw.WriteLine(@"C# CLR Version " + Environment.Version);
                sw.WriteLine(@"Unity Version " + Application.unityVersion);
                sw.WriteLine(@"-------------------------------------");
            }
        }

        private void OutputPluginsList() {
            using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine(@"Mods Installed are:");
                foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                    sw.WriteLine(@"=> " + info.name + '-' + (info.userModInstance as IUserMod).Name + ' ' + (info.isEnabled ? @"** Enabled **" : @"** Disabled **"));
                }
                sw.WriteLine(@"-------------------------------------");
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
