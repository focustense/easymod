using System;

namespace Focus.Apps.EasyNpc.Maintenance
{
    public interface IMaintenanceContainer<TKey>
        where TKey : struct
    {
        MaintenanceViewModel<TKey> Maintenance { get; }
    }
}