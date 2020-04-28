namespace BlendRoadManager.Patches.TMPE {
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using CSUtil.Commons;
    using BlendRoadManager;
    using BlendRoadManager.Util;
    using HarmonyLib;

    [HarmonyPatch]
    public static class GetDefaultPedestrianCrossingAllowed {
        public static MethodBase TargetMethod() {
            return typeof(JunctionRestrictionsManager).
                GetMethod(nameof(JunctionRestrictionsManager.GetDefaultPedestrianCrossingAllowed));
        }

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result) {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeBlendManager.Instance.buffer[nodeID];
            return PrefixUtils.HandleTernaryBool(
                data?.GetDefaultPedestrianCrossingAllowed(),
                ref __result);
        }
    }
}