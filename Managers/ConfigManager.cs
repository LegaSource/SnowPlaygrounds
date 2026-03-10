using BepInEx.Configuration;

namespace SnowPlaygrounds.Managers;

public class ConfigManager
{
    // _GLOBAL_
    public static ConfigEntry<bool> anyLevel;
    public static ConfigEntry<string> spawnLevels;
    public static ConfigEntry<string> spawnWeathers;
    // SNOW PILE
    public static ConfigEntry<int> snowPileAmount;
    public static ConfigEntry<bool> isSnowPileInside;
    public static ConfigEntry<int> minSnowPileInside;
    public static ConfigEntry<int> maxSnowPileInside;
    public static ConfigEntry<bool> isSnowPileOutside;
    public static ConfigEntry<int> minSnowPileOutside;
    public static ConfigEntry<int> maxSnowPileOutside;
    // SNOWBALL
    public static ConfigEntry<int> snowBallAmount;
    public static ConfigEntry<float> snowBallPushForce;
    public static ConfigEntry<float> snowBallSlowdownDuration;
    public static ConfigEntry<float> snowBallSlowdownFactor;
    public static ConfigEntry<float> snowBallThrowCooldown;
    // SNOWMAN
    public static ConfigEntry<bool> isSnowmanInside;
    public static ConfigEntry<int> minSnowmanInside;
    public static ConfigEntry<int> maxSnowmanInside;
    public static ConfigEntry<bool> isSnowmanOutside;
    public static ConfigEntry<int> minSnowmanOutside;
    public static ConfigEntry<int> maxSnowmanOutside;
    public static ConfigEntry<int> amountSnowBallToBuild;
    // FAKE SNOWMAN
    public static ConfigEntry<bool> isJumpscareOn;
    public static ConfigEntry<float> jumpscareVolume;
    public static ConfigEntry<int> minFakeSnowman;
    public static ConfigEntry<int> maxFakeSnowman;
    // FROSTBITE
    public static ConfigEntry<int> frostbiteRarity;
    public static ConfigEntry<bool> frostbiteEating;
    public static ConfigEntry<int> frostbiteDamage;
    public static ConfigEntry<bool> frostbiteDefaultHit;
    // SNOWGUN
    public static ConfigEntry<int> snowGunAmount;
    // ADDONS
    public static ConfigEntry<int> glacialBallCooldown;
    public static ConfigEntry<int> glacialDecoyCooldown;
    // ICE ZONE
    public static ConfigEntry<bool> isIceZoneInside;
    public static ConfigEntry<int> minIceZoneInside;
    public static ConfigEntry<int> maxIceZoneInside;
    public static ConfigEntry<bool> isIceZoneOutside;
    public static ConfigEntry<int> minIceZoneOutside;
    public static ConfigEntry<int> maxIceZoneOutside;

    public static void Load()
    {
        // _GLOBAL_
        anyLevel = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Any level", true, "If true, the hazards can spawn on any level");
        spawnLevels = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Spawn levels", "tundralevel,titanlevel,dinelevel,rendlevel", "Name of the levels where the hazards can spawn");
        spawnWeathers = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Spawn weathers", "Blizzard,Snowfall", "Name of the weathers where the hazards can spawn");
        // SNOW PILE
        snowPileAmount = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Amount", 20, $"Amount of snow in a {Constants.SNOW_PILE}");
        isSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Can spawn inside", true, $"Can {Constants.SNOW_PILE} spawn inside");
        minSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Min spawn inside", 5, $"Min {Constants.SNOW_PILE} to spawn");
        maxSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Max spawn inside", 8, $"Max {Constants.SNOW_PILE} to spawn");
        isSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Can spawn outside", true, $"Can {Constants.SNOW_PILE} spawn outside");
        minSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Min spawn outside", 2, $"Min {Constants.SNOW_PILE} to spawn");
        maxSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Max spawn outside", 4, $"Max {Constants.SNOW_PILE} to spawn");
        // SNOWBALL
        snowBallAmount = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Amount", 5, $"Amount of {Constants.SNOWBALL} per slot");
        snowBallPushForce = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Push force", 1f, "Push force applied to target player");
        snowBallSlowdownDuration = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Slowdown duration", 1f, "Slowdown duration applied to targeted enemy");
        snowBallSlowdownFactor = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Slowdown factor", 3f, "Slowdown factor applied to targeted enemy");
        snowBallThrowCooldown = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Throw cooldown", 1f, $"{Constants.SNOWBALL} throw cooldown");
        // SNOWMAN
        isSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn inside", true, $"Can {Constants.SNOWMAN} spawn inside");
        minSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn inside", 2, $"Min {Constants.SNOWMAN} to spawn");
        maxSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn inside", 3, $"Max {Constants.SNOWMAN} to spawn");
        isSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn outside", true, $"Can {Constants.SNOWMAN} spawn outside");
        minSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn outside", 2, $"Min {Constants.SNOWMAN} to spawn");
        maxSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn outside", 3, $"Max {Constants.SNOWMAN} to spawn");
        amountSnowBallToBuild = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Amount snowball", 20, $"Amout of {Constants.SNOWBALL} required to build a {Constants.SNOWMAN}");
        // FAKE SNOWMAN
        isJumpscareOn = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Enable jumpscare", true, "Enable jumpscare audio");
        jumpscareVolume = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Jumpscare volume", 0.5f, "Jumpscare audio volume");
        minFakeSnowman = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Min spawn", 1, $"Min {Constants.FAKE_SNOWMAN} to spawn by default");
        maxFakeSnowman = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Max spawn", 2, $"Max {Constants.FAKE_SNOWMAN} to spawn by default");
        // FROSTBITE
        frostbiteRarity = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Rarity", 10, $"{Constants.FROSTBITE} rarity");
        frostbiteEating = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Eat the player?", true, $"Does {Constants.FROSTBITE} eat the player on collision?");
        frostbiteDamage = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Damage", 20, $"{Constants.FROSTBITE} damages on collision, does not apply if 'Eat the player?' is true");
        frostbiteDefaultHit = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Default hit", false, $"Allow hitting {Constants.FROSTBITE} with conventional means");
        // SNOWGUN
        snowGunAmount = SnowPlaygrounds.configFile.Bind(Constants.SNOWGUN, "Amount", 20, $"Amount of snow in a {Constants.SNOWGUN}");
        // ADDONS
        glacialBallCooldown = SnowPlaygrounds.configFile.Bind(Constants.ADDONS, $"{Constants.GLACIAL_BALL} Cooldown", 45, $"Cooldown duration of the {Constants.GLACIAL_BALL}");
        glacialDecoyCooldown = SnowPlaygrounds.configFile.Bind(Constants.ADDONS, $"{Constants.GLACIAL_DECOY} Cooldown", 45, $"Cooldown duration of the {Constants.GLACIAL_DECOY}");
        // ICE ZONE
        isIceZoneInside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Can spawn inside", true, $"Can {Constants.ICE_ZONE} spawn inside");
        minIceZoneInside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Min spawn inside", 2, $"Min {Constants.ICE_ZONE} to spawn");
        maxIceZoneInside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Max spawn inside", 3, $"Max {Constants.ICE_ZONE} to spawn");
        isIceZoneOutside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Can spawn outside", true, $"Can {Constants.ICE_ZONE} spawn outside");
        minIceZoneOutside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Min spawn outside", 2, $"Min {Constants.ICE_ZONE} to spawn");
        maxIceZoneOutside = SnowPlaygrounds.configFile.Bind(Constants.ICE_ZONE, "Max spawn outside", 3, $"Max {Constants.ICE_ZONE} to spawn");
    }
}
