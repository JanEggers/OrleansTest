using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Streams;
using Orleans.Providers.Streams.SimpleMessageStream;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using Orleans.Serialization;
using Orleans.Storage;

namespace OrleansTest
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            // First, configure and start a local silo
            var siloConfig = ClusterConfiguration.LocalhostPrimarySilo(siloPort: 8080);
            siloConfig.Globals.RegisterStreamProvider<SimpleMessageStreamProvider>("SimpleStreamProvider");
            siloConfig.Globals.RegisterStorageProvider<MemoryStorage>("Default");
            siloConfig.Globals.RegisterStorageProvider<MemoryStorage>("PubSubStore");
            siloConfig.Globals.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();
            var silo = new SiloHost("TestSilo", siloConfig);
            silo.InitializeOrleansSilo();
            silo.StartOrleansSilo();

            Console.WriteLine("Silo started.");

            // Then configure and connect a client.
            var clientConfig = ClientConfiguration.LocalhostSilo();
            clientConfig.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();
            clientConfig.RegisterStreamProvider<SimpleMessageStreamProvider>("SimpleStreamProvider");
            var client = new ClientBuilder()
                .UseConfiguration(clientConfig)
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(IGreeterGrain).Assembly);
                })
                .Build();

            await client.Connect();

            Console.WriteLine("Client connected.");
            //await SpeedTest(client);

            var repository = client.GetDeviceRepository();

            var deviceId1 = "device1";
            var device1 = await repository.CreateDevice(deviceId1);
            var statusStreamId = await device1.GetStatusStreamId();

            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<DeviceStatus>(statusStreamId, nameof(DeviceStatus));

            var handle = await stream.SubscribeAsync(async (s, token) => Console.WriteLine(s.OperationStatus.ToString()));

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            await handle.UnsubscribeAsync();

            // Shut down
            await client.Close();
            silo.ShutdownOrleansSilo();
        }

        private static async Task SpeedTest(IClusterClient client)
        {
            //
            // This is the place for your test code.
            //

            //var id = Guid.NewGuid();
            //var friend = client.GetGrain<IGreeterGrain>(id);
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            //var stream = streamProvider.GetStream<GreetRequest>(id, "MyStreamNamespace");

            var stopwatch = new Stopwatch();

            await Task.Run(async () =>
            {
                var name = "peter";
                var request = new GreetRequest()
                {
                    Name = name
                };
                stopwatch.Start();

                var ids = Enumerable.Range(0, 100000)
                    .Select(p => new Guid())
                    .ToList();

                while (true)
                {
                    var work = ids
                        .Select(id =>
                        {
                            var friend = client.GetGrain<IGreeterGrain>(id);
                            return friend.SayHello(name);
                        });
                    //.Select(stream.OnNextAsync(request));

                    await Task.WhenAll(work);

                    //var recv = await friend.GetStatistics();
                    Console.WriteLine($"{ids.Count / stopwatch.Elapsed.TotalSeconds} msg/sec");
                    stopwatch.Restart();
                }
            });
        }
    }
}
