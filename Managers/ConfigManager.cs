using BepInEx.Configuration;

namespace SnowPlaygrounds.Managers
{
    public class ConfigManager
    {
        // _GLOBAL_
        public static ConfigEntry<bool> anyLevel;
        public static ConfigEntry<string> spawnLevels;
        public static ConfigEntry<string> frozenShaderExclusions;
        // SNOW PILE
        public static ConfigEntry<bool> isSnowPileInside;
        public static ConfigEntry<int> minSnowPileInside;
        public static ConfigEntry<int> maxSnowPileInside;
        public static ConfigEntry<bool> isSnowPileOutside;
        public static ConfigEntry<int> minSnowPileOutside;
        public static ConfigEntry<int> maxSnowPileOutside;
        // SNOWBALL
        public static ConfigEntry<int> snowballAmount;
        public static ConfigEntry<float> snowballPushForce;
        public static ConfigEntry<float> snowballSlowdownDuration;
        public static ConfigEntry<float> snowballSlowdownFactor;
        public static ConfigEntry<float> snowballThrowCooldown;
        // SNOWMAN
        public static ConfigEntry<bool> isSnowmanInside;
        public static ConfigEntry<int> minSnowmanInside;
        public static ConfigEntry<int> maxSnowmanInside;
        public static ConfigEntry<bool> isSnowmanOutside;
        public static ConfigEntry<int> minSnowmanOutside;
        public static ConfigEntry<int> maxSnowmanOutside;
        public static ConfigEntry<int> amountSnowballToBuild;
        public static ConfigEntry<float> snowmanSlowdownDuration;
        public static ConfigEntry<float> snowmanSlowdownFactor;
        // FAKE SNOWMAN
        public static ConfigEntry<bool> isJumpscareOn;
        public static ConfigEntry<float> jumpscareVolume;
        public static ConfigEntry<int> minFakeSnowman;
        public static ConfigEntry<int> maxFakeSnowman;
        // FROSTBITE
        public static ConfigEntry<int> frostbiteRarity;
        public static ConfigEntry<bool> frostbiteEating;
        public static ConfigEntry<int> frostbiteDamage;
        public static ConfigEntry<float> frostbiteMinCooldown;
        public static ConfigEntry<float> frostbiteMaxCooldown;
        public static ConfigEntry<float> frostbiteSnowballSpeed;
        public static ConfigEntry<int> frostbiteSnowballDamage;
        public static ConfigEntry<float> frostbiteStunDuration;
        public static ConfigEntry<float> frostbiteHitIncrement;
        public static ConfigEntry<float> frostbiteHitMax;

        public static void Load()
        {
            // _GLOBAL_
            anyLevel = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Any level", true, "If true, the hazards can spawn on any level");
            spawnLevels = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Spawn levels", "TitanLevel,DineLevel,RendLevel", "Name of the levels where the hazards can spawn");
            frozenShaderExclusions = SnowPlaygrounds.configFile.Bind(Constants.GLOBAL, "Frozen shader exclusions", "Red Locust Bees", "List of creatures that are not affected by the frozen shader, but are still slowed down");
            // SNOW PILE
            isSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Can spawn inside", true, $"Can {Constants.SNOW_PILE} spawn inside");
            minSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Min spawn inside", 5, $"Min {Constants.SNOW_PILE} to spawn");
            maxSnowPileInside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Max spawn inside", 8, $"Max {Constants.SNOW_PILE} to spawn");
            isSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Can spawn outside", true, $"Can {Constants.SNOW_PILE} spawn outside");
            minSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Min spawn outside", 2, $"Min {Constants.SNOW_PILE} to spawn");
            maxSnowPileOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOW_PILE, "Max spawn outside", 4, $"Max {Constants.SNOW_PILE} to spawn");
            // SNOWBALL
            snowballAmount = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Amount", 5, $"Amount of {Constants.SNOWBALL} per slot");
            snowballPushForce = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Push force", 1f, "Push force applied to target player");
            snowballSlowdownDuration = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Slowdown duration", 1f, "Slowdown duration applied to targeted enemy");
            snowballSlowdownFactor = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Slowdown factor", 3f, "Slowdown factor applied to targeted enemy");
            snowballThrowCooldown = SnowPlaygrounds.configFile.Bind(Constants.SNOWBALL, "Throw cooldown", 1f, $"{Constants.SNOWBALL} throw cooldown");
            // SNOWMAN
            isSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn inside", true, $"Can {Constants.SNOWMAN} spawn inside");
            minSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn inside", 3, $"Min {Constants.SNOWMAN} to spawn");
            maxSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn inside", 4, $"Max {Constants.SNOWMAN} to spawn");
            isSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn outside", true, $"Can {Constants.SNOWMAN} spawn outside");
            minSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn outside", 2, $"Min {Constants.SNOWMAN} to spawn");
            maxSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn outside", 3, $"Max {Constants.SNOWMAN} to spawn");
            amountSnowballToBuild = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Amount snowball", 20, $"Amout of {Constants.SNOWBALL} required to build a {Constants.SNOWMAN}");
            snowmanSlowdownDuration = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Slowdown duration", 4f, "Slowdown duration applied to targeted enemy");
            snowmanSlowdownFactor = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Slowdown factor", 3f, "Slowdown factor applied to targeted enemy");
            // FAKE SNOWMAN
            isJumpscareOn = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Enable jumpscare", true, "Enable jumpscare audio");
            jumpscareVolume = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Jumpscare volume", 0.5f, "Jumpscare audio volume");
            minFakeSnowman = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Min spawn", 2, $"Min {Constants.FAKE_SNOWMAN} to spawn by default");
            maxFakeSnowman = SnowPlaygrounds.configFile.Bind(Constants.FAKE_SNOWMAN, "Max spawn", 3, $"Max {Constants.FAKE_SNOWMAN} to spawn by default");
            // FROSTBITE
            frostbiteRarity = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Rarity", 10, $"{Constants.FROSTBITE} rarity");
            frostbiteEating = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Eat the player?", true, $"Does {Constants.FROSTBITE} eat the player on collision?");
            frostbiteDamage = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Damage", 20, $"{Constants.FROSTBITE} damages on collision, does not apply if 'Eat the player?' is true");
            frostbiteMinCooldown = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Min cooldown", 0.5f, $"Minimum cooldown between {Constants.SNOWBALL} throws");
            frostbiteMaxCooldown = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Max cooldown", 1.75f, $"Maximum cooldown between {Constants.SNOWBALL} throws");
            frostbiteSnowballSpeed = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Snowball speed", 45f, $"{Constants.SNOWBALL} speed");
            frostbiteSnowballDamage = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Snowball damage", 5, $"{Constants.SNOWBALL} damage");
            frostbiteStunDuration = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Stun duration", 2f, $"Stun duration when a player is hit by a {Constants.SNOWBALL}");
            frostbiteHitIncrement = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Hit increment", 0.1f, $"Hit increment - this value starts at 1 and is used as a multiplier for the enemy's speed and the damage inflicted by his {Constants.SNOWBALL}");
            frostbiteHitMax = SnowPlaygrounds.configFile.Bind(Constants.FROSTBITE, "Hit max value", 2f, $"Max hit value before the {Constants.FROSTBITE} dies, this value is reached through the increment of the previous value");
        }
    }
}
