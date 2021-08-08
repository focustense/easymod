namespace Focus.ModManagers
{
    public interface IComponentResolver
    {
        ModComponentInfo ResolveComponentInfo(string componentName);
    }
}
