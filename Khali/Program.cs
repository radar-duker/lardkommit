using System.Linq;
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Khali.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Configuration;

namespace Khali {

    public static class Program {

        /*
        HostLobbyTemplate="C:\hduke\hduke.exe" -name {USERNAME} -kalihost [/y{TIMELIMIT}] /v{EPISODE} /l{LEVEL} /s{SKILL} {NOMONSTERS} {SpawnCodes:SPAWN} /p{NUMPLAYERS} /c{PLAYMODE} [-map {USERMAP}] /h{HOSTIP} /k{JOINIP} -net netlist.txt
        JoinLobbyTemplate="C:\hduke\hduke.exe" -name {USERNAME} -kalijoin [/y{TIMELIMIT}] /v{EPISODE} /l{LEVEL} /s{SKILL} {NOMONSTERS} {SpawnCodes:SPAWN} /p{NUMPLAYERS} /c{PLAYMODE} [-map {USERMAP}] /h{JOINIP} /k{HOSTIP} -net netlist.txt
        */

        public static void Main(string[] args) {

            try {

                var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(entry => entry.AddressFamily == AddressFamily.InterNetwork).ToString();
                var tcp = Convert.ToInt32(ConfigurationManager.AppSettings.Get("TCP"));
                var udp = Convert.ToInt32(ConfigurationManager.AppSettings.Get("UDP"));
                var delay = Convert.ToInt32(ConfigurationManager.AppSettings.Get("DELAY"));

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string[] ips = null;

                if (args.Contains("-kalihost")) {

                    var hostIp = args.First(arg => arg.StartsWith("/h")).Replace("/h", "");
                    var endpoint = new IPEndPoint(IPAddress.Parse(hostIp), tcp);

                    var numplayers = Convert.ToInt32(args.First(arg => arg.StartsWith("/p")).Replace("/p", ""));
                    var sockets = new Socket[numplayers - 1];
                    var count = 1;

                    Console.WriteLine($"Listening on {endpoint.Address}:{endpoint.Port}, waiting for {numplayers} players...");
                    socket.Bind(endpoint);
                    socket.Listen(0);

                    while (count < numplayers) {
                        sockets[count - 1] = socket.Accept();
                        Console.WriteLine($"{count}/{numplayers} {sockets[count - 1].RemoteEndPoint} connected!");
                        count++;
                    }

                    ips = sockets.Select(p => p.RemoteEndPoint.ToString().Split(':')[0]).ToArray();

                    var dictionary = new Dictionary<string, object>();

                    dictionary["netlist"] = ips;

                    Thread.Sleep(delay);

                    Console.WriteLine("Broadcasting arguments to clients...");

                    foreach (var s in sockets) {
                        Console.WriteLine($"Sending args to {s.RemoteEndPoint}...");
                        s.Write(dictionary);
                    }

                    Thread.Sleep(delay);

                } else {

                    var hostIp = args.First(arg => arg.StartsWith("/k")).Replace("/k", "");
                    var hostendpoint = new IPEndPoint(IPAddress.Parse(hostIp), tcp);
                    var joinIp = args.First(arg => arg.StartsWith("/h")).Replace("/h", "");
                    var joinendpoint = new IPEndPoint(IPAddress.Parse(joinIp), tcp);

                    Console.WriteLine($"Connecting to {hostendpoint.Address}:{hostendpoint.Port}...");
                    socket.Bind(joinendpoint);
                    socket.Connect(hostendpoint);

                    Console.WriteLine($"Connected to {hostendpoint.Address}:{hostendpoint.Port}!");

                    Console.WriteLine("Waiting for response from host...");

                    var dictionary = socket.Read<Dictionary<string, object>>();

                    Console.WriteLine("Received response!");

                    if (dictionary["netlist"] is string[] net) {
                        ips = net.Where(n => n != $"{joinIp}").Concat(new[] { $"{hostIp}" }).ToArray();
                    }

                }

                var netlist = new[] { $"interface {ip}:{udp}", "mode peer" }.Concat(ips.Select(i => $"allow {i}:{udp}")).ToArray();
                //var duke = args[0].Split('=')[1].Replace(@"""", "");
                //var dir = Path.GetDirectoryName(duke);
                var netpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "netlist.txt");

                Console.WriteLine("Generaing netlist.txt...");
                File.WriteAllLines(netpath, netlist);
                Console.WriteLine($@"File created at {netpath}!");

                //var arguments = new string[args.Length - 2];

                //var index = 0;

                //var filter = new[] { 0, 3, 10, 14, 15 };

                //for (var i = 0; i < args.Length; i++) {
                //    if (!filter.Contains(i)) {
                //        arguments[index] = args[i];
                //        index++;
                //    }
                //}

                //var finalArgs = string.Join(" ", arguments);

                //Console.WriteLine($"Executing {duke} {finalArgs}");

                //var process = new Process {
                //    StartInfo = new ProcessStartInfo(duke, finalArgs) {
                //        WorkingDirectory = dir
                //    }
                //};

                //process.Start();
                //process.WaitForExit();

            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ForegroundColor = ConsoleColor.Gray;
            }

        }

    }

}