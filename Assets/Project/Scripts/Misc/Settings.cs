public static class Settings 
{
    // Obscuring Item Fading - ObscuringItemFader
    public const float fadeInSeconds = 0.25f;
    public const float fadeOutSeconds = 0.35f;
    public const float targetAlpha = 0.45f;

    // Inventory
    public static int playerInitialInventoryCapacity = 24;
    public static int playerMaximumInventoryCapaccity = 48;

    // Tools
    public const string HoeingTool = "Hoe";
    public const string ChoppingTool = "Axe";
    public const string BreakingTool = "Pickaxe";
    public const string ReapingTool = "Scythe";
    public const string WateringTool = "Watering Can";
    public const string CollectingTool = "Basket";

    // Tilemap
    public const float gridCellSize = 1f; // grid cell size in unity units
}