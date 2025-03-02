using HarmonyLib;

namespace Utilla.HarmonyPatches.Patches;

public class GorillaGameManagerPatch
{
    [HarmonyPatch(typeof(GorillaGameManager))]
    public class Temp
    {
        [HarmonyPatch("GameTypeName")]
        [HarmonyPrefix]
        public static bool Patch(GorillaGameManager __instance, ref string __result)
        {
            if (int.TryParse(__instance.GameType().ToString(), out int value))
            {
                __result = __instance.GameModeName();
                return false;
            }
            return true;
        }
    }
}