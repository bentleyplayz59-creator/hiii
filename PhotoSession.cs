using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace BentleyRestaurantPhotoMode;

internal enum StopMode
{
    /// <summary>Put villagers back where they were before photo mode, then release them.</summary>
    Restore,

    /// <summary>Only release temporary flags; never warp anyone (title screen, mod unload, new day).</summary>
    Release
}

/// <summary>Stages the 12 villagers and keeps them frozen until stopped. Nothing here is saved to disk or to NPC data.</summary>
internal sealed class PhotoSession
{
    private sealed class Participant
    {
        public NPC Npc = null!;
        public Placement Placement = null!;

        // Original state, restored by Stop.
        public string? OriginalLocation;
        public Point OriginalTile;
        public int OriginalFacing;
        public int OriginalFrame;
        public bool OriginalIgnoreSchedule;
    }

    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly List<Participant> participants = new();
    private GameLocation? location;

    public PhotoSession(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    public bool Active { get; private set; }

    /*********
    ** Start
    *********/
    public void Start()
    {
        if (this.Active)
        {
            this.monitor.Log("Photo mode is already active. Run bentley_photo_stop first.", LogLevel.Warn);
            return;
        }

        if (!Context.IsWorldReady)
        {
            this.monitor.Log("Load a save first.", LogLevel.Error);
            return;
        }

        if (!Context.IsMainPlayer)
        {
            this.monitor.Log("Only the host/main player can stage villagers.", LogLevel.Error);
            return;
        }

        if (Game1.eventUp || Game1.isFestival())
        {
            this.monitor.Log("Can't start photo mode during an event or festival.", LogLevel.Error);
            return;
        }

        this.location = Game1.getLocationFromName(this.config.RestaurantLocationName);
        if (this.location is null)
        {
            this.monitor.Log($"Restaurant location '{this.config.RestaurantLocationName}' was not found. Is Bentley's Fast Food installed?", LogLevel.Error);
            return;
        }

        this.ValidateLayout(this.location);

        // From here on the session counts as active so bentley_photo_stop always cleans up a partial start.
        this.Active = true;
        this.participants.Clear();

        foreach (Placement placement in PhotoLayout.All)
        {
            try
            {
                NPC? npc = Game1.getCharacterFromName(placement.Npc);
                if (npc is null)
                {
                    this.monitor.Log($"Could not find villager '{placement.Npc}'; skipped.", LogLevel.Error);
                    continue;
                }

                var participant = new Participant
                {
                    Npc = npc,
                    Placement = placement,
                    OriginalLocation = npc.currentLocation?.NameOrUniqueName,
                    OriginalTile = npc.TilePoint,
                    OriginalFacing = npc.FacingDirection,
                    OriginalFrame = npc.Sprite.CurrentFrame,
                    OriginalIgnoreSchedule = npc.ignoreScheduleToday
                };
                this.participants.Add(participant);

                // Direct teleport, no walking.
                Game1.warpCharacter(npc, this.location, new Vector2(placement.X, placement.Y));
                this.Pose(participant);
            }
            catch (Exception ex)
            {
                this.monitor.Log($"Failed to place '{placement.Npc}' at ({placement.X},{placement.Y}): {ex.Message}", LogLevel.Error);
                this.monitor.Log(ex.ToString(), LogLevel.Trace);
            }
        }

        int placed = this.participants.Count;
        this.monitor.Log($"Photo mode started: {placed}/{PhotoLayout.All.Count} villagers placed.", placed == PhotoLayout.All.Count ? LogLevel.Info : LogLevel.Warn);

        if (placed == 0)
        {
            this.Stop(StopMode.Release);
            return;
        }

        if (this.config.ShowHudMessages)
            Game1.addHUDMessage(new HUDMessage("Bentley photo mode started.", HUDMessage.newQuest_type));
    }

    /*********
    ** Per-tick freeze
    *********/
    public void Enforce()
    {
        if (!this.Active || this.location is null || !Context.IsWorldReady || Game1.eventUp)
            return;

        foreach (Participant p in this.participants)
        {
            try
            {
                NPC npc = p.Npc;
                Placement pl = p.Placement;

                if (npc.currentLocation != this.location)
                    Game1.warpCharacter(npc, this.location, new Vector2(pl.X, pl.Y));

                this.Pose(p);
            }
            catch (Exception ex)
            {
                this.monitor.LogOnce($"Could not hold '{p.Placement.Npc}' in place: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    /*********
    ** Stop / cleanup
    *********/
    public void Stop(StopMode mode)
    {
        bool wasActive = this.Active;
        this.Active = false;

        foreach (Participant p in this.participants)
        {
            try
            {
                NPC npc = p.Npc;

                // Remove temporary overrides.
                npc.ignoreScheduleToday = p.OriginalIgnoreSchedule;
                npc.controller = null;
                npc.temporaryController = null;

                if (mode == StopMode.Restore && Context.IsWorldReady)
                    this.RestorePlace(p);
            }
            catch (Exception ex)
            {
                this.monitor.Log($"Could not fully release '{p.Placement.Npc}': {ex.Message}", LogLevel.Warn);
            }
        }

        this.participants.Clear();
        this.location = null;

        if (wasActive && mode == StopMode.Restore)
        {
            this.monitor.Log("Photo mode stopped. Villagers released.", LogLevel.Info);
            if (this.config.ShowHudMessages && Context.IsWorldReady)
                Game1.addHUDMessage(new HUDMessage("Bentley photo mode stopped.", HUDMessage.newQuest_type));
        }
    }

    /*********
    ** Helpers
    *********/
    /// <summary>Hold one villager still: no pathing, no schedule, fixed tile, fixed facing, approved pose if any.</summary>
    private void Pose(Participant p)
    {
        NPC npc = p.Npc;
        Placement pl = p.Placement;

        npc.controller = null;
        npc.temporaryController = null;
        npc.ignoreScheduleToday = true;

        Vector2 target = new(pl.X * 64f, pl.Y * 64f);
        if (npc.Position != target)
            npc.Position = target;

        npc.Halt();
        npc.faceDirection(pl.Facing);

        // Only Alex and Penny have a seated frame; it is applied after facing so it is not overwritten.
        if (pl.Frame is int frame && npc.Sprite.CurrentFrame != frame)
            npc.Sprite.CurrentFrame = frame;
    }

    private void RestorePlace(Participant p)
    {
        NPC npc = p.Npc;
        GameLocation? origin = p.OriginalLocation is null ? null : Game1.getLocationFromName(p.OriginalLocation);

        if (origin is not null)
        {
            Game1.warpCharacter(npc, origin, new Vector2(p.OriginalTile.X, p.OriginalTile.Y));
        }
        else
        {
            // Original location no longer exists: fall back to the villager's own default spot, never an arbitrary one.
            GameLocation? home = Game1.getLocationFromName(npc.DefaultMap);
            if (home is null)
                return;
            Game1.warpCharacter(npc, home, new Vector2((int)(npc.DefaultPosition.X / 64f), (int)(npc.DefaultPosition.Y / 64f)));
        }

        npc.Halt();
        npc.faceDirection(p.OriginalFacing);
        if (npc.Sprite.CurrentFrame != p.OriginalFrame)
            npc.Sprite.CurrentFrame = p.OriginalFrame;
    }

    /// <summary>Logs problems with the fixed layout (duplicates, bounds, blocked tiles, warps) without changing it.</summary>
    private void ValidateLayout(GameLocation loc)
    {
        var seen = new HashSet<Point>();
        var buildings = loc.Map?.GetLayer("Buildings");

        foreach (Placement p in PhotoLayout.All)
        {
            if (!seen.Add(new Point(p.X, p.Y)))
                this.monitor.Log($"Layout problem: duplicate tile ({p.X},{p.Y}) for {p.Npc}.", LogLevel.Warn);

            if (p.X < 0 || p.Y < 0 || p.X >= PhotoLayout.MapWidth || p.Y >= PhotoLayout.MapHeight)
            {
                this.monitor.Log($"Layout problem: {p.Npc} at ({p.X},{p.Y}) is outside the {PhotoLayout.MapWidth}x{PhotoLayout.MapHeight} map.", LogLevel.Warn);
                continue;
            }

            if (buildings is not null && p.X < buildings.LayerWidth && p.Y < buildings.LayerHeight)
            {
                bool blocked = buildings.Tiles[p.X, p.Y] != null;

                // Alex's tile is his chair on purpose.
                if (blocked && p.AnimationName != "alex_sit_left")
                    this.monitor.Log($"Layout warning: {p.Npc}'s tile ({p.X},{p.Y}) has a Buildings tile. The map may differ from the one this layout was built for.", LogLevel.Warn);

                // Penny needs the table directly behind (north of) her.
                if (p.AnimationName == "penny_sit_down" && p.Y > 0 && buildings.Tiles[p.X, p.Y - 1] == null)
                    this.monitor.Log($"Layout warning: no table tile found directly behind Penny at ({p.X},{p.Y - 1}).", LogLevel.Warn);
            }

            foreach (var warp in loc.warps)
            {
                if (warp.X == p.X && warp.Y == p.Y)
                    this.monitor.Log($"Layout warning: {p.Npc} is standing on a warp tile ({p.X},{p.Y}).", LogLevel.Warn);
            }
        }
    }
}
