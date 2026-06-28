using AddonFusion.Registries;
using BepInEx;
using HarmonyLib;
using LegaFusionCore.Managers;
using LethalLib.Modules;
using SnowPlaygrounds;
using SnowPlaygroundsAddon.Behaviours.AddonComponents;
using SnowPlaygroundsAddon.Behaviours.AddonProps;
using SnowPlaygroundsAddon.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SnowPlaygroundsAddon;

[BepInPlugin(modGUID, modName, modVersion)]
[BepInDependency("Lega.SnowPlaygrounds", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("Lega.AddonFusion", BepInDependency.DependencyFlags.HardDependency)]
public class SnowPlaygroundsAddon : BaseUnityPlugin
{
    public const string modGUID = "Lega.SnowPlaygroundsAddon";
    public const string modName = "Snow Playgrounds Addon";
    public const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snowplaygroundsaddon"));

    public static GameObject frostProjectorObj;

    public void Awake()
    {
        LoadItems();
        LoadPrefabs();
        harmony.PatchAll(typeof(SnowGunPatch));
    }

    public void LoadItems() => RegisterAddon(typeof(FrostMark), Constants.FROST_MARK, typeof(FrostMarkItem), bundle.LoadAsset<Item>("Assets/AddonProps/FrostMarkItem.asset"));

    public void RegisterAddon(Type addonType, string addonName, Type itemType, Item item)
    {
        item = LFCObjectsManager.RegisterObject(itemType, item);
        AddonObjectRegistry.Add(addonType, addonName, item.spawnPrefab);
    }

    public void LoadPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (frostProjectorObj = bundle.LoadAsset<GameObject>("Assets/AoEProjector/FrostProjector.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
            Utilities.FixMixerGroups(gameObject);
    }
}

