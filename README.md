# Bentley Restaurant Photo Mode 1.0.0

A **separate, optional promotional photography mod** for Bentley's Fast Food. It exists only to stage one promo screenshot. The main Bentley's Fast Food mod does **not** bring villagers into the restaurant by itself, and this mod is not needed to play it.

Requires: Stardew Valley 1.6.15, SMAPI 4.x, and Bentley's Fast Food (`BentleyPlayz.FastFoodUsefulStuff`) installed.

## Commands (exactly two)
- `bentley_photo_start` - teleports 12 vanilla villagers into Bentley's Restaurant and freezes them. Shows a HUD notice.
- `bentley_photo_stop` - releases everyone and puts them back where they were.

## The scene
| Villager | Tile | Pose |
|---|---|---|
| Sebastian | (21,5) | stands at arcade, facing up |
| Abigail | (23,5) | stands at arcade, facing up |
| Alex | (18,11) | **seated**, `alex_sit_left`, frame 39, facing left |
| Penny | (3,12) | **seated**, `penny_sit_down`, frame 34, facing down |
| Sam | (18,6) | standing, facing east |
| Haley | (17,12) | standing, facing east |
| Emily | (15,12) | standing, facing east |
| Leah | (13,12) | standing, facing east |
| Elliott | (1,6) | standing, facing east |
| Harvey | (1,8) | standing, facing east |
| Shane | (1,10) | standing, facing east |
| Maru | (6,12) | standing, facing east |

- **Alex and Penny are the only seated villagers.**
- **Penny is perched on the bottom (south) edge of the lower-left dining table**, facing the camera with the table directly behind her, to recreate the "sitting on the edge" look of the bridge reference. No other animation is used.
- The other eight dining-room villagers stand next to walls. The east wall has only one free tile, so four stand at the south-east end of the dining room and the rest along the west and south-west walls.
- Sebastian and Abigail stay at the arcade machines.
- Kept clear: entrance (9,13)/(10,13), staff door and NPC warp (16,3)/(17,3)/(18,4), the counter, the arcade doorway (19,9) and its approach tiles, all chairs, and hanging-lamp tiles.

## Safety and cleanup
Everything is temporary and nothing is saved: schedules and NPC data are never edited. While active, villagers cannot walk, follow schedules or wander, and their facing and pose are held every tick. Before staging, each villager's location, tile, facing and sprite frame are remembered.

- `bentley_photo_stop` and **end of day**: villagers return to their remembered spot, temporary overrides are removed, and normal schedule behaviour resumes.
- **New day, save load, return to title, mod unload**: temporary state is dropped without moving anyone.
- Partial starts (a villager missing or failing) are logged per villager and can still be stopped normally.

## Config (`config.json`, optional)
- `ShowHudMessages` (default `true`) - HUD notice on start/stop.
- `RestaurantLocationName` - default `BentleyPlayz.FastFoodUsefulStuff_BentleysRestaurant`.

## Building
Push to GitHub; the included Actions workflow builds with the SMAPI build environment and uploads `_releases`. `Directory.Build.props` and the workflow are unchanged from the handoff. **This source has not been compiled or tested in-game by its author.**
