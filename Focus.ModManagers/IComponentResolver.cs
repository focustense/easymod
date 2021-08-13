using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Focus.ModManagers
{
    public interface IComponentResolver
    {
        Task<ModComponentInfo> ResolveComponentInfo(string componentName);
    }

    public record KeyedComponent(string Key, ModComponentInfo Component);

    public static class ComponentResolverExtensions
    {
        public static async Task<IEnumerable<KeyedComponent>> ResolveAll(
            this IComponentResolver resolver, IEnumerable<string> names)
        {
            var resolveTasks = names.Select(async b => new KeyedComponent(b, await resolver.ResolveComponentInfo(b)));
            var resolved = await Task.WhenAll(resolveTasks);
            return resolved.NotNull();
        }
    }
}
