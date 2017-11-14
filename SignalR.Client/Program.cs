using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;
using SignalR.Server.Model;

namespace CsharpClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            Task.Run(Run).Wait();
        }

        static async Task Run()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/DataTicker")
                .WithConsoleLogger()
                .WithMessagePackProtocol()
                .WithTransport(TransportType.WebSockets)
                .Build();

            await connection.StartAsync();

            Console.WriteLine("Starting connection. Press Ctrl-C to close.");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                cts.Cancel();
            };

            connection.Closed += e =>
            {
                Console.WriteLine("Connection closed with error: {0}", e);

                cts.Cancel();
                return Task.CompletedTask;
            };

            connection.On("serverOpened", async () =>
            {
                await StartStreaming();
            });



            // Keep client running until cancel requested.
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(250);
            }

            async Task StartStreaming()
            {
                var channel = await connection.StreamAsync<MessageData>("GetDataStreaming", CancellationToken.None);
                while (await channel.WaitToReadAsync() && !cts.IsCancellationRequested)
                {
                    while (channel.TryRead(out var stock))
                    {
                        Console.WriteLine($"{stock.DataKey} {stock.DataValue}");
                    }
                }
            }
        }
    }


}
