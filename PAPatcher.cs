using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Reflection;

namespace PropAnarchy {
    internal partial class PAPatcher : SingletonLite<PAPatcher> {
        private const string HARMONYID = @"quistar.propanarchy.mod";
        private Harmony m_harmony;
        private Harmony CurrentHarmony {
            get => (m_harmony is null) ? m_harmony = new Harmony(HARMONYID) : m_harmony;
        }
        private struct ModInfo {
            public readonly ulong fileID;
            public readonly string name;
            public ModInfo(ulong modID, string modName) {
                fileID = modID;
                name = modName;
            }
        }
        private static readonly ModInfo[] IncompatibleMods = new ModInfo[] {
            //new ModInfo(455403039, "Unlimited Trees Mod"),
        };

        internal static bool IsPluginExists(ulong id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.publishedFileID.AsUInt64 == id || info.ToString().Contains(name)) {
                    if (info.isEnabled) return true;
                }
            }
            foreach (var mod in PlatformService.workshop.GetSubscribedItems()) {
                for (int i = 0; i < IncompatibleMods.Length; i++) {
                    if (mod.AsUInt64 == id) {
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool CheckIncompatibleMods() {
            string errorMsg = "";
            foreach (var mod in PlatformService.workshop.GetSubscribedItems()) {
                for (int i = 0; i < IncompatibleMods.Length; i++) {
                    if (mod.AsUInt64 == IncompatibleMods[i].fileID) {
                        errorMsg += $"[{IncompatibleMods[i].name}] detected\n";
                        PAModule.PALog($"Incompatible mod: [{IncompatibleMods[i].name}] detected");
                    }
                }
            }
            if (errorMsg.Length > 0) {
                UIView.ForwardException(new Exception("Tree Anarchy detected incompatible mods, please remove the following mentioned mods", new Exception("\n" + errorMsg)));
                PAModule.PALog($"Tree Anarchy detected incompatible mods, please remove the following mentioned mods\n{errorMsg}");
                return false;
            }
            return true;
        }

        internal void EnableCore() {
            Harmony harmony = CurrentHarmony;
            EnableLimitPatches(harmony);
        }

        internal void DisableCore() {
            Harmony harmony = CurrentHarmony;
            DisableLimitPatches(harmony);
        }

        internal void LateEnable() {
            Harmony harmony = CurrentHarmony;
        }

        internal void DisableLatePatch() {
            Harmony harmony = CurrentHarmony;
        }
    }
}
