using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaComputer), "InitializeGameMode")]
    internal class InitializeGamemodePatch
    {
        internal static bool Prefix(GorillaComputer __instance)
        {
            string text = PlayerPrefs.GetString("currentGameMode", "INFECTION");

            __instance.leftHanded = PlayerPrefs.GetInt("leftHanded", 0) == 1;
            __instance.OnModeSelectButtonPress(text, __instance.leftHanded);
            // GameModePages.SetSelectedGameModeShared(text);

            return false;
        }
    }
}
