using HarmonyLib;
using UnityEngine;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaLevelScreen), "UpdateText")]
    internal class GorillaLevelScreenPatch
    {
        public static bool Prefix(GorillaLevelScreen __instance) => __instance.GetComponent<MeshRenderer>();
    }
}
