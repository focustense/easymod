using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Environment
{
    public class LoadOrderGraph : ILoadOrderGraph
    {
        private readonly Dictionary<string, LoadOrderNode> nodesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly LoadOrderNode root;

        public LoadOrderGraph(IEnumerable<PluginInfo> plugins, IReadOnlySet<string> blacklist)
        {
            root = GetOrAddNode("Skyrim.esm");
            foreach (var plugin in plugins)
            {
                var node = GetOrAddNode(plugin.FileName);
                node.IsBlacklisted = !plugin.IsReadable || blacklist.Contains(plugin.FileName);
                if (node.IsBlacklisted)
                    // This would normally be handled by Validate(), but it's possible that the node isn't actually
                    // reachable in the graph.
                    node.CanLoad = false;
                node.IsEnabled = plugin.IsEnabled;
                node.IsImplicit = plugin.IsImplicit;
                foreach (var master in plugin.Masters)
                {
                    var masterNode = GetOrAddNode(master);
                    masterNode.Children.Add(node);
                    node.Masters.Add(masterNode);
                }
            }
            Validate(root, null);
        }

        public bool CanLoad(string pluginName)
        {
            return nodesByName.TryGetValue(pluginName, out var node) && node.CanLoad;
        }

        public IEnumerable<string> GetAllMasters(string pluginName, bool includeImplicit)
        {
            if (!nodesByName.TryGetValue(pluginName, out var node))
                return Enumerable.Empty<string>();
            var masterNodes = includeImplicit ?
                GetMastersRecursive(node, new()).Where(p => p.PluginName != pluginName) :
                node.Masters;
            return masterNodes.Select(x => x.PluginName);
        }

        public IEnumerable<string> GetAllPluginNames()
        {
            return nodesByName.Values.Select(x => x.PluginName);
        }

        private IEnumerable<LoadOrderNode> GetMastersRecursive(LoadOrderNode node, HashSet<LoadOrderNode> visitedNodes)
        {
            if (visitedNodes.Contains(node))
                return Enumerable.Empty<LoadOrderNode>();
            visitedNodes.Add(node);
            return node.Masters.SelectMany(x => GetMastersRecursive(x, visitedNodes)).Append(node);
        }

        public IEnumerable<string> GetMissingMasters(string pluginName)
        {
            return nodesByName.TryGetValue(pluginName, out var node) ?
                node.Masters.Where(x => !x.IsEnabled || !x.CanLoad).Select(x => x.PluginName) :
                Enumerable.Empty<string>();
        }

        public bool IsEnabled(string pluginName)
        {
            return nodesByName.TryGetValue(pluginName, out var node) && node.IsEnabled;
        }

        public bool IsImplicit(string pluginName)
        {
            return nodesByName.TryGetValue(pluginName, out var node) && node.IsImplicit;
        }

        public void SetEnabled(string pluginName, bool enabled)
        {
            // Changing the state does NOT check whether or not a node "can" be enabled based on its own masters.
            // These are separate concepts, and kept separate to preserve existing selections. For example, if we turn
            // off Dawnguard.esm and it subsequently invalidates a ton of downstream plugins, like USSEP and LOTD, and
            // then re-enable Dawnguard, then those plugins should be re-enabled (unless they were already disabled).
            if (!nodesByName.TryGetValue(pluginName, out var node))
                return;
            node.IsEnabled = enabled;
            Validate(node);
        }

        private LoadOrderNode GetOrAddNode(string pluginName)
        {
            if (!nodesByName.TryGetValue(pluginName, out var node))
            {
                node = new LoadOrderNode(pluginName);
                nodesByName.Add(pluginName, node);
            }
            return node;
        }

        private void Validate(LoadOrderNode node, HashSet<string>? visitedNodes = null)
        {
            if (visitedNodes == null)
                visitedNodes = new(StringComparer.OrdinalIgnoreCase);
            if (visitedNodes.Contains(node.PluginName))
                // Prevent cycles
                return;
            visitedNodes.Add(node.PluginName);
            try
            {
                node.CanLoad = !node.IsBlacklisted && node.Masters.All(x => x.CanLoad && x.IsEnabled);
                foreach (var child in node.Children)
                    Validate(child, visitedNodes);
            }
            finally
            {
                visitedNodes.Remove(node.PluginName);
            }
        }
    }

    class LoadOrderNode
    {
        public List<LoadOrderNode> Children { get; private init; } = new();
        public List<LoadOrderNode> Masters { get; private init; } = new();

        public bool CanLoad { get; set; } = true;
        public bool IsBlacklisted { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsImplicit { get; set; }
        public string PluginName { get; private init; }

        public LoadOrderNode(string pluginName)
        {
            PluginName = pluginName;
        }
    }
}