using System;
using System.Collections.Generic;
using System.Linq;

namespace Cthangover.ResourceAtlas
{
    /// <summary>
    /// Everything the atlas learned in one pass: load order and depends
    /// closures, every provision/request/patch, and the findings derived from
    /// them. Report writers and the test driver read only this object.
    /// </summary>
    public sealed class ResourceAtlasResult
    {
        public List<string> LoadOrder { get; } = new();

        public Dictionary<string, List<string>> Depends { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, HashSet<string>> Closures { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Integrations mounted in this set (<c>&lt;host&gt;__&lt;id&gt;</c>), with their host and partners. An integration
        /// is never a dependency of its consumers, so <see cref="Reaches"/> counts it through them.
        /// </summary>
        public Dictionary<string, AtlasIntegration> Integrations { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Whether <paramref name="consumerId"/> may use <paramref name="providerId"/> as declared: the provider is in the
        /// consumer's transitive depends, or it is a mounted integration whose host and every partner are there.
        /// </summary>
        public bool Reaches(string consumerId, string providerId)
        {
            var closure = Closures[consumerId];
            if (closure.Contains(providerId))
                return true;
            return Integrations.TryGetValue(providerId, out var link)
                && closure.Contains(link.Host)
                && link.With.All(closure.Contains);
        }

        public List<AtlasProvision> Provisions { get; } = new();

        /// <summary>Provisions of installed mods that are not loaded (only when the atlas runs inside a game).</summary>
        public List<AtlasProvision> OutsideProvisions { get; } = new();

        public List<AtlasRequest> Requests { get; } = new();

        public List<AtlasPatch> Patches { get; } = new();

        public List<AtlasPlacement> Placements { get; } = new();

        public List<AtlasFinding> Findings { get; } = new();

        /// <summary>Keys the kernel provides (e.g. scenario actions from Core); always available, never a cross-mod link.</summary>
        public HashSet<(string Kind, string Id)> KernelKeys { get; } = new();

        public IEnumerable<AtlasFinding> Errors => Findings.Where(f => f.Severity == AtlasSeverity.Error);

        public IEnumerable<AtlasFinding> Warnings => Findings.Where(f => f.Severity == AtlasSeverity.Warning);

        /// <summary>Mods with at least one error finding, in load order.</summary>
        public List<string> FailingMods()
        {
            var failing = new HashSet<string>(Errors.Select(f => f.ModId), StringComparer.Ordinal);
            return LoadOrder.Where(failing.Contains).ToList();
        }
    }

    /// <summary>A mounted integration: its host mod and the partners it was mounted with.</summary>
    public sealed record AtlasIntegration(string Host, IReadOnlyList<string> With);
}
