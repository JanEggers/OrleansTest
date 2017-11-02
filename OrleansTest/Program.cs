using System;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainInterfaces.Model;
using Orleans;
using Orleans.Providers;
using Orleans.Providers.Streams.SimpleMessageStream;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using Orleans.Storage;
using Orleans.Streams;

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
            var silo = new SiloHost("TestSilo", siloConfig);
            silo.InitializeOrleansSilo();
            silo.StartOrleansSilo();

            Console.WriteLine("Silo started.");

            // Then configure and connect a client.
            var clientConfig = ClientConfiguration.LocalhostSilo();
            clientConfig.RegisterStreamProvider<SimpleMessageStreamProvider>("SimpleStreamProvider");
            var client = new ClientBuilder()
                .UseConfiguration(clientConfig)
                .Build();

            client.Connect().Wait();

            Console.WriteLine("Client connected.");

            //
            // This is the place for your test code.
            //

            var friend = client.GetGrain<IGreeterGrain>(Guid.NewGuid());
            var streamProvider = client.GetStreamProvider("SimpleStreamProvider");
            var stream = streamProvider.GetStream<Request>(Guid.NewGuid(), "MyStreamNamespace");
            
            var send = 0;

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var recv = await friend.GetStatistics();
                    Console.WriteLine($"{send}/{recv}");
                    send = 0;
                }
            });
            
            Task.Run(async () =>
            {
                while (true)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        friend.SayHello("peter");
                        //await stream.OnNextAsync(request);
                        send++;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
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
