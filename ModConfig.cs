namespace BentleyRestaurantPhotoMode;

/// <summary>Optional settings (config.json). Villager roster and positions are intentionally not configurable.</summary>
internal sealed class ModConfig
{
    /// <summary>Show a HUD notification when photo mode starts or stops.</summary>
    public bool ShowHudMessages { get; set; } = true;

    /// <summary>SMAPI location name of Bentley's Restaurant interior in the main mod.</summary>
    public string RestaurantLocationName { get; set; } = "BentleyPlayz.FastFoodUsefulStuff_BentleysRestaurant";
}
