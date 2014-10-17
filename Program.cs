using MeshNetwork;
using System;
using System.Linq;

namespace MeshNetworkTester
{
    public static class Program
    {
        private static void Main(String[] args)
        {
            int port;
            string servers;
            if (args.Length == 2)
            {
                port = int.Parse(args[0]);
                servers = args[1];
            }
            else
            {
                Console.Write("Input a port to run on: ");
                port = int.Parse(Console.ReadLine());
                Console.Write("Input a comma separated list of servers to connect to: ");
                servers = Console.ReadLine();
            }

            NetworkNode node = new NetworkNode("MeshNetworkTester" + port + ".log", LogLevels.Info);
            node.ConnectToNetworkAsync(port, servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new NodeProperties(e)).ToList());

            var thisMachine = new NodeProperties("localhost", port);

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
                }

                Console.WriteLine("Currently connected to " + neighbors.Count + " nodes.");
                Console.WriteLine("Press enter to update.");
                Console.ReadLine();
            }
        }
    }
}