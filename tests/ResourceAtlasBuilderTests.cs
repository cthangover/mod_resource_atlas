namespace Cthangover.ResourceAtlas.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Providers;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Validator;

    /// <summary>Map event patches must participate in the ordinary dependency audit.</summary>
    public class ResourceAtlasBuilderTests
    {
        [ModTest("patches/map_event_owner_and_missing_dependency")]
        static void EventOwnership(ModTestContext ctx)
        {
            var root = Path.Combine(Path.GetTempPath(), "atlas_probe_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "owner/map_events/field"));
                Directory.CreateDirectory(Path.Combine(root, "patcher/patches"));
                File.WriteAllText(Path.Combine(root, "owner/map_events/field/props.json"),
                    "{\"Events\":[{\"Id\":\"barrel\",\"ObjectSprite\":\"desert/barrel_closed\"}]}");
                File.WriteAllText(Path.Combine(root, "patcher/patches/rpg_map_event.json"),
                    "{\"Items\":[{\"Id\":\"field/barrel\",\"Solid\":false}]}");
                using var owner = new FolderModFileProvider(Path.Combine(root, "owner"), "owner");
                using var patcher = new FolderModFileProvider(Path.Combine(root, "patcher"), "patcher");
                var mods = new IModInfo[] { new ModInfo { Id = "owner", FileProvider = owner }, new ModInfo { Id = "patcher", FileProvider = patcher } };
                var result = ResourceAtlasBuilder.Build(mods, new ValidationGraph());
                ModAssert.True(result.Provisions.Any(p => p.Kind == "rpg_map_event" && p.Id == "field/barrel" && p.ModId == "owner"), "event owner indexed");
                ModAssert.True(result.Errors.Any(p => p.Kind == AtlasFindingKind.UndeclaredPatch && p.ModId == "patcher"), "missing dependency is an error");
                ModAssert.False(result.Findings.Any(p => p.Kind == AtlasFindingKind.PatchTargetMissing), "target is recognized");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [ModTest("patches/unknown_id_warns_case_does_not_matter")]
        static void PatchOnUnknownId(ModTestContext ctx)
        {
            var root = Path.Combine(Path.GetTempPath(), "atlas_probe_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "owner/items"));
                Directory.CreateDirectory(Path.Combine(root, "patcher/patches"));
                File.WriteAllText(Path.Combine(root, "owner/items/food.json"),
                    "{\"Kind\":\"item\",\"Items\":[{\"Id\":\"food/ration\"}]}");
                File.WriteAllText(Path.Combine(root, "patcher/patches/item.json"),
                    "{\"Items\":[{\"Id\":\"FOOD/RATION\",\"Cost\":1},{\"Id\":\"food/rasion\",\"Cost\":2}]}");
                using var owner = new FolderModFileProvider(Path.Combine(root, "owner"), "owner");
                using var patcher = new FolderModFileProvider(Path.Combine(root, "patcher"), "patcher");
                var mods = new IModInfo[]
                {
                    new ModInfo { Id = "owner", FileProvider = owner },
                    new ModInfo { Id = "patcher", FileProvider = patcher },
                };
                var graph = new ValidationGraph();
                graph.AddDeclaration("item", "food/ration", "owner", "items/food.json", 0);
                var result = ResourceAtlasBuilder.Build(mods, graph);
                var missing = result.Warnings.Where(f => f.Kind == AtlasFindingKind.PatchTargetMissing).ToList();
                ModAssert.True(missing.Count == 1 && missing[0].ResourceId == "food/rasion" && missing[0].File == "patches/item.json",
                    "one warning, for the unknown id: " + string.Join(" | ", result.Findings.Select(f => $"{f.Kind} {f.ResourceKind}:{f.ResourceId} {f.Message}")));
                ModAssert.True(result.Findings.Any(f => f.Kind is AtlasFindingKind.DeclaredPatch or AtlasFindingKind.UndeclaredPatch && f.ResourceId == "FOOD/RATION"),
                    "the upper-case id finds its owner");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        /// <summary>
        /// An integration (<c>rpg_fx__battles</c>, host <c>rpg_fx</c>, partner <c>battle_fx</c>) provides a resource. A consumer
        /// may use it as declared when its depends reach the host and every partner, not only the integration id.
        /// Same shape as the <c>test_integrations</c> fixture, built in memory so the atlas rule is checked on its own.
        /// </summary>
        [ModTest("integrations/consumer_reaches_integration_through_host_and_partners")]
        static void IntegrationIsDeclaredThroughItsHostAndPartners(ModTestContext ctx)
        {
            var root = Path.Combine(Path.GetTempPath(), "atlas_probe_" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (var name in new[] { "rpg_fx", "battle_fx", "rpg_fx__battles", "game_full", "game_partial" })
                    Directory.CreateDirectory(Path.Combine(root, name));
                using var host = new FolderModFileProvider(Path.Combine(root, "rpg_fx"), "rpg_fx");
                using var partner = new FolderModFileProvider(Path.Combine(root, "battle_fx"), "battle_fx");
                using var glue = new FolderModFileProvider(Path.Combine(root, "rpg_fx__battles"), "rpg_fx__battles");
                using var full = new FolderModFileProvider(Path.Combine(root, "game_full"), "game_full");
                using var partial = new FolderModFileProvider(Path.Combine(root, "game_partial"), "game_partial");

                var hostInfo = new ModInfo
                {
                    Id = "rpg_fx", FileProvider = host,
                    Manifest = new ModManifest { Id = "rpg_fx", Integrations = new List<ModIntegration> { new ModIntegration { Id = "battles", With = new List<string> { "battle_fx" } } } },
                };
                var partnerInfo = new ModInfo { Id = "battle_fx", FileProvider = partner, Manifest = new ModManifest { Id = "battle_fx" } };
                var glueInfo = new ModInfo
                {
                    Id = "rpg_fx__battles", FileProvider = glue,
                    Manifest = new ModManifest { Id = "rpg_fx__battles", Depends = new List<ModDependency> { ModDependency.Parse("rpg_fx"), ModDependency.Parse("battle_fx") } },
                };
                var fullInfo = new ModInfo
                {
                    Id = "game_full", FileProvider = full,
                    Manifest = new ModManifest { Id = "game_full", Depends = new List<ModDependency> { ModDependency.Parse("rpg_fx"), ModDependency.Parse("battle_fx") } },
                };
                var partialInfo = new ModInfo
                {
                    Id = "game_partial", FileProvider = partial,
                    Manifest = new ModManifest { Id = "game_partial", Depends = new List<ModDependency> { ModDependency.Parse("rpg_fx") } },
                };
                var mods = new IModInfo[] { hostInfo, partnerInfo, glueInfo, fullInfo, partialInfo };

                var graph = new ValidationGraph();
                graph.AddDeclaration("item", "fx/thing", "rpg_fx__battles", "items/thing.json", 0);
                graph.AddReference("item", "fx/thing", "game_full", "scenarios/a.scenario", 3);
                graph.AddReference("item", "fx/thing", "game_partial", "scenarios/b.scenario", 5);
                var result = ResourceAtlasBuilder.Build(mods, graph);

                ModAssert.True(result.Integrations.ContainsKey("rpg_fx__battles"), "the mounted integration is known with its host");
                ModAssert.True(result.Reaches("game_full", "rpg_fx__battles"), "host and partner in the depends closure: declared");
                ModAssert.False(result.Reaches("game_partial", "rpg_fx__battles"), "partner missing from the closure: not declared");
                ModAssert.True(result.Findings.Any(f => f.Kind == AtlasFindingKind.DeclaredUse && f.ModId == "game_full"), "full consumer is a declared use");
                ModAssert.True(result.Errors.Any(f => f.Kind == AtlasFindingKind.UndeclaredUse && f.ModId == "game_partial"), "partial consumer is an error");
                ModAssert.False(result.Errors.Any(f => f.ModId == "game_full"), "no error for the full consumer");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        /// <summary>Fixture for the C1 rule: a <c>*_pack</c> mod may place objects only on maps it defines itself.</summary>
        [ModTest("placements/pack_on_foreign_map_is_error")]
        static void ForeignPlacement(ModTestContext ctx)
        {
            var root = Path.Combine(Path.GetTempPath(), "atlas_probe_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Path.Combine(root, "owner/maps"));
                Directory.CreateDirectory(Path.Combine(root, "owner/placements/field"));
                Directory.CreateDirectory(Path.Combine(root, "acme_pack/maps"));
                Directory.CreateDirectory(Path.Combine(root, "acme_pack/placements/field"));
                Directory.CreateDirectory(Path.Combine(root, "acme_pack/placements/own"));
                Directory.CreateDirectory(Path.Combine(root, "tool/placements/field"));
                File.WriteAllText(Path.Combine(root, "owner/maps/field.json"), "{\"Kind\":\"rpg_map\",\"Items\":[{\"Id\":\"field\"}]}");
                File.WriteAllText(Path.Combine(root, "owner/placements/field/own.json"), "{\"Kind\":\"rpg_placement\",\"Items\":[{\"Id\":\"field/own\"}]}");
                File.WriteAllText(Path.Combine(root, "acme_pack/maps/own.json"), "{\"Kind\":\"rpg_map\",\"Items\":[{\"Id\":\"own\"}]}");
                File.WriteAllText(Path.Combine(root, "acme_pack/placements/own/own.json"), "{\"Kind\":\"rpg_placement\",\"Items\":[{\"Id\":\"own/chest\"}]}");
                File.WriteAllText(Path.Combine(root, "acme_pack/placements/field/doors.json"), "{\"Kind\":\"rpg_placement\",\"Items\":[{\"Id\":\"field/exit\"},{\"Id\":\"field/exit_2\"}]}");
                File.WriteAllText(Path.Combine(root, "tool/placements/field/x.json"), "{\"Kind\":\"rpg_placement\",\"Items\":[{\"Id\":\"field/x\"}]}");
                using var owner = new FolderModFileProvider(Path.Combine(root, "owner"), "owner");
                using var pack = new FolderModFileProvider(Path.Combine(root, "acme_pack"), "acme_pack");
                using var tool = new FolderModFileProvider(Path.Combine(root, "tool"), "tool");
                var mods = new IModInfo[]
                {
                    new ModInfo { Id = "owner", FileProvider = owner },
                    new ModInfo { Id = "acme_pack", FileProvider = pack },
                    new ModInfo { Id = "tool", FileProvider = tool },
                };
                var result = ResourceAtlasBuilder.Build(mods, new ValidationGraph());
                var foreign = result.Findings.Where(f => f.Kind == AtlasFindingKind.ForeignPlacement).ToList();
                ModAssert.True(foreign.Count == 1, "one finding for the pack's file on the foreign map: " + string.Join(" | ", foreign.Select(f => f.Message)));
                ModAssert.True(foreign[0].Severity == AtlasSeverity.Error && foreign[0].ModId == "acme_pack" && foreign[0].ResourceId == "field"
                    && foreign[0].OtherMods.Contains("owner") && foreign[0].File == "placements/field/doors.json",
                    "error names the pack, the map, its owner and the file");
                ModAssert.False(result.Findings.Any(f => f.Kind == AtlasFindingKind.ForeignPlacement && f.ModId != "acme_pack"),
                    "a mod that is not a pack is not checked");
                ModAssert.True(result.Errors.Any(f => f.Kind == AtlasFindingKind.ForeignPlacement), "the finding is an error");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
