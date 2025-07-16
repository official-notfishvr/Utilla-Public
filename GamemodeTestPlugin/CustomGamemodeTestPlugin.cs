using BepInEx;
using GorillaGameModes;
using GorillaLocomotion;
using Photon.Realtime;
using System;
using UnityEngine;
using Utilla;
using Utilla.Attributes;

namespace GamemodeTestPlugin
{
    [BepInPlugin("org.test.customgamemodetest", "Custom Gamemode Test Plugin", "1.0.0")]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [ModdedGamemode("customtest", "CUSTOM TEST GAMEMODE", GameModeType.Infection)]
    public class CustomGamemodeTestPlugin : BaseUnityPlugin
    {
        private bool inCustomGamemode = false;

        void Awake()
        {
            Debug.Log("[CustomGamemodeTest] Plugin Awake called");
        }

        [ModdedGamemodeJoin]
        private void OnCustomGamemodeJoin(string gamemode)
        {
            inCustomGamemode = true;
            Debug.Log($"[CustomGamemodeTest] Joined custom gamemode: {gamemode}");
        }

        [ModdedGamemodeLeave]
        private void OnCustomGamemodeLeave(string gamemode)
        {
            inCustomGamemode = false;
            Debug.Log($"[CustomGamemodeTest] Left custom gamemode: {gamemode}");
        }

        [ModdedGamemodeStart]
        private void OnCustomGamemodeStart(string gamemode)
        {
            Debug.Log($"[CustomGamemodeTest] Custom gamemode started: {gamemode}");
        }

        [ModdedGamemodeEnd]
        private void OnCustomGamemodeEnd(string gamemode)
        {
            Debug.Log($"[CustomGamemodeTest] Custom gamemode ended: {gamemode}");
        }

        [ModdedGamemodeUpdate]
        private void OnCustomGamemodeUpdate(string gamemode)
        {
            Debug.Log($"[CustomGamemodeTest] Custom gamemode update: {gamemode}");
        }

        [ModdedGamemodePlayerJoin]
        private void OnCustomPlayerJoin(string gamemode, Player player)
        {
            Debug.Log($"[CustomGamemodeTest] Player joined custom gamemode: {player?.UserId} in {gamemode}");
        }

        [ModdedGamemodePlayerLeave]
        private void OnCustomPlayerLeave(string gamemode, Player player)
        {
            Debug.Log($"[CustomGamemodeTest] Player left custom gamemode: {player?.UserId} from {gamemode}");
        }

        [ModdedGamemodePlayerTag]
        private void OnCustomPlayerTag(string gamemode, Player tagger, Player tagged)
        {
            Debug.Log($"[CustomGamemodeTest] Player tag in custom gamemode: {tagger?.UserId} tagged {tagged?.UserId} in {gamemode}");
        }
    }
} 