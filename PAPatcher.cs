using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PropAnarchy {
    internal static class PAPatcher {
        private const string HARMONYID = @"com.quistar.PropAnarchy";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ActionAddPostfix(HashSet<Instance> selection) => PAPainter.ActionAddHandler?.Invoke(selection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ActionClonePostfix(Dictionary<Instance, Instance> ___m_origToClone) => PAPainter.ActionCloneHandler?.Invoke(___m_origToClone);

        internal static void EnablePatches() {
            //Harmony harmony = new Harmony(HARMONYID);
        }

        internal static void AttachMoveItPostProcess() {
            Harmony harmony = new Harmony(HARMONYID);
            try {
                harmony.Patch(AccessTools.Method(typeof(MyExtensions), nameof(MyExtensions.AddObject)),
                    postfix: new HarmonyMethod(typeof(PAPatcher), nameof(ActionAddPostfix)));
            } catch (Exception e) {
                PAModule.PALog("Failed to patch MoveIt::MyExtensions::AddObject()");
                PAModule.PALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(CloneActionBase), nameof(CloneActionBase.Do)),
                    postfix: new HarmonyMethod(typeof(PAPatcher), nameof(ActionClonePostfix)));
            } catch (Exception e) {
                PAModule.PALog("Failed to patch MoveIt::CloneActionBase::Do()");
                PAModule.PALog(e.Message);
            }
        }

        internal static void DisablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(MyExtensions), nameof(MyExtensions.AddObject)), HarmonyPatchType.Postfix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(CloneActionBase), nameof(CloneActionBase.Do)), HarmonyPatchType.Postfix, HARMONYID);
        }
    }
}
