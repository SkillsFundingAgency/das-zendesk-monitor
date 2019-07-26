﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(ZenWatchFunction.Startup))]

namespace ZenWatchFunction
{
    public class WatcherOnTheWalls
    {
        public WatcherOnTheWalls(ILogger<WatcherOnTheWalls> log)
        {
            this.log = log;
        }

        public async Task<long[]> SearchForTickets()
        {
            log?.LogInformation($"Searching for tickets");
            await Task.Delay(TimeSpan.FromSeconds(5));
            SimulateFailure(0.2);
            return new long[] { 1234, 2345, 3456, 4567 };
        }

        public async Task ShareTicket(long id)
        {
            var ticket = await LoadTicket(id);
            await StartSending(ticket);
            await PostTicket(ticket);
            await FinishedSending(ticket);
        }

        private async Task<string> LoadTicket(long id)
        {
            log?.LogInformation($"Fetching ticket data {id}");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            SimulateFailure(0.2);
            return "hello";
        }

        private async Task StartSending(string id)
        {
            log?.LogInformation($"{id} - Adding tag `sending`");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            SimulateFailure();
        }

        private async Task FinishedSending(string id)
        {
            log?.LogInformation($"{id} - Removing tags");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            SimulateFailure();
        }

        private async Task PostTicket(string id)
        {
            log?.LogInformation($"{id} - Sending to middleware");
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            SimulateFailure();
        }

        private static bool ChanceOfFailure(double chance) => random.NextDouble() < chance;

        private static void SimulateFailure(double chance = 0.02)
        {
            if (ChanceOfFailure(chance))
                throw new Exception($"{chance} of failure!");
        }

        private static readonly Random random = new Random();
        private readonly ILogger<WatcherOnTheWalls> log;
    }
}