using System.Linq;
using Content.Server.Administration;
using Content.Server.Parallax;
using Content.Shared.Administration;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

[AdminCommand(AdminFlags.Mapping)]
public sealed class BakePlanetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public string Command => "bakeplanet";
    public string Description => "Bakes a biome into real permanent grid tiles.";
    public string Help => "bakeplanet <mapId> <biome> <radius> [seed]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            shell.WriteError(Help);
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError($"Invalid map id: {args[0]}");
            return;
        }

        var mapId = new MapId(mapInt);

        if (!_mapManager.MapExists(mapId))
        {
            shell.WriteError($"Map does not exist: {mapId}");
            return;
        }

        if (!_protoManager.TryIndex<BiomeTemplatePrototype>(args[1], out var biomeTemplate))
        {
            shell.WriteError($"Biome prototype does not exist: {args[1]}");
            return;
        }

        if (!int.TryParse(args[2], out var radius) || radius <= 0)
        {
            shell.WriteError($"Invalid radius: {args[2]}");
            return;
        }

        int? seed = null;

        if (args.Length == 4)
        {
            if (!int.TryParse(args[3], out var parsedSeed))
            {
                shell.WriteError($"Invalid seed: {args[3]}");
                return;
            }

            seed = parsedSeed;
        }

        var biomeSystem = _entManager.System<BiomeSystem>();

        // Create a real savable grid on this map.
        var gridUid = _mapManager.CreateGridEntity(mapId);
        var grid = _entManager.GetComponent<MapGridComponent>(gridUid);

        shell.WriteLine($"Baking biome {biomeTemplate.ID} onto grid {gridUid} on map {mapId} with radius {radius}...");

        biomeSystem.BakePlanetGrid(gridUid, grid, biomeTemplate, radius, seed);

        shell.WriteLine($"Baked planet grid {gridUid}. Use savegrid on this grid if you want to keep it.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(_entManager), "Map Id");

        if (args.Length == 2)
        {
            var options = _protoManager.EnumeratePrototypes<BiomeTemplatePrototype>()
                .Select(o => new CompletionOption(o.ID, "Biome"));

            return CompletionResult.FromOptions(options);
        }

        if (args.Length == 3)
            return CompletionResult.FromHint("Radius");

        if (args.Length == 4)
            return CompletionResult.FromHint("Seed");

        return CompletionResult.Empty;
    }
}
