using UnityEngine;

namespace EVAOverhaul
{
    /// <summary>
    /// Runs once when the main menu first loads (all part prefabs exist by then):
    /// loads settings and adds the extra inventory slots to every kerbal EVA suit prefab.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EVAOverhaulLoader : MonoBehaviour
    {
        static bool inventoryPatched;

        void Start()
        {
            Settings.Load();
            PatchKerbalInventories();
        }

        static void PatchKerbalInventories()
        {
            if (inventoryPatched || Settings.ExtraInventorySlots <= 0) return;
            inventoryPatched = true;

            int patched = 0;
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
            {
                // Covers kerbalEVA, kerbalEVAfemale, the Vintage / Future / SlimSuit variants, etc.
                if (ap == null || ap.partPrefab == null || !ap.name.StartsWith("kerbalEVA")) continue;

                ModuleInventoryPart inv = ap.partPrefab.FindModuleImplementing<ModuleInventoryPart>();
                if (inv == null) continue;

                int before = inv.InventorySlots;
                inv.InventorySlots = before + Settings.ExtraInventorySlots;
                patched++;
                Debug.Log("[EVAOverhaul] " + ap.name + " inventory: " + before + " -> " + inv.InventorySlots + " slots");
            }

            if (patched == 0)
                Debug.LogWarning("[EVAOverhaul] No kerbal EVA prefabs with ModuleInventoryPart were found.");
        }
    }
}
