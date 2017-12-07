using System;
using System.Diagnostics;
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
        static void Main(string[] args)
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
                .AddApplicationPart(typeof(IGreeterGrain).Assembly)
                .Build();

            client.Connect().Wait();

            Console.WriteLine("Client connected.");

            //
            // This is the place for your test code.
            //

            var id = Guid.NewGuid();
            var friend = client.GetGrain<IGreeterGrain>(id);
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<GreetRequest>(id, "MyStreamNamespace");
            
            var send = 0;
            var stopwatch = new Stopwatch();
            
            Task.Run(async () =>
            {
                var name = "peter";
                var request = new GreetRequest()
                {
                    Name = name
                };
                stopwatch.Start();

                while (true)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        //await friend.SayHello(name);
                        await stream.OnNextAsync(request);
                        send++;
                    }
                    var recv = await friend.GetStatistics();
                    Console.WriteLine($"{send}/{recv} {send/stopwatch.Elapsed.TotalSeconds} msg/sec");
                    send = 0;
                    stopwatch.Restart();
                }
            });

            Console.WriteLine("\nPress Enter to terminate...");
            Console.ReadLine();

            // Shut down
            client.Close();
            silo.ShutdownOrleansSilo();
        }
    }
}
