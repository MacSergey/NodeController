namespace NodeController.Patches.Corner {
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController;
    using NodeController.Patches;
    using NodeController.Util;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCorner_MinCornerOffsetPatch {
        /// <param name="leftSide">left side going away from the junction</param>
        static float FixMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID, bool leftSide) {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            if (segmentData == null)
                return cornerOffset0;
            if (segmentData.IsNodeless)
                return 0;
            return segmentData.Corner(leftSide).Offset;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return AccessTools.Method(typeof(NetSegment), nameof(NetSegment.CalculateCorner), [typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(NetInfo), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(ushort), typeof(ushort), typeof(bool), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType(), typeof(float)]) ??
                    throw new Exception("CalculateCornerPatch Could not find target method.");
        }


        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
            MethodInfo m_GetMinCornerOffset = AccessTools.Method(typeof(NetAI), nameof(NetAI.GetMinCornerOffset));
            MethodInfo m_FixMinCornerOffset = AccessTools.Method(typeof(CalculateCorner_MinCornerOffsetPatch), nameof(FixMinCornerOffset));

            // apply the flat junctions transpiler
            instructions = FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, original);

            CodeInstruction ldarg_startNodeID = GetLDArg(original, "startNodeID"); // push startNodeID into stack,
            CodeInstruction ldarg_segmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction ldarg_leftSide = GetLDArg(original, "leftSide");
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, m_FixMinCornerOffset);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                bool is_Callvirt_GetMinCornerOffsetOriginal =
                    instruction.opcode == OpCodes.Callvirt && instruction.operand == m_GetMinCornerOffset;
                if (is_Callvirt_GetMinCornerOffsetOriginal) {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return ldarg_segmentID;
                    yield return ldarg_leftSide;
                    yield return call_GetMinCornerOffset;
                }
            }

            Log.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). " +
                $"found {n} instances of Callvirt NetAI.GetMinCornerOffset()");
            yield break;
        }
    }
}
