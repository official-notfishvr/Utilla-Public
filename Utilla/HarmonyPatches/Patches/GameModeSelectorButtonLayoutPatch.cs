using HarmonyLib;

namespace Utilla.HarmonyPatches.Patches;

[HarmonyPatch(typeof(GameModeSelectorButtonLayout))]
public class GameModeSelectorButtonLayoutPatch
{

    [HarmonyPatch("OnEnable"), HarmonyPrefix]
    public static bool OnEnablePatch(GameModeSelectorButtonLayout __instance)
    {
        __instance.SetupButtons();
        return false;
    }
    
    [HarmonyPatch("SetupButtons"), HarmonyPrefix]
    public static void SetupButtons(GameModeSelectorButtonLayout __instance)
    {
        NetworkSystem.Instance.OnJoinedRoomEvent -= __instance.SetupButtons;
    }
}