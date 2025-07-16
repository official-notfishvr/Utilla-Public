using BepInEx;
using GorillaLocomotion;
using Photon.Realtime;
using System;
using System.Linq;
using Utilla.Models;

namespace Utilla
{
    public class PluginInfo
    {
        public BaseUnityPlugin Plugin { get; set; }
        public Gamemode[] Gamemodes { get; set; }
        public Action<string> OnGamemodeJoin { get; set; }
        public Action<string> OnGamemodeLeave { get; set; }
        public Action<string> OnGamemodeStart { get; set; }
        public Action<string> OnGamemodeEnd { get; set; }
        public Action<string> OnGamemodeUpdate { get; set; }
        public Action<string, GTPlayer> OnGamemodePlayerJoin { get; set; }
        public Action<string, GTPlayer> OnGamemodePlayerLeave { get; set; }
        public Action<string, GTPlayer, GTPlayer> OnGamemodePlayerTag { get; set; }
        public Action<string, Player> OnGamemodePhotonPlayerJoin { get; set; }
        public Action<string, Player> OnGamemodePhotonPlayerLeave { get; set; }
        public Action<string, Player, Player> OnGamemodePhotonPlayerTag { get; set; }

        public override string ToString()
        {
            return $"{Plugin.Info.Metadata.Name} [{string.Join(", ", Gamemodes.Select(x => x.DisplayName))}]";
        }
    }
}
