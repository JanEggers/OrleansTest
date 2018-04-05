using Orleans;

namespace GrainInterfaces
{
    public static class DeviceRepositoryExtensions
    {
        public static IDeviceRepository GetDeviceRepository(this IGrainFactory factory)
        {
            return factory.GetGrain<IDeviceRepository>(nameof(IDeviceRepository));
        }
    }
}
