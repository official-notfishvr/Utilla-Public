using HarmonyLib;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(ModeSelectButton))]
    internal class ModeSelectButtonPatch
    {
        [HarmonyPatch("Start"), HarmonyPrefix]
        public static void SetupButtonsPatch(ModeSelectButton __instance)
        {
            __instance.transform.parent.GetOrAddComponent<UtillaGamemodeSelector>(out var utillaGamemodeSelector);
            if (GamemodeManager.Instance == null)
            {
                Plugin.Instance.PostInitialized();
            }
        }
    }
}