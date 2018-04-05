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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.Serialization;

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
                .ConfigureAppConfiguration(builder => {
                    builder.AddJsonFile("appsettings.json");
                })
                .ConfigureLogging((context, builder) => {
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
                .ConfigureApplicationParts(parts => {
                    parts.AddApplicationPart(typeof(IGreeterGrain).Assembly);
                })
                .UseLocalhostClustering()
                .AddSimpleMessageStreamProvider("SimpleStreamProvider")
                .Build();

            await client.Connect();

            Console.WriteLine("Client connected.");
            
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
                    var work = Enumerable.Range(0, 15_000)
                        .Select(p => friend.SayHello(name));
                      //  .Select(p => stream.OnNextAsync(request));
                    
                    await Task.WhenAll(work);
                    
                    var recv = await friend.GetStatistics();
                    Console.WriteLine($"{recv / stopwatch.Elapsed.TotalSeconds} msg/sec");
                    stopwatch.Restart();
                }
            });
            
            // Shut down
            await client.Close();
            await silo.StopAsync();
        }
    }
}
