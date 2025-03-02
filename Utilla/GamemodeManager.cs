using BepInEx;
using GorillaGameModes;
using GorillaNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Utilla.Attributes;
using Utilla.Models;
using Utilla.Tools;

namespace Utilla
{
    public class GamemodeManager : MonoBehaviour
    {
        public static bool HasInstance => Instance;
        public static GamemodeManager Instance { get; private set; }

        public List<Gamemode> DefaultModdedGamemodes;
        public List<Gamemode> Gamemodes { get; private set; }

        private List<PluginInfo> pluginInfos;

        /*
        FieldInfo fiGameModeInstance = typeof(GameMode).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        GameMode gtGameModeInstance;

        FieldInfo fiGameModeTable = typeof(GameMode).GetField("gameModeTable", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<int, GorillaGameManager> gtGameModeTable;

        FieldInfo fiGameModeKeyByName = typeof(GameMode).GetField("gameModeKeyByName", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, int> gtGameModeKeyByName;

        FieldInfo fiGameModes = typeof(GameMode).GetField("gameModes", BindingFlags.Static | BindingFlags.NonPublic);
        List<GorillaGameManager> gtGameModes;
        */

        List<string> gtGameModeNames;

        GameObject moddedGameModesObject;

        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;

            Events.RoomJoined += OnRoomJoin;
            Events.RoomLeft += OnRoomLeft;
        }

        public void Start()
        {
            gtGameModeNames = GameMode.gameModeNames;

            moddedGameModesObject = new GameObject("Modded Game Modes");
            moddedGameModesObject.transform.SetParent(GameMode.instance.gameObject.transform);

            var currentGameMode = PlayerPrefs.GetString("currentGameMode", "INFECTION");
            GorillaComputer.instance.currentGameMode.Value = currentGameMode;
            
            var defaultSelector = FindObjectsOfType<UtillaGamemodeSelector>().First(x => x.Zone == GTZone.forest);

            defaultSelector.GetSelectorGamemodes(out var gamemodes, out DefaultModdedGamemodes);

            Logging.Info(defaultSelector == null);

            Gamemodes = gamemodes;

            pluginInfos = GetPluginInfos();

            Gamemodes.AddRange(GetGamemodes(pluginInfos));

            Gamemodes.ForEach(AddGamemodeToPrefabPool);

            Logging.Info($"Current Game Mode is set at {currentGameMode}.");

            var highlightedIndex = Gamemodes.FindIndex(gm => gm.ID == currentGameMode);

            Logging.Info($"Highlighted index is set at {highlightedIndex}");

            defaultSelector.ShowPage(highlightedIndex == -1 ? 0 : Mathf.FloorToInt(highlightedIndex / (float)Constants.PageSize));
        }

        public List<Gamemode> GetExtraGameModes()
        {
            List<Gamemode> extraGameModes = new List<Gamemode>();

            foreach (var info in GetPluginInfos())
            {
                extraGameModes.AddRange(info.Gamemodes);
            }
            return extraGameModes;
        }

        public List<Gamemode> GetGamemodes(List<PluginInfo> infos)
        {
            List<Gamemode> gamemodes = [.. DefaultModdedGamemodes];

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
            foreach (var info in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (info.Value == null) continue;
                BaseUnityPlugin plugin = info.Value.Instance;
                if (plugin == null) continue;
                Type type = plugin.GetType();

                IEnumerable<Gamemode> gamemodes = GetGamemodes(type);

                if (gamemodes.Count() > 0)
                {
                    infos.Add(new PluginInfo
                    {
                        Plugin = plugin,
                        Gamemodes = gamemodes.ToArray(),
                        OnGamemodeJoin = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeJoinAttribute)),
                        OnGamemodeLeave = CreateJoinLeaveAction(plugin, type, typeof(ModdedGamemodeLeaveAttribute))
                    });
                }
            }

            return infos;
        }

        Action<string> CreateJoinLeaveAction(BaseUnityPlugin plugin, Type baseType, Type attribute)
        {
            ParameterExpression param = Expression.Parameter(typeof(string));
            ParameterExpression[] paramExpression = new ParameterExpression[] { param };
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

        HashSet<Gamemode> GetGamemodes(Type type)
        {
            IEnumerable<ModdedGamemodeAttribute> attributes = type.GetCustomAttributes<ModdedGamemodeAttribute>();

            HashSet<Gamemode> gamemodes = new HashSet<Gamemode>();
            if (attributes != null)
            {
                foreach (ModdedGamemodeAttribute attribute in attributes)
                {
                    if (attribute.gamemode != null)
                    {
                        gamemodes.Add(attribute.gamemode);
                    }
                    else
                    {
                        gamemodes.UnionWith(DefaultModdedGamemodes);
                    }
                }
            }

            return gamemodes;
        }

        void AddGamemodeToPrefabPool(Gamemode gamemode)
        {
            if (gamemode.GameManager is null) return;
            if (GameMode.gameModeKeyByName.ContainsKey(gamemode.GamemodeString) || GameMode.gameModeKeyByName.ContainsKey(gamemode.ID))
            {
                Logging.Error($"Game Mode with name '{gamemode.GamemodeString}' or '{gamemode.ID}' already exists.");
                return;
            }

            Type gmType = gamemode.GameManager;
            if (gmType == null || !gmType.IsSubclassOf(typeof(GorillaGameManager)))
            {
                GameModeType? gmKey = Enum.TryParse<GameModeType>(gamemode.BaseGamemode, out var result) ? result : null;

                if (gmKey == null)
                {
                    return;
                }

                GameMode.gameModeKeyByName[gamemode.ID] = (int)gmKey;
                //GameMode.gameModeKeyByName[gamemode.DisplayName] = (int)gmKey;
                gtGameModeNames.Add(gamemode.ID);
                return;
            }

            GameObject prefab = new GameObject(gamemode.ID);
            prefab.SetActive(false);
            var gameMode = prefab.AddComponent(gamemode.GameManager) as GorillaGameManager;
            int gameModeKey = (int)gameMode.GameType();

            if (GameMode.gameModeTable.ContainsKey(gameModeKey))
            {
                Logging.Error($"Game Mode with name '{GameMode.gameModeTable[gameModeKey].GameModeName()}' is already using GameType '{gameModeKey}'.");
                GameObject.Destroy(prefab);
                return;
            }

            GameMode.gameModeTable[gameModeKey] = gameMode;
            GameMode.gameModeKeyByName[gamemode.ID] = gameModeKey;
            //GameMode.gameModeKeyByName[gamemode.DisplayName] = gameModeKey;
            gtGameModeNames.Add(gamemode.ID);
            GameMode.gameModes.Add(gameMode);

            prefab.transform.SetParent(moddedGameModesObject.transform);
            prefab.SetActive(true);

        }

        internal void OnRoomJoin(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            Logging.Info($"Game Mode is set as {gamemode}");

            foreach (var pluginInfo in pluginInfos)
            {
                Logging.Info($"{pluginInfo.Plugin.GetType().Name}: {string.Join(", ", pluginInfo.Gamemodes.Select(gm => gm.GamemodeString))}");
                try
                {
                    pluginInfo.OnGamemodeJoin?.Invoke(gamemode);
                    Logging.Info("yes");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        internal void OnRoomLeft(object sender, Events.RoomJoinedArgs args)
        {
            string gamemode = args.Gamemode;

            foreach (var pluginInfo in pluginInfos)
            {
                try
                {
                    pluginInfo.OnGamemodeLeave?.Invoke(gamemode);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}
