using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace ClientTest
{
    enum Command
    {
        Connect,
        Disconncet,
        Observe,
        Stop_Observe,
        Observing
    }
    class Client
    {
        Thread m_ReceiveDataThread = null;
        Thread m_ObserveThread = null;
        Socket m_LocalSocket = null;
        public string sn = "12345678910";
        EndPoint remote;
        public Client()
        {
        }

        public void Start(IPAddress address, int port)
        {
            try
            {
                m_LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                EndPoint endPointInfo = new IPEndPoint(address, port) as EndPoint;
                m_LocalSocket.Bind(endPointInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void StartListen()
        {
            m_ReceiveDataThread = new Thread(ReceiveData);
            m_ReceiveDataThread.IsBackground = true;
            m_ReceiveDataThread.Start();
        }

        void ReceiveData()
        {
            byte[] recvBuf = new byte[1024];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
            int recvLength = 0;
            while (true)
            {
                try
                {
                    recvLength = m_LocalSocket.ReceiveFrom(recvBuf, ref remoteEndPoint);
                    if (recvLength == 0)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    //Receive From Server
                    string recvData = Encoding.ASCII.GetString(recvBuf, 0, recvLength);
                    Debug.WriteLine(recvData);
                    if (recvData != null)
                    {
                        string[] arr = recvData.Split(new char[] { '|' }).Where(x => x != "").ToArray();
                        if (arr.Length == 2)
                        {
                            if (Convert.ToInt32(arr[0]) == (int)Command.Observe && arr[1].Equals(this.sn))
                            {
                                Console.WriteLine("Start observe");
                                m_ObserveThread = new Thread(() => StartObserve(remoteEndPoint));
                                m_ObserveThread.Start();
                            }
                            else if (Convert.ToInt32(arr[0]) == (int)Command.Stop_Observe && arr[1].Equals(this.sn))
                            {
                                Console.WriteLine("Stop observe");
                                if (m_ObserveThread != null)
                                    m_ObserveThread.Abort();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

        }

        void StartObserve(EndPoint remoteEndPoint)
        {
            String deviceInfo = String.Format("{0}|{1},RAM,CPU", (int)Command.Observing, this.sn);
            while (true)
            {
                SendTo(deviceInfo, remoteEndPoint);
                Thread.Sleep(3000);
            }
        }

        public bool SendTo(String msg, EndPoint target)
        {
            try
            {
                m_LocalSocket.SendTo(Encoding.UTF8.GetBytes(msg), target);
            }
            catch (System.Exception ex)
            {
                //ShowMsg(String.Format("[{0}]->[{1}] Exception : {2}", "UdpClientEP", "Send", ex.Message));
                Debug.WriteLine(ex.ToString());
            }
            return true;
        }

        public string GetLocalAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        public void Stop()
        {
            if (m_ReceiveDataThread != null)
            {
                m_ReceiveDataThread.Abort();
            }
            if (m_ObserveThread != null)
            {
                m_ObserveThread.Abort();
            }
            if (m_LocalSocket != null)
            {
                m_LocalSocket.Close();
            }
        }
    }
}
