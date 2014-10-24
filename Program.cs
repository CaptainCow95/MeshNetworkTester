using MeshNetwork;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MeshNetworkTester
{
    public static class Program
    {
        private static int _port;

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Exception e = (Exception)unhandledExceptionEventArgs.ExceptionObject;
            File.AppendAllText("error" + _port + ".txt", e.Message + "\n" + e.StackTrace);
        }

        private static void Main(String[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            string servers;
            if (args.Length == 2)
            {
                _port = int.Parse(args[0]);
                servers = args[1];
            }
            else
            {
                Console.Write("Input a port to run on: ");
                _port = int.Parse(Console.ReadLine());
                Console.Write("Input a comma separated list of servers to connect to: ");
                servers = Console.ReadLine();
            }

            MeshNetworkNode node = new MeshNetworkNode("MeshNetworkTester" + _port + ".log", LogLevels.Debug);
            node.ConnectToNetwork(_port, servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new NodeProperties(e)).ToList());

            var thisMachine = new NodeProperties("localhost", _port);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Currently running on " + thisMachine.IpAddress + ":" + thisMachine.Port);
                Console.WriteLine();
                var neighbors = node.GetNeighbors();
                Console.WriteLine("Connected Nodes: ");
                foreach (var neighbor in neighbors)
                {
                    Console.WriteLine(neighbor.IpAddress + ":" + neighbor.Port);

                    //node.SendMessage(neighbor, "hi");
                }

                Console.WriteLine("Currently connected to " + neighbors.Count + " nodes.");

                Thread.Sleep(1000);
            }
        }
    }
}