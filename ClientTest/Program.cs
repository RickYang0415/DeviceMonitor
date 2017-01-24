using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ClientTest
{
    class Program
    {

        static void Main(string[] args)
        {
            Client client = new Client();
            client.Start(IPAddress.Any, 0);
            String address = "192.168.67.47";
            int port = 5488;
            IPEndPoint endPointInfo = new IPEndPoint(IPAddress.Parse(address), port);
            //string command = String.Format("{0}|{1},{2}", Convert.ToInt16(Command.Connect), "Rick", "11111111");
            // Connect
            string command = String.Format("0|Butterfly,{0}", client.sn);// 0|Butterfly, 12345678910
            Console.WriteLine("Enter command to send...");
            //command = Console.ReadLine();
            client.SendTo(command, endPointInfo);
            Console.WriteLine("Waiting command...");
            client.StartListen();
            Console.ReadLine();
            // Disconnect
            //command = String.Format("{0}|{1}", (int)Command.Disconncet, client.sn);
            //client.SendTo(command, endPointInfo);
            //client.Stop();
        }
    }
}
