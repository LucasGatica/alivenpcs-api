using AliveNpcsIntegration;
using StardewModdingAPI;
using StardewValley;

namespace ExampleAddon;

/// <summary>
/// Today's radio bulletin, written once a day by AliveNpcs' AI. Every request uses the player's API key,
/// so the text is kept for the rest of the day instead of being asked for each time the app opens.
/// </summary>
internal sealed class RadioBulletin
{
    private const string SystemPrompt =
        "You are the host of a tiny local radio station in Pelican Town, Stardew Valley. "
        + "Cozy and upbeat. Two or three short sentences. No emoji, no lists, no stage directions.";

    private readonly IAliveNpcsApi _aliveNpcs;
    private readonly IMonitor _monitor;
    private readonly ITranslationHelper _translation;
    private readonly object _lock = new();

    private int _day = -1;
    private string? _text;

    public RadioBulletin(IAliveNpcsApi aliveNpcs, IMonitor monitor, ITranslationHelper translation)
    {
        _aliveNpcs = aliveNpcs;
        _monitor = monitor;
        _translation = translation;
    }

    /// <summary>Today's bulletin, or null while it is being written. Call it from the game thread.</summary>
    public string? Today()
    {
        var today = Game1.Date.TotalDays;
        lock (_lock)
        {
            if (_day == today)
                return _text;

            _day = today;
            _text = null;
        }

        // The prompt reads the save, so it is built here on the game thread, before anything is awaited.
        _ = WriteAsync(today, BuildUserPrompt());
        return null;
    }

    private string BuildUserPrompt()
    {
        var villagers = _aliveNpcs.GetAvailableNpcNames().ToList();
        var villager = villagers.Count > 0 ? villagers[Game1.Date.TotalDays % villagers.Count] : "Lewis";
        var weather = Game1.isRaining ? "rain" : Game1.isSnowing ? "snow" : "sunny";

        return $"Morning bulletin for {Game1.CurrentSeasonDisplayName} {Game1.dayOfMonth}, year {Game1.year}. "
               + $"Weather: {weather}. Mention {villager} in passing.";
    }

    private async Task WriteAsync(int day, string userPrompt)
    {
        string? text = null;
        try
        {
            text = await _aliveNpcs.GenerateOutputAsync(null, SystemPrompt, userPrompt);
        }
        catch (Exception ex)
        {
            _monitor.Log($"The radio bulletin couldn't be written: {ex.Message}", LogLevel.Warn);
        }

        // A failed request isn't retried until tomorrow, so a missing API key doesn't cause a request per frame.
        var result = string.IsNullOrWhiteSpace(text)
            ? _translation.Get("radio.static").ToString()
            : TextCleanup.StripEmoji(text.Trim());

        lock (_lock)
        {
            if (_day == day)
                _text = result;
        }
    }
}
