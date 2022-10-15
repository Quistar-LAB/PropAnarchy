using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using EManagersLib;
using ICities;
using PropAnarchy.AdditiveShader;
using PropAnarchy.TransparencyLODFix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using UI;
using UnityEngine;

namespace PropAnarchy {
    public sealed class PAModule : ILoadingExtension, IUserMod {
        private const string m_modName = @"Prop Anarchy (temp fix by algernon)";
        private const string m_modDesc = @"Extends the Prop Framework";
        internal const string m_modVersion = @"0.7.5.1";
        internal const string m_AssemblyVersion = m_modVersion + @".*";
        private const string m_debugLogFile = @"00PropAnarchyDebug.log";
        internal const string KeybindingConfigFile = @"PropAnarchyKeyBindSetting";
        internal static bool IsInGame = false;

        public static float PropLimitScale {
            get => EPropManager.PROP_LIMIT_SCALE;
            set {
                if (EPropManager.PROP_LIMIT_SCALE != value) {
                    EPropManager.PROP_LIMIT_SCALE = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }
        public static bool UsePropAnarchy {
            get => EPropManager.UsePropAnarchy;
            set {
                if (EPropManager.UsePropAnarchy != value) {
                    EPropManager.UsePropAnarchy = value;
                    UIIndicator.AnarchyIndicator?.SetState(value);
                    if (PAOptionPanel.m_propAnarchyCB) PAOptionPanel.m_propAnarchyCB.isChecked = value;
                }
            }
        }
        /* Prop Snapping related */
        public static bool UsePropSnapping {
            get => EPropManager.UsePropSnapping;
            set {
                if (EPropManager.UsePropSnapping != value) {
                    EPropManager.UsePropSnapping = value;
                    UIIndicator.SnapIndicator?.SetState(value);
                    if (PAOptionPanel.m_propSnappingCB) PAOptionPanel.m_propSnappingCB.isChecked = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        #region UserMod
        public string Name => m_modName + ' ' + m_modVersion;
        public string Description => m_modDesc;
        public void OnEnabled() {
            try {
                CreateDebugFile();
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
            try {
                if (GameSettings.FindSettingsFileByName(KeybindingConfigFile) is null) {
                    GameSettings.AddSettingsFile(new SettingsFile[] {
                        new SettingsFile() { fileName = KeybindingConfigFile }
                    });
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
            PALocale.Init();
            for (int loadTries = 0; loadTries < 2; loadTries++) {
                if (LoadSettings()) break; // Try 2 times, and if still fails, then use default settings
            }
            HarmonyHelper.DoOnHarmonyReady(PAPatcher.EnablePatches);
        }
        public void OnDisabled() {
            SaveSettings();
            if (HarmonyHelper.IsHarmonyInstalled) PAPatcher.DisablePatches();
        }
        public void OnSettingsUI(UIHelperBase helper) {
            PLT.PropLineTool.InitializeTextures();
            PALocale.OnLocaleChanged();
            LocaleManager.eventLocaleChanged += PALocale.OnLocaleChanged;
            PAOptionPanel.SetupPanel((helper.AddGroup(m_modName + @" -- Version " + m_modVersion) as UIHelper).self as UIPanel);
        }
        #endregion UserMod

        #region LoadingExtension
        public void OnCreated(ILoading loading) {
            OutputPluginsList();
            PAPatcher.AttachMoveItPostProcess();
        }

        public void OnReleased() {
        }

        public void UpdateCustomPrefabs() {
            List<ManagedAsset> assets = new List<ManagedAsset>();
            List<PrefabInfo> rotorShaderPrefabs = new List<PrefabInfo>();
            try {
                PrefabInfo[] prefabs = Resources.FindObjectsOfTypeAll<PrefabInfo>();
                int prefabLen = prefabs.Length;
                for (int i = 0; i < prefabLen; i++) {
                    PrefabInfo prefab = prefabs[i];
                    if (prefab is PropInfo prop) {
                        /* Decal prop fix routine added here to prevent re-allocating new enumerator for all prefabinfo types */
                        DecalPropFix.AssignFix(prop); /* DECAL PROP FIX ROUTINE */
                        if (prop.m_mesh && prop.m_mesh.name is string propData && AdditiveShaderManager.HasValidData(propData)) {
                            assets.Add(new ManagedAsset(prop));
                            PALog(@"[AdditiveShader] : Loaded a prop - " + prop.name + @" marked as having the AdditiveShader");
                        }
                        prop.CheckRotorSignature(rotorShaderPrefabs); // Transparency LOD Fix Routine
                    } else if (prefab is BuildingInfo building) {
                        if (building.m_mesh && AdditiveShaderManager.HasValidData(building.m_mesh.name)) {
                            assets.Add(new ManagedAsset(building));
                            PALog(@"[AdditiveShader] : Loaded a building - " + building.name + @" marked as having the AdditiveShader");
                        }
                        if (!(building.m_props is null) && AdditiveShaderManager.ContainsShaderProps(building)) {
                            assets.Add(new ManagedAsset(building, true));
                        }
                        building.CheckRotorSignature(rotorShaderPrefabs); // Transparency LOD Fix Routine
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
                    ManagedAsset[] managedAssets = assets.ToArray();
                    int assetsLen = managedAssets.Length;
                    AdditiveShaderManager.m_managedAssets = managedAssets;
                    for (int i = 0; i < assetsLen; i++) {
                        if (assets[i].IsContainer || assets[i].Profile.IsStatic) {
                            assets[i].SetVisible(true);
                        } else {
                            assets[i].SetVisible(false);
                        }
                    }
                    // Adding Additive Shader thread as a coroutine into UIView. This saves valuable resources
                    // instead of creating another gameobject to do mundane work
                    UIView.GetAView().StartCoroutine(AdditiveShaderManager.AdditiveShaderThread());
                    // Now process Transparency LOD fix
                    TransparencyLODFix.TransparencyLODFix.m_rotorShaderPrefabs = rotorShaderPrefabs.ToArray();
                }
            }
        }

        public void OnLevelLoaded(LoadMode mode) {
            IsInGame = true;
            PAOptionPanel.UpdateState(true);
            if (Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.AssetEditor) {
                UIIndicator indicatorPanel = UIIndicator.Setup();
                if (indicatorPanel) {
                    UIIndicator.UIIcon propSnap = default;
                    propSnap = indicatorPanel.AddSnappingIcon(PALocale.GetLocale(@"PropSnapIsOn"), PALocale.GetLocale(@"PropSnapIsOff"), UsePropSnapping, (_, p) => {
                        UsePropSnapping = !UsePropSnapping;
                    }, out bool finalState);
                    if (finalState != UsePropSnapping) {
                        UsePropSnapping = finalState;
                        propSnap.State = finalState;
                    }
                    UIIndicator.UIIcon propAnarchy = default;
                    propAnarchy = indicatorPanel.AddAnarchyIcon(PALocale.GetLocale(@"PropAnarchyIsOn"), PALocale.GetLocale(@"PropAnarchyIsOff"), UsePropAnarchy, (_, p) => {
                        UsePropAnarchy = !UsePropAnarchy;
                    }, out finalState);
                    if (finalState != UsePropAnarchy) {
                        UsePropAnarchy = finalState;
                        propAnarchy.State = finalState;
                    }
                }
            }

            // Fix for prop snapping issues if buffer is at the standard size.
            // Since we can't recreate the original height, we clear the fixed height flag instead to at least restore them to terrain height (from 0).
            EPropInstance[] propBuffer = EPropManager.m_props.m_buffer;
            for (int i = 0; i < propBuffer.Length; i++) {
                if (propBuffer[i].m_posY == 0) {
                    propBuffer[i].m_flags &= ~(ushort)EPropInstance.Flags.FixedHeight & 0xFFFF;
                }
            }

            // The original mods created a new GameObject for running additive shader routines. I'm opting
            // to just use existing GameObject and add a coroutine so it doesn't stress Update()
            UpdateCustomPrefabs(); // This thread handles initialization of Additive Shader asset and Decal Prop Fix
            UIDropDown levelOfDetail = UIView.GetAView().FindUIComponent<UIDropDown>("LevelOfDetail");
            levelOfDetail.eventSelectedIndexChanged += (c, val) => TransparencyLODFix.TransparencyLODFix.Update();
            UIComponent optionsPanel = UIView.library.Get<UIPanel>("OptionsPanel");
            optionsPanel.eventVisibilityChanged += (c, isVisible) => TransparencyLODFix.TransparencyLODFix.Update();
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            PLT.PropLineTool.InitializedPLT(mode);
            PAPainter.Initialize(smInstance);
        }

        public void OnLevelUnloading() {
            IsInGame = false;
            PAOptionPanel.UpdateState(false);
            PLT.PropLineTool.UnloadPLT();
            UIView.GetAView().StopCoroutine(AdditiveShaderManager.AdditiveShaderThread());
            AdditiveShaderManager.m_managedAssets = null;
        }
        #endregion LoadingExtension

        private const string SettingsFileName = @"PropAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument {
                    XmlResolver = null
                };
                xmlConfig.Load(SettingsFileName);
                EPropManager.PROP_LIMIT_SCALE = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"PropLimitScale"), System.Globalization.NumberStyles.Float);
                EPropManager.UsePropSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UsePropSnapping"));
                PLT.Settings.m_controlMode = (PLT.PropLineTool.ControlMode)int.Parse(xmlConfig.DocumentElement.GetAttribute(@"ControlMode"));
                PLT.Settings.m_angleMode = (PLT.PropLineTool.AngleMode)int.Parse(xmlConfig.DocumentElement.GetAttribute(@"AngleMode"));
                PLT.Settings.m_angleFlip180 = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"AngleFlip180"));
                PLT.Settings.m_showUndoPreview = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"ShowUndoPreviews"));
                PLT.Settings.m_perfectCircles = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"PerfectCircles"));
                PLT.Settings.m_linearFenceFill = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"LinearFenceFill"));
                PLT.Settings.m_useMeshCenterCorrection = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseMeshCenterCorrection"));
                PLT.Settings.m_verticalLayout = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"VerticalLayout"));
                PLT.Settings.m_optionXPos = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"PLTOptionXPos"), System.Globalization.NumberStyles.Float);
                PLT.Settings.m_optionYPos = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"PLTOptionYPos"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_hideClouds = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"HideClouds"));
                TransparencyLODFix.Settings.m_lodFactorMultiplierProps = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"LodFactorMultiplierProps"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_distanceOffsetProps = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"DistanceOffsetProps"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_lodDistanceMultiplierProps = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"LodDistanceMultiplierProps"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_fallbackRenderDistanceProps = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"FallbackRenderDistanceProps"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_lodFactorMultiplierBuildings = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"LodFactorMultiplierBuildings"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_distanceOffsetBuildings = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"DistanceOffsetBuildings"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_lodDistanceMultiplierBuildings = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"LodDistanceMultiplierBuildings"), System.Globalization.NumberStyles.Float);
                TransparencyLODFix.Settings.m_fallbackRenderDistanceBuildings = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"FallbackRenderDistanceBuildings"), System.Globalization.NumberStyles.Float);
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        private static readonly object settingsLock = new object();
        internal static void SaveSettings(object _ = null) {
            Monitor.Enter(settingsLock);
            try {
                XmlDocument xmlConfig = new XmlDocument {
                    XmlResolver = null
                };
                XmlElement root = xmlConfig.CreateElement(@"PropAnarchyConfig");
                root.Attributes.Append(AddElement(xmlConfig, @"PropLimitScale", PropLimitScale));
                root.Attributes.Append(AddElement(xmlConfig, @"UsePropSnapping", UsePropSnapping));
                root.Attributes.Append(AddElement(xmlConfig, @"ControlMode", (int)PLT.Settings.m_controlMode));
                root.Attributes.Append(AddElement(xmlConfig, @"AngleMode", (int)PLT.Settings.m_angleMode));
                root.Attributes.Append(AddElement(xmlConfig, @"AngleFlip180", PLT.Settings.m_angleFlip180));
                root.Attributes.Append(AddElement(xmlConfig, @"ShowUndoPreviews", PLT.Settings.m_showUndoPreview));
                root.Attributes.Append(AddElement(xmlConfig, @"PerfectCircles", PLT.Settings.m_perfectCircles));
                root.Attributes.Append(AddElement(xmlConfig, @"LinearFenceFill", PLT.Settings.m_linearFenceFill));
                root.Attributes.Append(AddElement(xmlConfig, @"UseMeshCenterCorrection", PLT.Settings.m_useMeshCenterCorrection));
                root.Attributes.Append(AddElement(xmlConfig, @"VerticalLayout", PLT.Settings.m_verticalLayout));
                root.Attributes.Append(AddElement(xmlConfig, @"PLTOptionXPos", PLT.Settings.m_optionXPos));
                root.Attributes.Append(AddElement(xmlConfig, @"PLTOptionYPos", PLT.Settings.m_optionYPos));
                root.Attributes.Append(AddElement(xmlConfig, @"HideClouds", TransparencyLODFix.Settings.m_hideClouds));
                root.Attributes.Append(AddElement(xmlConfig, @"LodFactorMultiplierProps", TransparencyLODFix.Settings.m_lodFactorMultiplierProps));
                root.Attributes.Append(AddElement(xmlConfig, @"DistanceOffsetProps", TransparencyLODFix.Settings.m_distanceOffsetProps));
                root.Attributes.Append(AddElement(xmlConfig, @"LodDistanceMultiplierProps", TransparencyLODFix.Settings.m_lodDistanceMultiplierProps));
                root.Attributes.Append(AddElement(xmlConfig, @"FallbackRenderDistanceProps", TransparencyLODFix.Settings.m_fallbackRenderDistanceProps));
                root.Attributes.Append(AddElement(xmlConfig, @"LodFactorMultiplierBuildings", TransparencyLODFix.Settings.m_lodFactorMultiplierBuildings));
                root.Attributes.Append(AddElement(xmlConfig, @"DistanceOffsetBuildings", TransparencyLODFix.Settings.m_distanceOffsetBuildings));
                root.Attributes.Append(AddElement(xmlConfig, @"LodDistanceMultiplierBuildings", TransparencyLODFix.Settings.m_lodDistanceMultiplierBuildings));
                root.Attributes.Append(AddElement(xmlConfig, @"FallbackRenderDistanceBuildings", TransparencyLODFix.Settings.m_fallbackRenderDistanceBuildings));
                xmlConfig.AppendChild(root);
                xmlConfig.Save(SettingsFileName);
            } finally {
                Monitor.Exit(settingsLock);
            }
        }

        internal static XmlAttribute AddElement<T>(XmlDocument doc, string name, T t) {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = t.ToString();
            return attr;
        }

        private static readonly Stopwatch profiler = new Stopwatch();
        private static readonly object fileLock = new object();
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
            Monitor.Enter(fileLock);
            try {
                using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None))
                using (StreamWriter sw = new StreamWriter(debugFile)) {
                    sw.WriteLine(@"Mods Installed are:");
                    foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                        if (!(info is null) && info.userModInstance is IUserMod modInstance)
                            sw.WriteLine(@"=> " + info.name + '-' + modInstance.Name + ' ' + (info.isEnabled ? @"** Enabled **" : @"** Disabled **"));
                    }
                    sw.WriteLine(@"-------------------------------------");
                }
            } finally {
                Monitor.Exit(fileLock);
            }
        }

        internal static void PALog(string msg) {
            var ticks = profiler.ElapsedTicks;
            Monitor.Enter(fileLock);
            try {
                using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append))
                using (StreamWriter sw = new StreamWriter(debugFile)) {
                    sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
                }
            } finally {
                Monitor.Exit(fileLock);
            }
        }
    }
}
