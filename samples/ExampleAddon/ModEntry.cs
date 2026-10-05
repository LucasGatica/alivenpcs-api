using System.Collections.Concurrent;
using AliveNpcsIntegration;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExampleAddon;

/// <summary>
/// A small addon that uses each part of the AliveNpcs API you can copy:
/// <list type="bullet">
///   <item>a Radio app in the AliveNpcs computer (<see cref="IAliveNpcsComputerApi"/>), whose bulletin is written by AI;</item>
///   <item>gossip when the farmer carries a Prismatic Shard (<see cref="IAliveNpcsApi.InjectGossip"/>);</item>
///   <item>the <c>example_ask</c> console command, which asks a villager a question (<see cref="IAliveNpcsApi.GenerateOutputAsync"/>).</item>
/// </list>
/// </summary>
public sealed class ModEntry : Mod
{
    private const string AliveNpcsId = "Lucas.AliveNpcs";
    private const string PrismaticShard = "(O)74";

    /// <summary>Work that must run on the game thread, queued from AI callbacks.</summary>
    private readonly ConcurrentQueue<Action> _gameThreadWork = new();

    private IAliveNpcsApi? _aliveNpcs;
    private IDisposable? _radioApp;
    private bool _toldShardGossipToday;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += (_, _) => _toldShardGossipToday = false;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Player.InventoryChanged += OnInventoryChanged;

        helper.ConsoleCommands.Add(
            "example_ask",
            "Asks a villager a question through AliveNpcs' AI.\n\nUsage: example_ask <villager> <question>",
            OnAskCommand);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Null when AliveNpcs isn't installed. The manifest asks for 1.7.0 or later, so when it is
        // installed both APIs exist.
        _aliveNpcs = Helper.ModRegistry.GetApi<IAliveNpcsApi>(AliveNpcsId);
        var computer = Helper.ModRegistry.GetApi<IAliveNpcsComputerApi>(AliveNpcsId);
        if (_aliveNpcs == null || computer == null)
        {
            Monitor.Log("AliveNpcs isn't installed, so the example features are off.", LogLevel.Info);
            return;
        }

        var bulletin = new RadioBulletin(_aliveNpcs, Monitor, Helper.Translation);
        var icon = Helper.ModContent.Load<Texture2D>("assets/radio-icon.png");

        _radioApp = computer.RegisterComputerApp(
            ownerModId: ModManifest.UniqueID,
            appId: "Radio",
            getName: () => Helper.Translation.Get("radio.name"),
            iconTexture: icon,
            iconSourceRect: null,
            createMenu: () => Context.IsWorldReady ? new RadioMenu(bulletin, Helper.Translation) : null);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        while (_gameThreadWork.TryDequeue(out var work))
            work();
    }

    /// <summary>Feeds a note to tonight's gossip the first time each day the farmer carries a Prismatic Shard.</summary>
    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        // Gossip is analysed by the host, so only the host injects it.
        if (_aliveNpcs == null || _toldShardGossipToday || !e.IsLocalPlayer || !Context.IsMainPlayer)
            return;

        if (!e.Added.Any(item => item.QualifiedItemId == PrismaticShard))
            return;

        _toldShardGossipToday = true;
        var accepted = _aliveNpcs.InjectGossip(
            ModManifest.UniqueID,
            $"{e.Player.Name} was seen carrying a Prismatic Shard around the valley, grinning from ear to ear.");

        // False is normal: the player may have turned third-party gossip off, or today's limit is reached.
        Monitor.Log(accepted ? "Told the village about the Prismatic Shard." : "AliveNpcs didn't take the gossip today.");
    }

    private void OnAskCommand(string command, string[] args)
    {
        if (_aliveNpcs == null)
        {
            Monitor.Log("AliveNpcs isn't installed.", LogLevel.Warn);
            return;
        }

        if (args.Length < 2)
        {
            Monitor.Log("Usage: example_ask <villager> <question>", LogLevel.Info);
            return;
        }

        // Read everything the prompt needs here, on the game thread.
        var npcName = args[0];
        var question = string.Join(" ", args.Skip(1));
        var displayName = Game1.getCharacterFromName(npcName)?.displayName ?? npcName;
        var systemPrompt =
            $"You are {displayName} from Stardew Valley. Stay in character.\n"
            + $"Personality: {_aliveNpcs.GetDefaultPersonality(npcName)}\n"
            + "Answer in one or two short sentences. No emoji, no stage directions.";

        _ = AskAsync(displayName, systemPrompt, question);
    }

    private async Task AskAsync(string displayName, string systemPrompt, string question)
    {
        string? answer;
        try
        {
            answer = await _aliveNpcs!.GenerateOutputAsync(null, systemPrompt, question);
        }
        catch (Exception ex)
        {
            Monitor.Log($"The question failed: {ex}", LogLevel.Error);
            return;
        }

        // From here on we may be on a background thread. Logging is fine; the game is not.
        if (string.IsNullOrWhiteSpace(answer))
        {
            Monitor.Log("No answer. Is the AI provider set up in AliveNpcs?", LogLevel.Warn);
            return;
        }

        var line = $"{displayName}: {TextCleanup.StripEmoji(answer.Trim())}";
        Monitor.Log(line, LogLevel.Info);
        _gameThreadWork.Enqueue(() =>
        {
            if (Context.IsPlayerFree)
                Game1.drawObjectDialogue(line);
        });
    }
}
