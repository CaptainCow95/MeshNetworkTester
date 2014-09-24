using System;
using System.Linq;
using System.Threading;

namespace MeshNetworkTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Write("Input a port to run on: ");
            int port = int.Parse(Console.ReadLine());
            Console.Write("Input a comma separated list of servers to connect to: ");
            string servers = Console.ReadLine();
            MeshNetwork.NetworkNode node = new MeshNetwork.NetworkNode("MeshNetworkTester" + port + ".log");
            node.ConnectToNetwork(port, servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new MeshNetwork.NodeProperties(e)));

            MeshNetwork.NodeProperties thisMachine = new MeshNetwork.NodeProperties("127.0.0.1", port);
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Currently running on " + thisMachine.IP.ToString() + ":" + thisMachine.Port.ToString());
                Console.WriteLine();
                Console.WriteLine("Connected Nodes:");
                foreach (var neighbor in node.GetNeighbors())
                {
                    Console.WriteLine(neighbor.IP + ":" + neighbor.Port);
                }

                Thread.Sleep(5000);
            }
        }
    }
}