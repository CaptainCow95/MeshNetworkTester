using MeshNetwork;
using System;
using System.Linq;
using System.Threading;

namespace MeshNetworkTester
{
    public static class Program
    {
        private static void Main()
        {
            Console.Write("Input a port to run on: ");
            int port = int.Parse(Console.ReadLine());
            Console.Write("Input a comma separated list of servers to connect to: ");
            string servers = Console.ReadLine();
            NetworkNode node = new NetworkNode("MeshNetworkTester" + port + ".log");
            node.ConnectToNetwork(port, servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new NodeProperties(e)));

            var thisMachine = new NodeProperties("127.0.0.1", port);
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Currently running on " + thisMachine.IpAddress + ":" + thisMachine.Port);
                Console.WriteLine();
                Console.WriteLine("Connected Nodes:");
                foreach (var neighbor in node.GetNeighbors())
                {
                    Console.WriteLine(neighbor.IpAddress + ":" + neighbor.Port);
                }

                Thread.Sleep(5000);
            }
        }
    }
}