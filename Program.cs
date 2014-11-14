using MeshNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshNetworkTester
{
    public static class Program
    {
        private static bool _mesh;
        private static NetworkNode _node;
        private static int _port;
        private static Dictionary<int, string> database = new Dictionary<int, string>();

        private static int Hash(string data)
        {
            byte[] bytes = Encoding.Default.GetBytes(data);
            return bytes[0] << 24 + bytes[1] << 16 + bytes[2] << 8 + bytes[3];
        }

        private static void Main(String[] args)
        {
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
                Console.Write("Is this a mesh network or chord network? (\"mesh\" for mesh and \"chord\" for chord): ");
                string networkType = Console.ReadLine();
                if (networkType == "mesh")
                {
                    _mesh = true;
                }
                else if (networkType == "chord")
                {
                    _mesh = false;
                }
                else
                {
                    Console.WriteLine("Unknown network type.");
                    return;
                }
            }

            if (_mesh)
            {
                _node = new MeshNetworkNode("MeshNetworkTester" + _port + ".log", LogLevels.Warning);
            }
            else
            {
                _node = new ChordNetworkNode("MeshNetworkTester" + _port + ".log", LogLevels.Warning);
            }

            _node.ReceivedMessage += node_ReceivedMessage;
            _node.ConnectToNetwork(_port,
                servers.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => new NodeProperties(e))
                    .ToList());
            var thisMachine = new NodeProperties("localhost", _port);

            Console.WriteLine("Please enter a command, or type \"help\" for help.");

            bool running = true;
            while (running)
            {
                Console.Write(" > ");
                string command = Console.ReadLine();

                switch (command)
                {
                    case "custom":
                        Console.WriteLine("Node to send data to: ");
                        string node = Console.ReadLine();
                        Console.WriteLine("Data to send: ");
                        string data = Console.ReadLine();
                        _node.SendMessage(new NodeProperties(node), data);
                        break;
                    case "exit":
                        Console.WriteLine("Shutting down node...");
                        running = false;
                        _node.Disconnect();
                        break;

                    case "help":
                        Console.WriteLine("exit: Exits the program\nstatus: Displays the status of the program\nread: Reads a value from the database\nwrite: Writes a value to the database\ncustom: Sends a custom message (warning: this will also output the message on the receiver's side immediately upon receiving it)");
                        break;

                    case "read":
                        Console.Write("What is the key (integer): ");
                        int key = int.Parse(Console.ReadLine());

                        string value = null;

                        // If the value is local, read it, otherwise go get it.
                        if (database.ContainsKey(key))
                        {
                            value = database[key];
                        }
                        else
                        {
                            if (_node is MeshNetworkNode)
                            {
                                // Search every node for the value.
                                List<MessageResponseResult> results = new List<MessageResponseResult>();
                                foreach (var neighbor in _node.GetNeighbors())
                                {
                                    results.Add(_node.SendMessageResponse(neighbor, "read" + key));
                                }

                                foreach (var result in results)
                                {
                                    if (result.SendResult == SendResults.Success &&
                                        result.ResponseResult == ResponseResults.Success)
                                    {
                                        if (result.ResponseMessage.Data.StartsWith("y"))
                                        {
                                            value = result.ResponseMessage.Data.Substring(1);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Search for the node that contains the value.
                                var result = ((ChordNetworkNode)_node).SendChordMessageResponse(key, "read" + key);
                                if (result.SendResult == SendResults.Success &&
                                    result.ResponseResult == ResponseResults.Success)
                                {
                                    if (result.ResponseMessage.Data.StartsWith("y"))
                                    {
                                        value = result.ResponseMessage.Data.Substring(1);
                                    }
                                }
                            }
                        }

                        if (value == null)
                        {
                            Console.WriteLine("Could not find a value associated with the key " + key + ".");
                        }
                        else
                        {
                            Console.WriteLine("Key: " + key + " Value: " + value);
                        }

                        break;

                    case "status":
                        Console.WriteLine("Currently running on " + thisMachine.IpAddress + ":" + thisMachine.Port);
                        Console.WriteLine();
                        if (_node is MeshNetworkNode)
                        {
                            var neighbors = _node.GetNeighbors();
                            Console.WriteLine("Connected Nodes: ");
                            foreach (var neighbor in neighbors)
                            {
                                Console.WriteLine(neighbor.IpAddress + ":" + neighbor.Port);
                            }

                            Console.WriteLine("Currently connected to " + neighbors.Count + " nodes.");
                        }
                        else
                        {
                            var chordNode = (ChordNetworkNode)_node;
                            Console.WriteLine("id: " + chordNode.Id + " Predecessor: " + chordNode.Predecessor +
                                              " Successor: " +
                                              (chordNode.Successor == null ? "self" : chordNode.Successor.ToString()));
                            Console.WriteLine("Fingers:");
                            foreach (var finger in chordNode.GetFingers())
                            {
                                Console.WriteLine(finger.IpAddress + ":" + finger.Port);
                            }
                        }
                        break;

                    case "write":
                        Console.Write("What is the key (integer): ");
                        key = int.Parse(Console.ReadLine());
                        Console.Write("What is the value (string): ");
                        value = Console.ReadLine();

                        if (_node is MeshNetworkNode)
                        {
                            // If this is a mesh network, write the value locally.
                            SetValue(key, value);
                        }
                        else
                        {
                            // Find the node that should contain the value and write it there.
                            var dataNode = ((ChordNetworkNode)_node).GetNodeContainingId(key);
                            if (dataNode == null || dataNode.Equals(thisMachine))
                            {
                                SetValue(key, value);
                            }
                            else
                            {
                                ((ChordNetworkNode)_node).SendChordMessage(key, "write" + key + " " + value);
                            }
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }

        private static void node_ReceivedMessage(object source, ReceivedMessageEventArgs args)
        {
            if (args.Message.Data.StartsWith("write"))
            {
                string data = args.Message.Data.Substring(5);
                int key = int.Parse(data.Split(' ')[0]);
                string value = data.Substring(data.Split(' ')[0].Length + 1);
                SetValue(key, value);
            }
            else if (args.Message.Data.StartsWith("read"))
            {
                int key = int.Parse(args.Message.Data.Substring(4));
                if (database.ContainsKey(key))
                {
                    _node.SendResponse(args.Message, "y" + database[key]);
                }
                else
                {
                    _node.SendResponse(args.Message, "n");
                }
            }
            else if(args.Message.InResponseToMessage == false)
            {
                Console.WriteLine(args.Message.Data);
            }
        }

        private static void SetValue(int key, string data)
        {
            if (database.ContainsKey(key))
            {
                database[key] = data;
            }
            else
            {
                database.Add(key, data);
            }
        }
    }
}