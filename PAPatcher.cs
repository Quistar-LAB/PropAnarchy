using HarmonyLib;
using MoveIt;
using PropAnarchy.PLT;
using System;
using System.Collections.Generic;
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

        internal static void EnablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Patch(AccessTools.Method(typeof(ToolController), @"SetTool"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPrefix))),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(ToolBar), nameof(ToolBar.SetToolPostfix))));
        }

        internal static void DisablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(ToolController), @"SetTool"), HarmonyPatchType.All, HARMONYID);
        }

        internal static void LateEnablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Patch(AccessTools.Method(typeof(BeautificationPanel), @"OnButtonClicked"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(PropLineTool), nameof(PropLineTool.BeautificationPanelOnClickPostfix))));
            harmony.Patch(AccessTools.Method(typeof(UIToolOptionPanel), nameof(UIToolOptionPanel.Start)), transpiler: new HarmonyMethod(AccessTools.Method(typeof(PAPatcher), nameof(UIToolOptionPanelStartTranspiler))));
        }

        internal static void LateDisablePatches() {
            Harmony harmony = new Harmony(HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(BeautificationPanel), @"OnButtonClicked"), HarmonyPatchType.Postfix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(UIToolOptionPanel), nameof(UIToolOptionPanel.Start)), HarmonyPatchType.Transpiler, HARMONYID);
        }
    }
}
