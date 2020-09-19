using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WatcherSatData_CLI.Application;

namespace WatcherSatData_CLI
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var application = new Application();
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o =>
                   {
                       Environment.Exit(application.Run(o));
                   });

        }
    }
}
