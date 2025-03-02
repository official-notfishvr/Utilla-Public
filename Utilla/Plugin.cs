using BepInEx;
using System;
using GorillaExtensions;
using UnityEngine;
using Utilla.HarmonyPatches;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private UtillaNetworkController _networkController;
        public static Plugin Instance;

        Plugin()
        {
            Instance = this;
            UtillaPatches.ApplyHarmonyPatches();
        }

        public void Start()
        {
            Logging.Logger = Logger;

            DontDestroyOnLoad(this);
            RoomUtils.RoomCode = RoomUtils.RandomString(6); // Generate a random room code in case we need it

            _networkController = gameObject.AddComponent<UtillaNetworkController>();
        }

        public void PostInitialized()
        {
            Logging.Info("Game initialized");
            
            GameObject gameModeManagerObject = new(typeof(GamemodeManager).FullName, typeof(GamemodeManager));
            DontDestroyOnLoad(gameModeManagerObject);
            _networkController.gameModeManager = gameModeManagerObject.GetComponent<GamemodeManager>();
        }
    }
}
