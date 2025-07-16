using BepInEx;
using BepInEx.Bootstrap;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Utilla.Attributes;
using Utilla.Models;
using Utilla.Tools;
using Utilla.Utils;

namespace Utilla.Behaviours
{
    internal class GamemodeManager : Singleton<GamemodeManager>
    {
        public List<Gamemode> Gamemodes { get; private set; }

        public Dictionary<GameModeType, Gamemode> ModdedGamemodesPerMode;
        public List<Gamemode> DefaultModdedGamemodes;

        // Custom game modes
        public List<Gamemode> CustomGameModes;
        private GameObject customGameModeContainer;
        private List<PluginInfo> pluginInfos;

        private List<string> gtGameModeNames;

        public override void Initialize()
        {
            base.Initialize();

            Events.RoomJoined += OnRoomJoin;
            Events.RoomLeft += OnRoomLeft;
        }

        public void Start()
        {
            gtGameModeNames = GameMode.gameModeNames;

            customGameModeContainer = new GameObject("Utilla Custom GameModes");
            customGameModeContainer.transform.SetParent(GameMode.instance.gameObject.transform);

            string currentGameMode = PlayerPrefs.GetString("currentGameMode", GameModeType.Infection.ToString());
            GorillaComputer.instance.currentGameMode.Value = currentGameMode;

            IEnumerable<GTZone> zones = Enum.GetValues(typeof(GTZone)).Cast<GTZone>();
            HashSet<GameModeType> usedGameModes = [];
            zones.Select(zone => GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate)).ForEach(usedGameModes.UnionWith);
            ModdedGamemodesPerMode = usedGameModes.ToDictionary(game_mode => game_mode, game_mode => new Gamemode(Constants.GamemodePrefix, $"MODDED {GameModeUtils.GetGameModeName(game_mode)}", game_mode));
            Logging.Info($"Modded Game Modes: {string.Join(", ", ModdedGamemodesPerMode.Select(item => item.Value).Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");
            DefaultModdedGamemodes = [.. ModdedGamemodesPerMode.Values];

            GTZone startZone = PhotonNetworkController.Instance.StartZone; // for future reference: use active zone if this reference is taken out completely
            if (!UtillaGamemodeSelector.SelectorLookup.TryGetValue(startZone, out var originalSelector))
            {
                Logging.Fatal($"Game Mode Selector not found for {startZone.GetName()}!!");
                Logging.Info(string.Join(Environment.NewLine, UtillaGamemodeSelector.SelectorLookup));
                return;
            }

            Gamemodes = [.. originalSelector.BaseGameModes];

