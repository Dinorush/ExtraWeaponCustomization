using Agents;
using Enemies;
using System.Collections.Generic;

namespace EWC.API
{
    public static class NativePatchAPI
    {
        public static bool Loaded => EntryPoint.Loaded;

        public delegate bool DetectOnNoisePrefix(EnemyDetection __instance, AgentTarget agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, ref float output);
        public delegate void DetectOnNoisePostfix(EnemyDetection __instance, AgentTarget agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, ref float output);

        private static readonly List<DetectOnNoisePrefix> s_detectPrefix = new();
        private static readonly List<DetectOnNoisePostfix> s_detectPostfix = new();

        public static void AddDetectPrefix(DetectOnNoisePrefix detectPrefix) => s_detectPrefix.Add(detectPrefix);
        public static void AddDetectPostfix(DetectOnNoisePostfix detectPostfix) => s_detectPostfix.Add(detectPostfix);

        internal static bool RunDetectPrefix(EnemyDetection __instance, AgentTarget agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, ref float output)
        {
            bool runOrig = true;
            foreach (var prefix in s_detectPrefix)
                runOrig &= prefix(__instance, agentTarget, movementDetectionDistance, shootDetectionDistance, delta, ref output);
            return runOrig;
        }

        internal static void RunDetectPostfix(EnemyDetection __instance, AgentTarget agentTarget, float movementDetectionDistance, float shootDetectionDistance, float delta, ref float output)
        {
            foreach (var postFix in s_detectPostfix)
                postFix(__instance, agentTarget, movementDetectionDistance, shootDetectionDistance, delta, ref output);
        }
    }
}
