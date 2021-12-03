using HarmonyLib;
using MoveIt;
using PropAnarchy.PLT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace PropAnarchy {
    internal static class PAPatcher {
        private const string HARMONYID = @"com.quistar.PropAnarchy";

        private static IEnumerable<CodeInstruction> UIToolOptionPanelStartTranspiler(IEnumerable<CodeInstruction> instructions) {
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Ldstr && next.operand is string str && str.Equals("MoveIt_OthersBtn", StringComparison.Ordinal)) {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UIToolOptionPanel), "m_moreTools"));
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Ldloc_3);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PAPainter), nameof(PAPainter.AddPropPainterBtn)));
                            yield return cur;
                            yield return next;
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

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

        private static IEnumerable<CodeInstruction> BeautificationPanelOnButtonClickedTranspiler(IEnumerable<CodeInstruction> instructions) {
            FieldInfo treePrefab = AccessTools.Field(typeof(TreeTool), nameof(TreeTool.m_prefab));
            FieldInfo propPrefab = AccessTools.Field(typeof(PropTool), nameof(PropTool.m_prefab));
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Ldloc_S && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Ldloc_S && (next.operand is LocalBuilder local) && codes.MoveNext()) {
                            var next1 = codes.Current;
                            yield return cur;
                            yield return next;
                            yield return next1;
                            if (next1.opcode == OpCodes.Stfld && next1.operand == treePrefab) {
                                yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PropLineTool), nameof(PropLineTool.SetTreePrefab)));
                            } else if (next1.opcode == OpCodes.Stfld && next1.operand == propPrefab) {
                                yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PropLineTool), nameof(PropLineTool.SetPropPrefab)));
                            }
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> ForestPanelOnButtonClickedTranspiler(IEnumerable<CodeInstruction> instructions) {
            FieldInfo treePrefab = AccessTools.Field(typeof(TreeTool), nameof(TreeTool.m_prefab));
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Stfld && code.operand == treePrefab) {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PropLineTool), nameof(PropLineTool.SetTreePrefab)));
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> PropsPanelOnButtonClickedTranspiler(IEnumerable<CodeInstruction> instructions) {
            FieldInfo propPrefab = AccessTools.Field(typeof(PropTool), nameof(PropTool.m_prefab));
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Stfld && code.operand == propPrefab) {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Castclass, typeof(PropInfo));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PropLineTool), nameof(PropLineTool.SetPropPrefab)));
                } else {
                    yield return code;
                }
            }
        }

        internal static void EnablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(ToolControllerSetToolTranspiler))));
        }

        internal static void DisablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(ToolController), @"SetTool"), HarmonyPatchType.Transpiler, HARMONYID);
        }

        internal static void LateEnablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Patch(AccessTools.Method(typeof(UIToolOptionPanel), nameof(UIToolOptionPanel.Start)), transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(UIToolOptionPanelStartTranspiler))));
            //            harmony.Patch(AccessTools.Method(typeof(BeautificationPanel), @"OnButtonClicked"),
            //                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(BeautificationPanelOnButtonClickedTranspiler))));
            //            harmony.Patch(AccessTools.Method(typeof(ForestPanel), @"OnButtonClicked"),
            //                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(ForestPanelOnButtonClickedTranspiler))));
            //            harmony.Patch(AccessTools.Method(typeof(PropsPanel), @"OnButtonClicked"),
            //                transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(PropsPanelOnButtonClickedTranspiler))));
        }

        internal static void LateDisablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(UIToolOptionPanel), nameof(UIToolOptionPanel.Start)), HarmonyPatchType.Transpiler, HARMONYID);
            //            harmony.Unpatch(AccessTools.Method(typeof(BeautificationPanel), @"OnButtonClicked"), HarmonyPatchType.Transpiler, HARMONYID);
            //            harmony.Unpatch(AccessTools.Method(typeof(ForestPanel), @"OnButtonClicked"), HarmonyPatchType.Transpiler, HARMONYID);
            //            harmony.Unpatch(AccessTools.Method(typeof(PropsPanel), @"OnButtonClicked"), HarmonyPatchType.Transpiler, HARMONYID);
        }
    }
}
