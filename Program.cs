using MeshNetwork;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace MeshNetworkTester
{
    public static class Program
    {
        private static bool _mesh;
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
            if (args.Length == 3)
            {
                _port = int.Parse(args[0]);
                servers = args[1];
                _mesh = bool.Parse(args[2]);
            }
            else
            {
                Console.Write("Input a port to run on: ");
                _port = int.Parse(Console.ReadLine());
                Console.Write("Input a comma separated list of servers to connect to: ");
                servers = Console.ReadLine();
                Console.Write("Is this a mesh network? (true for yes, false for chord network): ");
                _mesh = bool.Parse(Console.ReadLine());
            }

            NetworkNode node;
            if (_mesh)
            {
                node = new MeshNetworkNode("MeshNetworkTester" + _port + ".log", LogLevels.Warning);
            }
            else
            {
                node = new ChordNetworkNode("MeshNetworkTester" + _port + ".log", LogLevels.Warning);
                node.UpdateNetworkFrequency = 1;
            }

            node.ConnectToNetwork(_port, servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new NodeProperties(e)).ToList());

            var thisMachine = new NodeProperties("localhost", _port);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Currently running on " + thisMachine.IpAddress + ":" + thisMachine.Port);
                Console.WriteLine();
                if (_mesh)
                {
                    var neighbors = node.GetNeighbors();
                    Console.WriteLine("Connected Nodes: ");
                    foreach (var neighbor in neighbors)
                    {
                        Console.WriteLine(neighbor.IpAddress + ":" + neighbor.Port);
                    }

                    Console.WriteLine("Currently connected to " + neighbors.Count + " nodes.");
                }
                else
                {
                    var chordNode = (ChordNetworkNode)node;
                    Console.WriteLine("Predecessor: " + chordNode.Predecessor);
                    Console.WriteLine("Successor: " + (chordNode.Successor == null ? "self" : chordNode.Successor.ToString()));
                }

                foreach (var neighbor in node.GetNeighbors())
                {
                    node.SendMessage(neighbor, "Flush");
                }

                Thread.Sleep(500);
            }
        }
    }
}