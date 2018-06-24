using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Grains.Placement;

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
            var silo = new SiloHostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.json");
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddConsole();
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "TestSilo";
                    options.ServiceId = "TestSilo";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .UseLocalhostClustering()
                .AddPlacementDirector<RolePlacementStrategy, RolePlacementDirector>()
                .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                .AddMemoryGrainStorageAsDefault()
                .AddMemoryGrainStorage("PubSubStore")
                .Build();

            await silo.StartAsync();

            Console.WriteLine("Silo started.");

            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "TestSilo";
                    options.ServiceId = "TestSilo";
                })
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(IGreeterGrain).Assembly);
                })
                .UseLocalhostClustering()
                .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                .Build();

            await client.Connect();

            Console.WriteLine("Client connected.");
            await SpeedTest(client);

            //await DeviceStateTest(client);

            // Shut down
            await client.Close();
            await silo.StopAsync();
        }

        private static async Task DeviceStateTest(IClusterClient client)
        {
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
        }

        private static async Task SpeedTest(IClusterClient client)
        {
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");

            var stopwatch = new Stopwatch();

            await Task.Run(async () =>
            {
                var name = "peter";
                var request = new GreetRequest()
                {
                    Name = name
                };
                stopwatch.Start();

                var ids = Enumerable.Range(0, 30000)
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
                    //.Select(id => 
                    //{
                    //    var stream = streamProvider.GetStream<GreetRequest>(id, "MyStreamNamespace");
                    //    return stream.OnNextAsync(request);
                    //});

                    await Task.WhenAll(work);

                    //var recv = await friend.GetStatistics();
                    Console.WriteLine($"{ids.Count / stopwatch.Elapsed.TotalSeconds} msg/sec");
                    stopwatch.Restart();
                }
            });
        }
    }
}