            pluginInfos = GetPluginInfos();
            CustomGameModes = GetGamemodes(pluginInfos);
            Logging.Info($"Custom Game Modes: {string.Join(", ", CustomGameModes.Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");
            Gamemodes.AddRange(DefaultModdedGamemodes.Concat(CustomGameModes));
            Gamemodes.ForEach(AddGamemodeToPrefabPool);
            Logging.Info($"Game Modes: {string.Join(", ", Gamemodes.Select(mode => mode.DisplayName).Select(displayName => string.Format("\"{0}\"", displayName)))}");

            originalSelector.CheckGameMode();
            currentGameMode = GorillaComputer.instance.currentGameMode.Value;

            int basePageCount = originalSelector.BaseGameModes.Count;
            List<Gamemode> avaliableModes = originalSelector.GetSelectorGameModes();
            int selectedMode = avaliableModes.FindIndex(gm => gm.ID == currentGameMode);
            originalSelector.PageCount = Mathf.CeilToInt(avaliableModes.Count / (float)basePageCount);
            originalSelector.CurrentPage = (selectedMode != -1 && selectedMode < avaliableModes.Count) ? Mathf.FloorToInt(selectedMode / (float)basePageCount) : 0;
            originalSelector.ShowPage(true);
        }

        public List<Gamemode> GetGamemodes(List<PluginInfo> infos)
        {
            List<Gamemode> gamemodes = [];

            HashSet<Gamemode> additonalGamemodes = [];
            foreach (var info in infos)
            {
                additonalGamemodes.UnionWith(info.Gamemodes);
            }

            foreach (var gamemode in DefaultModdedGamemodes)
            {
                additonalGamemodes.Remove(gamemode);
            }

            gamemodes.AddRange(additonalGamemodes);

            return gamemodes;
        }

        List<PluginInfo> GetPluginInfos()
        {
            List<PluginInfo> infos = [];

            foreach (var info in Chainloader.PluginInfos)
            {
                if (info.Value is null) continue;
                BaseUnityPlugin plugin = info.Value.Instance;
                if (plugin is null) continue;
                Type type = plugin.GetType();

                IEnumerable<Gamemode> gamemodes = GetGamemodes(type);

                if (gamemodes.Any())
                {
                    infos.Add(new PluginInfo
                    {
                        Plugin = plugin,
                        Gamemodes = [.. gamemodes],
                        OnGamemodeJoin = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeJoinAttribute)),
                        OnGamemodeLeave = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeLeaveAttribute)),
                        OnGamemodeStart = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeStartAttribute)),
                        OnGamemodeEnd = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeEndAttribute)),
                        OnGamemodeUpdate = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeUpdateAttribute)),
                        OnGamemodePlayerJoin = CreatePlayerAction(plugin, type, typeof(ModdedGamemodePlayerJoinAttribute)),
                        OnGamemodePlayerLeave = CreatePlayerAction(plugin, type, typeof(ModdedGamemodePlayerLeaveAttribute)),
                        OnGamemodePlayerTag = CreatePlayerTagAction(plugin, type, typeof(ModdedGamemodePlayerTagAttribute)),
                        OnGamemodePhotonPlayerJoin = CreatePhotonPlayerAction(plugin, type, typeof(ModdedGamemodePlayerJoinAttribute)),
                        OnGamemodePhotonPlayerLeave = CreatePhotonPlayerAction(plugin, type, typeof(ModdedGamemodePlayerLeaveAttribute)),
                        OnGamemodePhotonPlayerTag = CreatePhotonPlayerTagAction(plugin, type, typeof(ModdedGamemodePlayerTagAttribute))
                    });
                }
            }

            return infos;
        }

        Action<string> CreateJoinLeaveAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression param = Expression.Parameter(typeof(string));
            ParameterExpression[] paramExpression = [param];
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, param);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        Action<string, GTPlayer> CreatePlayerAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression gamemodeParam = Expression.Parameter(typeof(string));
            ParameterExpression playerParam = Expression.Parameter(typeof(GTPlayer));
            ParameterExpression[] paramExpression = [gamemodeParam, playerParam];
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string, GTPlayer> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam);
                }
                else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(GTPlayer))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam, playerParam);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string, GTPlayer>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        Action<string, GTPlayer, GTPlayer> CreatePlayerTagAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression gamemodeParam = Expression.Parameter(typeof(string));
            ParameterExpression taggerParam = Expression.Parameter(typeof(GTPlayer));
            ParameterExpression taggedParam = Expression.Parameter(typeof(GTPlayer));
            ParameterExpression[] paramExpression = [gamemodeParam, taggerParam, taggedParam];
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string, GTPlayer, GTPlayer> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam);
                }
                else if (parameters.Length == 3 && parameters[0].ParameterType == typeof(string) && 
                         parameters[1].ParameterType == typeof(GTPlayer) && parameters[2].ParameterType == typeof(GTPlayer))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam, taggerParam, taggedParam);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string, GTPlayer, GTPlayer>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        Action<string, Player> CreatePhotonPlayerAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression gamemodeParam = Expression.Parameter(typeof(string));
            ParameterExpression playerParam = Expression.Parameter(typeof(Player));
            ParameterExpression[] paramExpression = [gamemodeParam, playerParam];
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string, Player> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam);
                }
                else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(Player))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam, playerParam);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string, Player>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        Action<string, Player, Player> CreatePhotonPlayerTagAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression gamemodeParam = Expression.Parameter(typeof(string));
            ParameterExpression taggerParam = Expression.Parameter(typeof(Player));
            ParameterExpression taggedParam = Expression.Parameter(typeof(Player));
            ParameterExpression[] paramExpression = [gamemodeParam, taggerParam, taggedParam];
            ConstantExpression instance = Expression.Constant(plugin);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Action<string, Player, Player> action = null;
            foreach (var method in baseType.GetMethods(bindingFlags).Where(m => m.GetCustomAttribute(attribute) != null))
            {
                var parameters = method.GetParameters();
                MethodCallExpression methodCall;
                if (parameters.Length == 0)
                {
                    methodCall = Expression.Call(instance, method);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam);
                }
                else if (parameters.Length == 3 && parameters[0].ParameterType == typeof(string) && 
                         parameters[1].ParameterType == typeof(Player) && parameters[2].ParameterType == typeof(Player))
                {
                    methodCall = Expression.Call(instance, method, gamemodeParam, taggerParam, taggedParam);
                }
                else
                {
                    continue;
                }

                action += Expression.Lambda<Action<string, Player, Player>>(methodCall, paramExpression).Compile();
            }

            return action;
        }

        HashSet<Gamemode> GetGamemodes(Type type)
        {
            IEnumerable<ModdedGamemodeAttribute> attributes = type.GetCustomAttributes<ModdedGamemodeAttribute>();

            HashSet<Gamemode> gamemodes = [];
            if (attributes is not null)
            {
                foreach (ModdedGamemodeAttribute attribute in attributes)
                {
                    if (attribute.gamemode is not null)
                    {
                        gamemodes.Add(attribute.gamemode);
                        continue;
                    }
                    gamemodes.UnionWith(DefaultModdedGamemodes);
                }
            }

            return gamemodes;
        }

        void AddGamemodeToPrefabPool(Gamemode gamemode)
        {
            if (gamemode.GameManager is null) return;

            if (GameMode.gameModeKeyByName.ContainsKey(gamemode.ID))
            {
                Logging.Warning($"Game Mode already exists: has ID {gamemode.ID}");
                return;
            }

            Type gmType = gamemode.GameManager;

            if (gmType is null || !gmType.IsSubclassOf(typeof(GorillaGameManager)))
            {
                GameModeType? gmKey = gamemode.BaseGamemode;

                if (gmKey == null)
                {
                    Logging.Warning($"Game Mode not made cuz lack of info: has ID {gamemode.ID}");
                    return;
                }

                GameMode.gameModeKeyByName[gamemode.ID] = (int)gmKey;
                //GameMode.gameModeKeyByName[gamemode.DisplayName] = (int)gmKey;
                gtGameModeNames.Add(gamemode.ID);
                return;
            }

            GameObject prefab = new($"{gamemode.ID}: {gmType.Name}");
            prefab.SetActive(false);

            GorillaGameManager gameMode = prefab.AddComponent(gmType) as GorillaGameManager;
            int gameModeKey = (int)gameMode.GameType();

            if (GameMode.gameModeTable.ContainsKey(gameModeKey))
            {
                Logging.Error($"Game Mode with name '{GameMode.gameModeTable[gameModeKey].GameModeName()}' is already using GameType '{gameModeKey}'.");
                Destroy(prefab);
                return;
            }

            GameMode.gameModeTable[gameModeKey] = gameMode;
            GameMode.gameModeKeyByName[gamemode.ID] = gameModeKey;
            //GameMode.gameModeKeyByName[gamemode.DisplayName] = gameModeKey;
            gtGameModeNames.Add(gamemode.ID);
            GameMode.gameModes.Add(gameMode);

            prefab.transform.SetParent(customGameModeContainer.transform);
            prefab.SetActive(true);

            if (gameMode.fastJumpLimit == 0 || gameMode.fastJumpMultiplier == 0)
            {
                Logging.Warning($"FAST JUMP SPEED AREN'T ASSIGNED FOR {gmType.Name}!!! ASSIGN THESE ASAP");

                float[] speed = gameMode.LocalPlayerSpeed();
                gameMode.fastJumpLimit = speed[0];
                gameMode.fastJumpMultiplier = speed[1];
            }
        }

        internal void OnRoomJoin(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            Logging.Info($"Joined room: with game mode {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeJoin?.Invoke(gamemode);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Join action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnRoomLeft(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            Logging.Info($"Left room: with game mode {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeLeave?.Invoke(gamemode);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Leave action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnGamemodeStart(string gamemode)
        {
            Logging.Info($"Gamemode started: {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeStart?.Invoke(gamemode);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Gamemode start action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnGamemodeEnd(string gamemode)
        {
            Logging.Info($"Gamemode ended: {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeEnd?.Invoke(gamemode);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Gamemode end action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnGamemodeUpdate(string gamemode)
        {
            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeUpdate?.Invoke(gamemode);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Gamemode update action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPlayerJoin(string gamemode, GTPlayer player)
        {
            Logging.Info($"Player joined gamemode: {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePlayerJoin?.Invoke(gamemode, player);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Player join action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPlayerLeave(string gamemode, GTPlayer player)
        {
            Logging.Info($"Player left gamemode: {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePlayerLeave?.Invoke(gamemode, player);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Player leave action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPlayerTag(string gamemode, GTPlayer tagger, GTPlayer tagged)
        {
            Logging.Info($"Player tagged: {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePlayerTag?.Invoke(gamemode, tagger, tagged);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Player tag action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPhotonPlayerJoin(string gamemode, Player player)
        {
            Logging.Info($"Photon player joined gamemode: {gamemode}, Player: {player?.UserId}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePhotonPlayerJoin?.Invoke(gamemode, player);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Photon player join action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPhotonPlayerLeave(string gamemode, Player player)
        {
            Logging.Info($"Photon player left gamemode: {gamemode}, Player: {player?.UserId}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePhotonPlayerLeave?.Invoke(gamemode, player);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Photon player leave action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        internal void OnPhotonPlayerTag(string gamemode, Player tagger, Player tagged)
        {
            Logging.Info($"Photon player tagged: {gamemode}, Tagger: {tagger?.UserId}, Tagged: {tagged?.UserId}");

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodePhotonPlayerTag?.Invoke(gamemode, tagger, tagged);
                }
                catch (Exception ex)
                {
                    Logging.Fatal($"Photon player tag action could not be called");
                    Logging.Error(ex);
                }
            }
        }

        private float updateTimer = 0f;
        private const float UPDATE_INTERVAL = 1f;

        private void Update()
        {
            if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom)
            {
                updateTimer += Time.deltaTime;
                
                if (updateTimer >= UPDATE_INTERVAL)
                {
                    string currentGamemode = GorillaComputer.instance.currentGameMode.Value;
                    if (!string.IsNullOrEmpty(currentGamemode))
                    {
                        OnGamemodeUpdate(currentGamemode);
                    }
                    updateTimer = 0f;
                }
            }
            else
            {
                updateTimer = 0f;
            }
        }
    }
}
