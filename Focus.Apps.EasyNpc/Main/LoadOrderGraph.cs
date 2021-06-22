using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Apps.EasyNpc.Main
{
    public class LoadOrderGraph
    {
        private readonly Dictionary<string, LoadOrderNode> nodesByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly LoadOrderNode root;

        public LoadOrderGraph(IEnumerable<PluginInfo> plugins, IReadOnlySet<string> blacklist)
        {
            root = GetOrAddNode("Skyrim.esm");
            foreach (var plugin in plugins)
            {
                var node = GetOrAddNode(plugin.FileName);
                node.IsBlacklisted = blacklist.Contains(plugin.FileName);
                node.IsEnabled = plugin.IsEnabled;
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

        private void Validate(LoadOrderNode node, HashSet<string> visitedNodes = null)
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
        public string PluginName { get; private init; }

        public LoadOrderNode(string pluginName)
        {
            PluginName = pluginName;
        }
    }
}