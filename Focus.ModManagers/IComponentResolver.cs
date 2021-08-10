using System.Threading.Tasks;

namespace Focus.ModManagers
{
    public interface IComponentResolver
    {
        Task<ModComponentInfo> ResolveComponentInfo(string componentName);
    }
}
