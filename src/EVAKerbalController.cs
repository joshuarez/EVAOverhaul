using UnityEngine;

namespace EVAOverhaul
{
    /// <summary>
    /// Lives on each EVA kerbal's GameObject. Handles:
    ///  1. Sprint (hold) by scaling KerbalEVA's own speed fields
    ///  2. Inventory slot fallback check
    ///  3. Snappier controls: faster turning/jetpack, ground braking, clean ladder/hatch release
    /// Only the ACTIVE kerbal reads input; all kerbals keep the responsiveness tweaks.
    /// </summary>
    public class EVAKerbalController : MonoBehaviour
    {
        public KerbalEVA eva;

        bool sprinting;

        // Stock values captured at start, so multipliers never compound.
        float baseWalk, baseRun, baseStrafe, baseBound;
        bool haveBase;

        // Ladder / hatch release
        bool wasOnLadder;
        Part hatchPart;
        float hatchTime = -999f;

        // Input snapshot (read in Update, used in FixedUpdate)
        bool anyMoveInput;

        Part P { get { return eva != null ? eva.part : null; } }
        Rigidbody RB { get { return P != null ? P.Rigidbody : null; } }
        bool IsActive { get { return eva != null && eva.vessel != null && eva.vessel == FlightGlobals.ActiveVessel; } }

        public void NotifyLeftHatch(Part from)
        {
            hatchPart = from;
            hatchTime = Time.time;
        }

        void Start()
        {
            if (eva == null) eva = GetComponent<KerbalEVA>();
            if (eva == null) { Destroy(this); return; }

            CaptureBaseAndApplyResponsiveness();
            EnsureInventorySlots();
        }

        // ------------------------------------------------------------------ setup

        void CaptureBaseAndApplyResponsiveness()
        {
            haveBase = EVAReflect.TryGetFloat(eva, "walkSpeed", out baseWalk)
                     & EVAReflect.TryGetFloat(eva, "runSpeed", out baseRun);
            EVAReflect.TryGetFloat(eva, "strafeSpeed", out baseStrafe);
            EVAReflect.TryGetFloat(eva, "boundSpeed", out baseBound);

            Scale("turnRate", Settings.TurnRateMultiplier);
            Scale("linPower", Settings.JetpackLinearMultiplier);
            Scale("rotPower", Settings.JetpackRotationMultiplier);
            Scale("ladderPushoffForce", Settings.LadderPushoffMultiplier);
        }

        void Scale(string field, float mult)
        {
            float v;
            if (mult != 1f && EVAReflect.TryGetFloat(eva, field, out v))
            {
                EVAReflect.SetFloat(eva, field, v * mult);
                if (Settings.DebugLogging) Debug.Log("[EVAOverhaul] " + field + ": " + v + " -> " + v * mult);
            }
        }

        // Fallback in case a save-time OnLoad overwrote the prefab's patched slot count.
        void EnsureInventorySlots()
        {
            ModuleInventoryPart inv = eva.part.FindModuleImplementing<ModuleInventoryPart>();
            if (inv == null || eva.part.partInfo == null || eva.part.partInfo.partPrefab == null) return;
            ModuleInventoryPart prefabInv = eva.part.partInfo.partPrefab.FindModuleImplementing<ModuleInventoryPart>();
            if (prefabInv != null && inv.InventorySlots < prefabInv.InventorySlots)
                inv.InventorySlots = prefabInv.InventorySlots;
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            if (eva == null || P == null) return;

            bool onLadder = EVAReflect.OnLadder(eva);
            bool busy = onLadder || EVAReflect.JetpackOn(eva) || EVAReflect.Ragdolled(eva)
                        || EVAReflect.Seated(eva) || EVAReflect.Swimming(eva);

            HandleLadderRelease(onLadder);

            if (!IsActive || FlightDriver.Pause || MapView.MapIsEnabled
                || !InputLockManager.IsUnlocked(ControlTypes.EVA_INPUT))
            {
                anyMoveInput = false;
                SetSprint(false);
                return;
            }

            anyMoveInput = GameSettings.EVA_forward.GetKey() || GameSettings.EVA_back.GetKey()
                        || GameSettings.EVA_left.GetKey() || GameSettings.EVA_right.GetKey();

            // ---- 1. Sprint
            SetSprint(!busy && Input.GetKey(Settings.SprintKey));
        }

        void FixedUpdate()
        {
            Rigidbody rb = RB;
            if (rb == null || eva == null) return;

            // ---- 3. Ground braking: kill the slidey "ice skating" inertia when you let go of the keys.
            if (IsActive && Settings.GroundBrake > 0f && !anyMoveInput && P.GroundContact
                && !EVAReflect.OnLadder(eva) && !EVAReflect.JetpackOn(eva) && !EVAReflect.Ragdolled(eva)
                && !EVAReflect.Seated(eva))
            {
                Vector3 up = (P.transform.position - eva.vessel.mainBody.position).normalized;
                Vector3 horiz = Vector3.ProjectOnPlane((Vector3)eva.vessel.srf_velocity, up);
                if (horiz.sqrMagnitude > 0.0025f)
                {
                    float k = Mathf.Min(Settings.GroundBrake * Time.fixedDeltaTime, 0.9f);
                    rb.AddForce(-horiz * k, ForceMode.VelocityChange);
                }
            }
        }

        // ------------------------------------------------------------------ 1. sprint

        void SetSprint(bool on)
        {
            if (on == sprinting || !haveBase) { sprinting = on; return; }
            sprinting = on;

            float m = on ? Settings.SprintMultiplier : 1f;

            EVAReflect.SetFloat(eva, "walkSpeed", baseWalk * m);
            EVAReflect.SetFloat(eva, "runSpeed", baseRun * m);
            if (baseStrafe > 0f) EVAReflect.SetFloat(eva, "strafeSpeed", baseStrafe * m);
            if (baseBound > 0f) EVAReflect.SetFloat(eva, "boundSpeed", baseBound * m);
        }

        // ------------------------------------------------------------------ 3. ladder / hatch release

        void HandleLadderRelease(bool onLadder)
        {
            // Just after leaving a hatch, Space lets go immediately even if the stock FSM is still settling.
            if (onLadder && IsActive && Time.time - hatchTime < 5f && GameSettings.EVA_Jump.GetKeyDown())
                EVAReflect.ForceLadderLetGo(eva);

            if (wasOnLadder && !onLadder && Settings.LadderReleaseImpulse > 0f)
            {
                // Shove clear of the ladder/hatch trigger so the kerbal doesn't re-grab it straight away.
                Rigidbody rb = RB;
                if (rb != null)
                {
                    Vector3 away = -P.transform.forward;
                    if (hatchPart != null && Time.time - hatchTime < 10f)
                    {
                        Vector3 fromHatch = P.transform.position - hatchPart.transform.position;
                        if (fromHatch.sqrMagnitude > 0.0001f) away = (away + fromHatch.normalized).normalized;
                    }
                    rb.AddForce(away * Settings.LadderReleaseImpulse, ForceMode.VelocityChange);
                }
            }
            wasOnLadder = onLadder;
        }
    }
}
