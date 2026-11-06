using System.Collections.Generic;

namespace BentleyRestaurantPhotoMode;

/// <summary>One staged villager: where they stand, which way they face, and an optional approved seated pose.</summary>
internal sealed record Placement(string Npc, int X, int Y, int Facing, string? AnimationName = null, int? Frame = null)
{
    public bool IsSeated => this.Frame.HasValue;
}

/// <summary>
/// The fixed 12-villager layout for FastFoodInterior_v2.tmx (26x14 tiles).
/// Directions: 0 = up, 1 = right/east, 2 = down, 3 = left/west.
/// </summary>
internal static class PhotoLayout
{
    public const int MapWidth = 26;
    public const int MapHeight = 14;

    public static readonly IReadOnlyList<Placement> All = new[]
    {
        // Arcade (unchanged assignments).
        new Placement("Sebastian", 21, 5, 0),
        new Placement("Abigail",   23, 5, 0),

        // The only two seated villagers.
        new Placement("Alex",  18, 11, 3, "alex_sit_left", 39),

        // Penny perches on the south edge of Table B (table tiles x3-4, y10-11), facing the camera (down);
        // the table is directly behind her. (3,12) is free floor with nothing on the Buildings/Front layers.
        new Placement("Penny", 3, 12, 2, "penny_sit_down", 34),

        // Standing customers, wall-adjacent, facing east (toward the arcade) where practical.
        // East side / south-east wall (nearest the arcade):
        new Placement("Sam",   18, 6,  1),
        new Placement("Haley", 17, 12, 1),
        new Placement("Emily", 15, 12, 1),
        new Placement("Leah",  13, 12, 1),
        // West wall and south-west wall (east side has too few free wall tiles):
        new Placement("Elliott", 1, 6,  1),
        new Placement("Harvey",  1, 8,  1),
        new Placement("Shane",   1, 10, 1),
        new Placement("Maru",    6, 12, 1),
    };
}
