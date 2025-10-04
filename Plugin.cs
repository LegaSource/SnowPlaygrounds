using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using SnowPlaygrounds.Patches;
using SnowPlaygrounds.Patches.ModsPatches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SnowPlaygrounds;

[BepInPlugin(modGUID, modName, modVersion)]
public class SnowPlaygrounds : BaseUnityPlugin
{
    internal const string modGUID = "Lega.SnowPlaygrounds";
    internal const string modName = "Snow Playgrounds";
    internal const string modVersion = "1.1.3";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snowplaygrounds"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    public static GameObject managerPrefab = NetworkPrefabs.CreateNetworkPrefab("SnowPlaygroundsNetworkManager");

    // Items
    public static GameObject snowballPlayerObj;
    public static GameObject snowballEnemyObj;
    public static GameObject snowballGDObj;
    public static GameObject snowballGunObj;
    public static GameObject snowgunObj;

    // Hazards
    public static GameObject snowPileObj;
    public static GameObject snowmanObj;
    public static List<Snowman> snowmen = []; // Valorisé et utilisé seulement côté serveur

    // Enemies
    public static EnemyType frostbiteEnemy;

    // Materials
    public static GameObject snowballDecal;
    public static HashSet<GameObject> snowballDecals = [];
    public static Material snowShader;

    // Particles
    public static GameObject snowballParticle;
    public static GameObject snowmanParticle;

    // Audios
    public static GameObject snowPoofAudio;
    public static GameObject snowShootAudio;
    public static GameObject jumpscareAudio;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("SnowPlaygrounds");
        configFile = Config;
        ConfigManager.Load();

        LoadManager();
        NetcodePatcher();
        LoadItems();
        LoadHazards();
        LoadEnemies();
        LoadDecals();
        LoadNetworkPrefabs();
        LoadShaders();

        harmony.PatchAll(typeof(StartOfRoundPatch));
        harmony.PatchAll(typeof(RoundManagerPatch));
        harmony.PatchAll(typeof(PlayerControllerBPatch));
        harmony.PatchAll(typeof(EnemyAIPatch));
        PatchOtherMods(harmony);
    }

    public static void LoadManager()
    {
        Utilities.FixMixerGroups(managerPrefab);
        _ = managerPrefab.AddComponent<SnowPlaygroundsNetworkManager>();
    }

    private static void NetcodePatcher()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length == 0) continue;
                _ = method.Invoke(null, null);
            }
        }
    }

    public void LoadItems()
    {
        snowballPlayerObj = RegisterItem(typeof(SnowballPlayer), bundle.LoadAsset<Item>("Assets/Snowball/SP_SnowballPlayerItem.asset")).spawnPrefab;
        snowgunObj = RegisterItem(typeof(Snowgun), bundle.LoadAsset<Item>("Assets/Snowgun/SP_SnowgunItem.asset")).spawnPrefab;
    }

    public Item RegisterItem(Type type, Item item)
    {
        if (item.spawnPrefab.GetComponent(type) == null)
        {
            PhysicsProp script = item.spawnPrefab.AddComponent(type) as PhysicsProp;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;
        }

        NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        Utilities.FixMixerGroups(item.spawnPrefab);
        Items.RegisterItem(item);

        return item;
    }

    public void LoadHazards()
    {
        snowPileObj = RegisterHazard(bundle.LoadAsset<GameObject>("Assets/SnowPile/SP_SnowPile.prefab"), ConfigManager.isSnowPileInside.Value, ConfigManager.minSnowPileInside.Value, ConfigManager.maxSnowPileInside.Value);
        snowmanObj = RegisterHazard(bundle.LoadAsset<GameObject>("Assets/Snowman/Prefabs/SP_Snowman.prefab"), ConfigManager.isSnowmanInside.Value, ConfigManager.minSnowmanInside.Value, ConfigManager.maxSnowmanInside.Value);
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
        {
            if (ConfigManager.anyLevel.Value) MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.All, (SelectableLevel _) => animationCurveInside);
            else MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.None, ConfigManager.spawnLevels.Value.ToLowerInvariant().Split(','), (SelectableLevel _) => animationCurveInside);
        }

        return mapObjDef.spawnableMapObject.prefabToSpawn;
    }

    public static void LoadEnemies()
    {
        frostbiteEnemy = bundle.LoadAsset<EnemyType>("Assets/Frostbite/SP_FrostbiteEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(frostbiteEnemy.enemyPrefab);

        TerminalNode terminalNode = bundle.LoadAsset<TerminalNode>("Assets/Frostbite/SP_FrostbiteTN.asset");
        TerminalKeyword terminalKey = bundle.LoadAsset<TerminalKeyword>("Assets/Frostbite/SP_FrostbiteTK.asset");

        if (ConfigManager.anyLevel.Value) Enemies.RegisterEnemy(frostbiteEnemy, ConfigManager.frostbiteRarity.Value, Levels.LevelTypes.All, terminalNode, terminalKey);
        else Enemies.RegisterEnemy(frostbiteEnemy, ConfigManager.frostbiteRarity.Value, Levels.LevelTypes.None, ConfigManager.spawnLevels.Value.Split(','), terminalNode, terminalKey);
    }

    public static void LoadDecals() => snowballDecal = bundle.LoadAsset<GameObject>("Assets/Snowball/Prefabs/SP_SnowballDecal.prefab");

    public static void LoadNetworkPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (snowballParticle = bundle.LoadAsset<GameObject>("Assets/Snowball/Prefabs/SP_SnowballParticle.prefab")),
            (snowballEnemyObj = bundle.LoadAsset<GameObject>("Assets/Snowball/Prefabs/SP_SnowballEnemy.prefab")),
            (snowballGDObj = bundle.LoadAsset<GameObject>("Assets/Snowball/Prefabs/SP_SnowballGD.prefab")),
            (snowballGunObj = bundle.LoadAsset<GameObject>("Assets/Snowball/Prefabs/SP_SnowballGun.prefab")),
            (snowmanParticle = bundle.LoadAsset<GameObject>("Assets/Snowman/Prefabs/SP_SnowmanParticle.prefab")),
            (snowPoofAudio = bundle.LoadAsset<GameObject>("Assets/SFX/Prefabs/SP_SnowPoofAudio.prefab")),
            (snowShootAudio = bundle.LoadAsset<GameObject>("Assets/SFX/Prefabs/SP_SnowShootAudio.prefab")),
            (jumpscareAudio = bundle.LoadAsset<GameObject>("Assets/Snowman/Prefabs/SP_JumpscareAudio.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
        {
            NetworkPrefabs.RegisterNetworkPrefab(gameObject);
            Utilities.FixMixerGroups(gameObject);
        }
    }

    public static void LoadShaders() => snowShader = bundle.LoadAsset<Material>("Assets/Shaders/SP_SnowMaterial.mat");

    public static void PatchOtherMods(Harmony harmony)
    {
        Type shipInventoryPatchClass = Type.GetType("ShipInventory.Helpers.ItemManager, ShipInventory");
        if (shipInventoryPatchClass != null)
        {
            _ = harmony.Patch(
                AccessTools.Method(shipInventoryPatchClass, "StoreItem"),
                prefix: new HarmonyMethod(typeof(ShipInventoryPatch).GetMethod("PreStoreItem"))
            );
        }
    }
}
