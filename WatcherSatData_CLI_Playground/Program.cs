﻿using System;
using System.Linq;
using System.ServiceModel;
using WatchSatData;
using WatchSatData.DataStore;

namespace WatcherSatData_CLI_Playground
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var binding = new NetNamedPipeBinding();
            var endpoint =
                new EndpointAddress("net.pipe://localhost/birdsWatcher_30c58e1c-300d-4dfb-ae9b-01da83d5c7d6/v1");
            Console.WriteLine($"Connecting to {endpoint.Uri.AbsoluteUri}");

            using (var chanFactory = new ChannelFactory<IService>(binding, endpoint))
            {
                IService client = null;

                try
                {
                    client = chanFactory.CreateChannel();

                    client.GetAllDirectories().ContinueWith(r =>
                    {
                        var list = r.Result.ToList();
                        Console.WriteLine($"Found {list.Count} items");

                        foreach (var item in r.Result)
                            Console.WriteLine($"{item.FullPath} - {item.Alias} - {item.AddedAt}");
                    }).Wait();
                    client.CreateDirectory(new DirectoryCleanupConfig
                    {
                        Alias = "hiTHre!",
                        FullPath = Guid.NewGuid().ToString(),
                        MaxAge = 42
                    }).Wait();

                    ((ICommunicationObject) client).Close();
                    chanFactory.Close();
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                    (client as ICommunicationObject)?.Abort();
                }
            }
        }
    }
}