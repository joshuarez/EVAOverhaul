using System;
using UnityEngine;

namespace EVAOverhaul
{
    /// <summary>
    /// All tunables, loaded from GameData/EVAOverhaul/EVAOverhaul.cfg (node EVAOVERHAUL).
    /// Every value has a sensible default, so the mod still works if the .cfg is missing.
    /// </summary>
    internal static class Settings
    {
        // --- Keys (Unity KeyCode names, e.g. LeftShift, LeftControl, Mouse3) ---
        public static KeyCode SprintKey = KeyCode.LeftShift;   // hold

        // --- Sprint ---
        public static float SprintMultiplier = 1.6f;

        // --- Inventory ---
        public static int ExtraInventorySlots = 2;

        // --- Responsiveness ---
        public static float TurnRateMultiplier = 1.6f;
        public static float JetpackLinearMultiplier = 1.3f;
        public static float JetpackRotationMultiplier = 1.6f;
        public static float GroundBrake = 7f;            // 1/s - how fast sliding stops with no input (0 = stock)
        public static float LadderPushoffMultiplier = 2.5f;
        public static float LadderReleaseImpulse = 1.2f; // m/s extra shove away from a ladder/hatch on release

        public static bool DebugLogging = false;

        public static void Load()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("EVAOVERHAUL");
            if (nodes == null || nodes.Length == 0)
            {
                Debug.LogWarning("[EVAOverhaul] No EVAOVERHAUL config node found - using defaults.");
                return;
            }
            ConfigNode n = nodes[0];

            SprintKey = Key(n, "sprintKey", SprintKey);

            SprintMultiplier = F(n, "sprintMultiplier", SprintMultiplier);

            ExtraInventorySlots = I(n, "extraInventorySlots", ExtraInventorySlots);

            TurnRateMultiplier        = F(n, "turnRateMultiplier", TurnRateMultiplier);
            JetpackLinearMultiplier   = F(n, "jetpackLinearMultiplier", JetpackLinearMultiplier);
            JetpackRotationMultiplier = F(n, "jetpackRotationMultiplier", JetpackRotationMultiplier);
            GroundBrake               = F(n, "groundBrake", GroundBrake);
            LadderPushoffMultiplier   = F(n, "ladderPushoffMultiplier", LadderPushoffMultiplier);
            LadderReleaseImpulse      = F(n, "ladderReleaseImpulse", LadderReleaseImpulse);

            DebugLogging = B(n, "debugLogging", DebugLogging);

            Debug.Log("[EVAOverhaul] Settings loaded. Sprint=" + SprintKey);
        }

        static float F(ConfigNode n, string k, float d)
        {
            float v;
            return (n.HasValue(k) && float.TryParse(n.GetValue(k), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out v)) ? v : d;
        }

        static int I(ConfigNode n, string k, int d)
        {
            int v;
            return (n.HasValue(k) && int.TryParse(n.GetValue(k), out v)) ? v : d;
        }

        static bool B(ConfigNode n, string k, bool d)
        {
            bool v;
            return (n.HasValue(k) && bool.TryParse(n.GetValue(k), out v)) ? v : d;
        }

        static KeyCode Key(ConfigNode n, string k, KeyCode d)
        {
            if (!n.HasValue(k)) return d;
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), n.GetValue(k).Trim(), true); }
            catch
            {
                Debug.LogWarning("[EVAOverhaul] Unknown key '" + n.GetValue(k) + "' for " + k + ", using " + d);
                return d;
            }
        }
    }
}
