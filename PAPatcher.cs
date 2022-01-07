using HarmonyLib;
using MoveIt;
using PropAnarchy.PLT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PropAnarchy {
    internal static class PAPatcher {
        private const string HARMONYID = @"com.quistar.PropAnarchy";

        private static IEnumerable<CodeInstruction> ToolControllerSetToolTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int sigCount = 0;
            Label exit = il.DefineLabel();
            MethodInfo setEnable = AccessTools.PropertySetter(typeof(UnityEngine.Behaviour), nameof(UnityEngine.Behaviour.enabled));
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPrefix)));
            yield return new CodeInstruction(OpCodes.Brfalse, exit);
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Brfalse) {
                    if (++sigCount == 2) {
                        code.operand = exit;
                    }
                    yield return code;
                } else if (sigCount > 1 && code.opcode == OpCodes.Callvirt && code.operand == setEnable) {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(exit);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPostfix)));
                } else {
                    yield return code;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ActionAddPostfix(HashSet<Instance> selection) => PAPainter.ActionAddHandler?.Invoke(selection);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ActionClonePostfix(Dictionary<Instance, Instance> ___m_origToCloneUpdate) => PAPainter.ActionCloneHandler?.Invoke(___m_origToCloneUpdate);

        internal static void EnablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            try {
                harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(ToolControllerSetToolTranspiler))));
            } catch (Exception e) {
                PAModule.PALog("Failed to patch ToolController::SetTool");
                PAModule.PALog(e.Message);
                harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAUtils), nameof(PAUtils.DebugPatchOutput))));
                throw;
            }
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
            harmony.Unpatch(AccessTools.Method(typeof(ToolController), @"SetTool"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(MyExtensions), nameof(MyExtensions.AddObject)), HarmonyPatchType.Postfix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(CloneActionBase), nameof(CloneActionBase.Do)), HarmonyPatchType.Postfix, HARMONYID);
        }
    }
}
