using UnityEngine;

namespace EVAOverhaul
{
    /// <summary>
    /// Flight-scene addon. Hooks GameEvents so every EVA kerbal gets an EVAKerbalController
    /// component - on hatch exit, on vessel switch, and when a save loads with kerbals already out.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EVAOverhaulFlight : MonoBehaviour
    {
        void Start()
        {
            GameEvents.onCrewOnEva.Add(OnCrewOnEva);
            GameEvents.onVesselChange.Add(OnVesselEvent);
            GameEvents.onVesselLoaded.Add(OnVesselEvent);

            foreach (Vessel v in FlightGlobals.VesselsLoaded)
                Attach(v);
        }

        void OnDestroy()
        {
            GameEvents.onCrewOnEva.Remove(OnCrewOnEva);
            GameEvents.onVesselChange.Remove(OnVesselEvent);
            GameEvents.onVesselLoaded.Remove(OnVesselEvent);
        }

        void OnCrewOnEva(GameEvents.FromToAction<Part, Part> action)
        {
            // from = the part the kerbal left through, to = the kerbal's own EVA part
            if (action.to == null) return;
            EVAKerbalController c = Attach(action.to.vessel);
            if (c != null) c.NotifyLeftHatch(action.from);
        }

        void OnVesselEvent(Vessel v)
        {
            Attach(v);
        }

        static EVAKerbalController Attach(Vessel v)
        {
            if (v == null || !v.isEVA) return null;
            KerbalEVA eva = v.FindPartModuleImplementing<KerbalEVA>();
            if (eva == null) return null;

            EVAKerbalController c = eva.gameObject.GetComponent<EVAKerbalController>();
            if (c == null) c = eva.gameObject.AddComponent<EVAKerbalController>();
            c.eva = eva;
            return c;
        }
    }
}
