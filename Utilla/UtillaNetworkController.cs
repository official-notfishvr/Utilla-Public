using ExitGames.Client.Photon;
using GorillaNetworking;
using Photon.Pun;
using Utilla.Utils;

namespace Utilla
{
    public class UtillaNetworkController : MonoBehaviourPunCallbacks
    {

        Events.RoomJoinedArgs lastRoom;

        public GamemodeManager gameModeManager;

        public override void OnJoinedRoom()
        {
            // trigger events
            bool isPrivate = false;
            string gamemode = "";
            if (PhotonNetwork.CurrentRoom != null)
            {
                var currentRoom = PhotonNetwork.NetworkingClient.CurrentRoom;
                isPrivate = !currentRoom.IsVisible; // Room Browser rooms
                if (currentRoom.CustomProperties.TryGetValue("gameMode", out var gamemodeObject))
                {
                    gamemode = gamemodeObject as string;
                }
            }

            // custom gamemode-names (which are used by GorillaComputer and are included in GorillaGameManager on the gamemode selectors) can be set by custom managers, just using the "CUSTOM" prefix isn't too great either, it's not specific enough
            /*
			var prefix = "ERROR";
			if (gamemode.Contains(Models.Gamemode.GamemodePrefix))
			{
				prefix = "CUSTOM";
            }
			else
            {
                var dict = new Dictionary<string, string> {
					{ "INFECTION", "INFECTION" },
                    { "CASUAL", "CASUAL"},
                    { "HUNT", "HUNT" },
                    { "BATTLE", "PAINTBRAWL"},
				};

				foreach (var item in dict)
                {
					if (gamemode.Contains(item.Key))
                    {
						prefix = item.Value;
						break;
                    }
                } 
            }
			GorillaComputer.instance.currentGameModeText.Value = "CURRENT MODE\n" + prefix;
			*/

            Events.RoomJoinedArgs args = new Events.RoomJoinedArgs
            {
                isPrivate = isPrivate,
                Gamemode = gamemode
            };
            Events.Instance.TriggerRoomJoin(args);

            lastRoom = args;

            RoomUtils.ResetQueue();
        }

        public override void OnLeftRoom()
        {
            if (lastRoom != null)
            {
                Events.Instance.TriggerRoomLeft(lastRoom);
                lastRoom = null;
            }

            GorillaComputer.instance.currentGameModeText.Value = "CURRENT MODE\n-NOT IN ROOM-";
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (!propertiesThatChanged.TryGetValue("gameMode", out var gameModeObject)) return;
            if (gameModeObject is not string gameMode) return;

            if (lastRoom.Gamemode.Contains(Constants.GamemodePrefix) && !gameMode.Contains(Constants.GamemodePrefix))
            {
                gameModeManager.OnRoomLeft(null, lastRoom);
            }

            lastRoom.Gamemode = gameMode;
            lastRoom.isPrivate = PhotonNetwork.CurrentRoom.IsVisible;
        }
    }
}
