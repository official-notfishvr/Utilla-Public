using HarmonyLib;
using UnityEngine;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaScoreboardSpawner), "OnJoinedRoom")]
    internal class ScoreboardPatch
    {
        public static void Prefix(ref GameObject ___notInRoomText)
        {
            if (!___notInRoomText)
            {
                ___notInRoomText = new GameObject();
            }
        }
    }
}
