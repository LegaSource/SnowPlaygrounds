using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using LethalLib.Modules;
using BepInEx.Logging;
using System;
using SnowPlaygrounds.Managers;
using SnowPlaygrounds.Behaviours.Items;
using LethalLib.Extras;
using System.Collections.Generic;
using SnowPlaygrounds.Patches;

namespace SnowPlaygrounds
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class SnowPlaygrounds : BaseUnityPlugin
    {
        private const string modGUID = "Lega.SnowPlaygrounds";
        private const string modName = "Snow Playgrounds";
        private const string modVersion = "1.0.2";

        private readonly Harmony harmony = new Harmony(modGUID);
        private readonly static AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snowplaygrounds"));
        internal static ManualLogSource mls;
        public static ConfigFile configFile;

        public static GameObject managerPrefab = NetworkPrefabs.CreateNetworkPrefab("SnowPlaygroundsNetworkManager");

        // Items
        public static GameObject snowballObj;

        // Hazards
        public static GameObject snowPileObj;
        public static GameObject snowmanObj;

        // Materials
        public static GameObject snowballDecal;
        public static Material frozenShader;

        // Particles
        public static GameObject snowballParticle;
        public static GameObject snowmanParticle;

        // Audios
        public static GameObject snowmanAudio;

        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("SnowPlaygrounds");
            configFile = Config;
            ConfigManager.Load();

            LoadManager();
            NetcodePatcher();
            LoadItems();
            LoadHazards();
            LoadDecals();
            LoadParticles();
            LoadAudios();
            LoadShaders();

            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
        }

        public static void LoadManager()
        {
            Utilities.FixMixerGroups(managerPrefab);
            managerPrefab.AddComponent<SnowPlaygroundsNetworkManager>();
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                        method.Invoke(null, null);
                }
            }
        }

        public void LoadItems()
            => snowballObj = RegisterItem(typeof(Snowball), bundle.LoadAsset<Item>("Assets/Snowball/SnowballItem.asset")).spawnPrefab;

        public Item RegisterItem(Type type, Item item)
        {
            PhysicsProp script = item.spawnPrefab.AddComponent(type) as PhysicsProp;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;

            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Utilities.FixMixerGroups(item.spawnPrefab);
            Items.RegisterItem(item);

            return item;
        }

        public void LoadHazards()
        {
            snowPileObj = RegisterHazard(bundle.LoadAsset<GameObject>("Assets/SnowPile/SnowPile.prefab"), ConfigManager.isSnowPileInside.Value, ConfigManager.minSnowPileInside.Value, ConfigManager.maxSnowPileInside.Value);
            snowmanObj = RegisterHazard(bundle.LoadAsset<GameObject>("Assets/Snowman/Snowman.prefab"), ConfigManager.isSnowmanInside.Value, ConfigManager.minSnowmanInside.Value, ConfigManager.maxSnowmanInside.Value);
        }

        public GameObject RegisterHazard(GameObject gameObject, bool isInside, float minSpawn, float maxSpawn)
        {
            SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
            mapObjDef.spawnableMapObject = new SpawnableMapObject
            {
                prefabToSpawn = gameObject
            };
            AnimationCurve animationCurveInside = new AnimationCurve(new Keyframe(minSpawn, maxSpawn));
            NetworkPrefabs.RegisterNetworkPrefab(mapObjDef.spawnableMapObject.prefabToSpawn);
            Utilities.FixMixerGroups(mapObjDef.spawnableMapObject.prefabToSpawn);
            if (isInside)
                MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.All, (SelectableLevel _) => animationCurveInside);

            return mapObjDef.spawnableMapObject.prefabToSpawn;
        }

        public static void LoadDecals()
            => snowballDecal = bundle.LoadAsset<GameObject>("Assets/Snowball/SnowballDecal.prefab");

        public static void LoadParticles()
        {
            HashSet<GameObject> gameObjects = new HashSet<GameObject>
            {
                (snowballParticle = bundle.LoadAsset<GameObject>("Assets/Snowball/SnowballParticle.prefab")),
                (snowmanParticle = bundle.LoadAsset<GameObject>("Assets/Snowman/SnowmanParticle.prefab"))
            };

            foreach (GameObject gameObject in gameObjects)
            {
                NetworkPrefabs.RegisterNetworkPrefab(gameObject);
                Utilities.FixMixerGroups(gameObject);
            }
        }

        public static void LoadAudios()
        {
            HashSet<GameObject> gameObjects = new HashSet<GameObject>
            {
                (snowmanAudio = bundle.LoadAsset<GameObject>("Assets/Snowman/SnowmanAudio.prefab"))
            };

            foreach (GameObject gameObject in gameObjects)
            {
                NetworkPrefabs.RegisterNetworkPrefab(gameObject);
                Utilities.FixMixerGroups(gameObject);
            }
        }

        public static void LoadShaders()
            => frozenShader = bundle.LoadAsset<Material>("Assets/Shaders/FrozenMaterial.mat");
    }
}
