using Cthangover.Core.Mods;
using Cthangover.Core.Utils;
using Godot;

namespace Cthangover.ResourceAtlas
{
    using Cthangover.Core.Games;
    using Cthangover.Core.Games.LaunchParameters;
    /// <summary>
    /// Activates the atlas only on demand: the <c>--resource-atlas</c> user
    /// arg (after <c>--</c>) queues <see cref="ResourceAtlasDriverNode"/>,
    /// which waits for the boot, writes the report and quits. Optional
    /// <c>--atlas-out=&lt;dir&gt;</c> (res://, user:// or absolute) sets the
    /// report directory, <c>--timeout=&lt;s&gt;</c> the watchdog.
    /// </summary>
    public class ResourceAtlasInitializer : IModInitializer
    {
        public const string Flag = "--" + ResourceAtlasParameter.Name;
        private const string DefaultOut = "user://resource_atlas";

        private static bool queued;

        public void OnModLoaded(string modId) { }

        public void OnModResourcesReady()
        {
            if (queued)
                return;

            var args = GameLaunchArgs.Current;
            if (!args.Contains<ResourceAtlasParameter>())
                return;

            var output = args.Get<AtlasOutputParameter>().Value ?? DefaultOut;
            var timeout = args.Get<TimeoutParameter>().Value;

            queued = true;
            var directory = ProjectSettings.GlobalizePath(output);
            GameLogger.Log("RESOURCE_ATLAS", $"resource atlas requested, report directory '{directory}'");
            ModRoot.AddDeferred(new ResourceAtlasDriverNode(directory, timeout > 0 && timeout <= float.MaxValue ? (float)timeout : 120f));
        }
    }
}
