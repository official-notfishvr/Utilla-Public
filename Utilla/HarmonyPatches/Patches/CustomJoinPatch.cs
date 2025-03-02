using GorillaNetworking;
using HarmonyLib;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(PhotonNetworkController), "OnJoinedRoom")]
    internal class CustomJoinPatch
    {
        public static void Prefix(PhotonNetworkController __instance, out string[] __state)
        {
            __state = GorillaComputer.instance.allowedMapsToJoin;

            var newMaps = new string[__state.Length + 1];
            GorillaComputer.instance.allowedMapsToJoin.CopyTo(newMaps, 0);
            newMaps[newMaps.Length - 1] = "MOD_";

            GorillaComputer.instance.allowedMapsToJoin = newMaps;
        }

        public static void Postfix(string[] __state)
        {
            GorillaComputer.instance.allowedMapsToJoin = __state;
        }
    }
}
