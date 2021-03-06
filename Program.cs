namespace MassTransitStop;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

class Program
{
    public static bool IsServer;
    static async Task Main(string[] args)
    {
        IsServer = args.Length == 0;
        var services = new ServiceCollection();

        if (IsServer)
        { AddServer(services); }
        else
        { AddClient(services); }

        var sp = services.BuildServiceProvider();
        var busControl = sp.GetRequiredService<IBusControl>();
        await busControl.StartAsync();
        Console.WriteLine(IsServer ? "Server started" : "Client started");
        if (IsServer)
        {
            Process.Start("MassTransitStop.exe", "client");
            await Task.Delay(2000); // Wait for client to stop
            await busControl.Publish(new TestMessage() { Key = 1 });
            await Task.Delay(1000);
        }

        Console.WriteLine(IsServer ? "Server stopping ..." : "Client stopping ...");
        await busControl.StopAsync(); // This call takes forever when a message is published, after the Client has stopped
        //await busControl.StopAsync(TimeSpan.FromSeconds(5)); // This call would end after 5 seconds
        Console.WriteLine(IsServer ? "Server stopped" : "Client stopped");
    }

    private static void AddServer(IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestMessageConsumer>();
            x.UsingGrpc((context, cfg) =>
            {
                cfg.Host(h => { h.Host = "0.0.0.0"; h.Port = 9009; });
                cfg.ReceiveEndpoint(Environment.ProcessId.ToString(), endpointCfg =>
                {
                    endpointCfg.ConfigureConsumer<TestMessageConsumer>(context);

                    endpointCfg.DiscardFaultedMessages();
                    endpointCfg.DiscardSkippedMessages();
                });
            });
        });
    }
    private static void AddClient(IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestMessageConsumer>();
            x.UsingGrpc((context, cfg) =>
            {
                cfg.Host(h => h.AddServer(new Uri($"http://127.0.0.1:9009")));
                cfg.ReceiveEndpoint(Environment.ProcessId.ToString(), endpointCfg =>
                {
                    endpointCfg.ConfigureConsumer<TestMessageConsumer>(context);

                    endpointCfg.DiscardFaultedMessages();
                    endpointCfg.DiscardSkippedMessages();
                });
            });
        });
    }
}

