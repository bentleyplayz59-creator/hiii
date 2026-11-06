using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BentleyRestaurantPhotoMode;

/// <summary>Entry point: exactly two console commands plus automatic cleanup hooks.</summary>
internal sealed class ModEntry : Mod
{
    private PhotoSession session = null!;

    public override void Entry(IModHelper helper)
    {
        ModConfig config = helper.ReadConfig<ModConfig>();
        this.session = new PhotoSession(this.Monitor, config);

        helper.ConsoleCommands.Add(
            "bentley_photo_start",
            "Stages 12 vanilla villagers in Bentley's Restaurant for a promo screenshot.",
            (_, _) => this.session.Start());

        helper.ConsoleCommands.Add(
            "bentley_photo_stop",
            "Ends photo mode and releases all villagers.",
            (_, _) => this.session.Stop(StopMode.Restore));

        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) => this.session.Enforce();

    // Day ends: put villagers back, then release.
    private void OnDayEnding(object? sender, DayEndingEventArgs e) => this.session.Stop(StopMode.Restore);

    // New day / save loaded / title screen: never warp anyone, just drop temporary state.
    private void OnDayStarted(object? sender, DayStartedEventArgs e) => this.session.Stop(StopMode.Release);
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => this.session.Stop(StopMode.Release);
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => this.session.Stop(StopMode.Release);

    // Mod unload.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { this.session?.Stop(StopMode.Release); }
            catch (Exception) { /* game is shutting down */ }
        }
        base.Dispose(disposing);
    }
}
