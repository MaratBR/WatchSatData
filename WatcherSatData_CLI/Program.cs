using System;
using System.Text;
using CommandLine;
using static WatcherSatData_CLI.Application;

namespace WatcherSatData_CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var application = new Application();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => { Environment.Exit(application.Run(o)); });
        }
    }
}