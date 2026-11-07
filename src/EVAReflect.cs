using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EVAOverhaul
{
    /// <summary>
    /// KerbalEVA's tuning fields and FSM internals aren't a documented API, and a few names
    /// differ between KSP builds. Everything that isn't rock-solid public API goes through here,
    /// so a renamed field logs a warning and that one tweak is skipped instead of the whole mod
    /// failing to load.
    /// </summary>
    internal static class EVAReflect
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Dictionary<string, MemberInfo> cache = new Dictionary<string, MemberInfo>();
        static readonly HashSet<string> warned = new HashSet<string>();

        static MemberInfo Find(Type t, string name)
        {
            string key = t.FullName + "." + name;
            MemberInfo m;
            if (cache.TryGetValue(key, out m)) return m;

            m = (MemberInfo)t.GetField(name, Flags) ?? t.GetProperty(name, Flags);
            cache[key] = m;
            if (m == null && warned.Add(key))
                Debug.LogWarning("[EVAOverhaul] " + t.Name + "." + name + " not found in this KSP build - that tweak is disabled.");
            return m;
        }

        public static object Get(object o, string name)
        {
            if (o == null) return null;
            MemberInfo m = Find(o.GetType(), name);
            FieldInfo f = m as FieldInfo;
            if (f != null) return f.GetValue(o);
            PropertyInfo p = m as PropertyInfo;
            if (p != null && p.CanRead) return p.GetValue(o, null);
            return null;
        }

        public static bool TryGetFloat(object o, string name, out float value)
        {
            object v = Get(o, name);
            if (v is float) { value = (float)v; return true; }
            value = 0f;
            return false;
        }

        public static void SetFloat(object o, string name, float value)
        {
            if (o == null) return;
            MemberInfo m = Find(o.GetType(), name);
            FieldInfo f = m as FieldInfo;
            if (f != null && f.FieldType == typeof(float)) { f.SetValue(o, value); return; }
            PropertyInfo p = m as PropertyInfo;
            if (p != null && p.CanWrite && p.PropertyType == typeof(float)) p.SetValue(o, value, null);
        }

        // ---------- KerbalEVA state helpers ----------

        public static string StateName(KerbalEVA eva)
        {
            object fsm = Get(eva, "fsm");
            return Get(fsm, "currentStateName") as string ?? "";
        }

        static bool StateContains(KerbalEVA eva, string s)
        {
            return StateName(eva).IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool OnLadder(KerbalEVA eva)
        {
            object v = Get(eva, "OnALadder");
            if (v is bool) return (bool)v;
            return StateContains(eva, "ladder");
        }

        public static bool JetpackOn(KerbalEVA eva)
        {
            object v = Get(eva, "JetpackDeployed");
            return v is bool && (bool)v;
        }

        public static bool Ragdolled(KerbalEVA eva)
        {
            object v = Get(eva, "isRagdoll");
            if (v is bool && (bool)v) return true;
            return StateContains(eva, "ragdoll") || StateContains(eva, "recover");
        }

        public static bool Seated(KerbalEVA eva)
        {
            try { return eva.IsSeated(); } catch { return StateContains(eva, "seat"); }
        }

        public static bool Swimming(KerbalEVA eva)
        {
            return StateContains(eva, "swim");
        }

        /// <summary>Fires the stock "let go of ladder" FSM event, if this build exposes it.</summary>
        public static bool ForceLadderLetGo(KerbalEVA eva)
        {
            object fsm = Get(eva, "fsm");
            if (fsm == null) return false;

            string[] candidates = { "On_ladderLetGo", "On_LadderLetGo", "On_ladderRelease" };
            foreach (string name in candidates)
            {
                FieldInfo f = eva.GetType().GetField(name, Flags);
                if (f == null) continue;
                object evt = f.GetValue(eva);
                if (evt == null) continue;

                MethodInfo run = fsm.GetType().GetMethod("RunEvent", Flags);
                if (run == null) return false;
                run.Invoke(fsm, new[] { evt });
                return true;
            }

            if (warned.Add("ladderLetGo"))
                Debug.LogWarning("[EVAOverhaul] Couldn't find the ladder let-go FSM event; using the release impulse only.");
            return false;
        }
    }
}
