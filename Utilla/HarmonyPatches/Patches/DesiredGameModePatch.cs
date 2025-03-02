using System;
using GorillaGameModes;
using GorillaNetworking;
using HarmonyLib;
using Utilla.Tools;

namespace Utilla.HarmonyPatches.Patches
{
    [HarmonyPatch(typeof(GorillaNetworkJoinTrigger), nameof(GorillaNetworkJoinTrigger.GetDesiredGameType))]
    internal class DesiredGameModePatch
    {
        public static bool Prefix(ref string __result)
        {
            var gameMode = GorillaComputer.instance.currentGameMode.Value;

            if (!Enum.IsDefined(typeof(GameModeType), gameMode))
            {
                Logging.Info($"Join trigger returning nom-defined desired game mode {gameMode}");

                __result = gameMode;
                return false;
            }

            return true;
        }
    }
}
