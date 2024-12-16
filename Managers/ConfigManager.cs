using BepInEx.Configuration;

namespace SnowPlaygrounds.Managers
{
    public class ConfigManager
    {
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

        public static void Load()
        {
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
            // SNOWMAN
            isSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn inside", true, $"Can {Constants.SNOWMAN} spawn inside");
            minSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn inside", 2, $"Min {Constants.SNOWMAN} to spawn");
            maxSnowmanInside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn inside", 3, $"Max {Constants.SNOWMAN} to spawn");
            isSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Can spawn outside", true, $"Can {Constants.SNOWMAN} spawn outside");
            minSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Min spawn outside", 1, $"Min {Constants.SNOWMAN} to spawn");
            maxSnowmanOutside = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Max spawn outside", 3, $"Max {Constants.SNOWMAN} to spawn");
            amountSnowballToBuild = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Amount snowball", 20, $"Amout of {Constants.SNOWBALL} required to build a {Constants.SNOWMAN}");
            snowmanSlowdownDuration = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Slowdown duration", 4f, "Slowdown duration applied to targeted enemy");
            snowmanSlowdownFactor = SnowPlaygrounds.configFile.Bind(Constants.SNOWMAN, "Slowdown factor", 3f, "Slowdown factor applied to targeted enemy");
        }
    }
}
