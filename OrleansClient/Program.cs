using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;

namespace OrleansClient
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        static async Task MainAsync(string[] args)
        {
            {
                var client = new ClientBuilder()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "TestSilo";
                        options.ServiceId = "TestSilo";
                    })
                    .ConfigureApplicationParts(parts => { parts.AddApplicationPart(typeof(IDevice).Assembly); })
                    .UseAdoNetClustering(options =>
                    {
                        options.ConnectionString =
                            @"Server=localhost\SQLEXPRESS;Initial Catalog=OrleansTest;Trusted_Connection=True;";
                        options.Invariant = "System.Data.SqlClient";
                    })
                    .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                    .Build();

                await Task.Delay(10000);
                Console.WriteLine("idle");

                await client.Connect();
                Console.WriteLine("Client connected.");

                Console.WriteLine("idle");
                await SpeedTest(client);

                //await DeviceStateTest(client);

                // Shut down
                await client.Close();
            }
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

                var ids = Enumerable.Range(0, 5000)
                    .Select(p => Guid.NewGuid())
                    //.Select(p => new Guid())
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
