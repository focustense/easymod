using System;
using System.Collections.Generic;
using System.Linq;

namespace Focus.Environment
{
    public class NullLoadOrderGraph : ILoadOrderGraph
    {
        public bool CanLoad(string pluginName)
        {
            return false;
        }

        public IEnumerable<string> GetAllMasters(string pluginName, bool includeImplicit = false)
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetMissingMasters(string pluginName)
        {
            return Enumerable.Empty<string>();
        }

        public bool IsEnabled(string pluginName)
        {
            return false;
        }

        public bool IsImplicit(string pluginName)
        {
            return false;
        }

        public void SetEnabled(string pluginName, bool enabled)
        {
            throw new InvalidOperationException(
                $"Load order has not been initialized. Use a different instance of {nameof(ILoadOrderGraph)} before " +
                "attempting to change plugin state.");
        }
    }
}
