using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
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
            var siloConfig = ClusterConfiguration.LocalhostPrimarySilo(siloPort:8080);
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
                .ConfigureApplicationParts(parts => {
                    parts.AddApplicationPart(typeof(IGreeterGrain).Assembly);
                })
                .Build();

            await client.Connect();

            Console.WriteLine("Client connected.");

            //
            // This is the place for your test code.
            //

            var id = Guid.NewGuid();
            var friend = client.GetGrain<IGreeterGrain>(id);
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<GreetRequest>(id, "MyStreamNamespace");
            
            var stopwatch = new Stopwatch();
            
            await Task.Run(async () =>
            {
                var name = "peter";
                var request = new GreetRequest()
                {
                    Name = name
                };
                stopwatch.Start();

                while (true)
                {
                    var work = Enumerable.Range(0, 10000)
                        .Select(p => friend.SayHello(name));
                    //.Select(stream.OnNextAsync(request));
                    
                    await Task.WhenAll(work);
                    
                    var recv = await friend.GetStatistics();
                    Console.WriteLine($"{recv / stopwatch.Elapsed.TotalSeconds} msg/sec");
                    stopwatch.Restart();
                }
            });
            
            // Shut down
            await client.Close();
            silo.ShutdownOrleansSilo();
        }
    }
}
