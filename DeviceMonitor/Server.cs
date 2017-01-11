using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Collections;

namespace DeviceMonitor
{
    enum Command
    {
        Connect,
        Disconncet,
        Observe,
        Stop_Observe,
        Observing
    }

    class Server
    {
        Thread m_ReceiveDataThread = null;
        Thread m_ReceiveParseThread = null;
        Socket m_LocalSocket = null;
        delegate void UpdateUICallback(String str);
        Action<String> m_CallBack = null;
        Action m_RefreshCallBack = null;
        Queue<String> m_DataQueue = new Queue<String>();
        public List<DeviceInfo> deviceInfo = new List<DeviceInfo>();
        DeviceObserve m_device = null;

        public void Start(IPAddress address, int port)
        {
            Debug.WriteLine("Start");
            m_LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint endPointInfo = new IPEndPoint(address, port) as EndPoint;
            m_LocalSocket.Bind(endPointInfo);
            m_ReceiveDataThread = new Thread(ReceiveData);
            m_ReceiveDataThread.IsBackground = true;
            m_ReceiveDataThread.Start();
        }


        public void Stop()
        {
            try
            {
                if (m_ReceiveDataThread != null)
                    m_ReceiveDataThread.Abort();
                if (m_ReceiveParseThread != null)
                    m_ReceiveParseThread.Abort();
                if (m_LocalSocket != null)
                {
                    m_LocalSocket.Shutdown(SocketShutdown.Both);
                    m_LocalSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
                    //Receive From Client
                    string recvData = Encoding.ASCII.GetString(recvBuf, 0, recvLength);
                    Debug.WriteLine(recvData);
                    m_DataQueue.Enqueue(String.Format("{0}|{1}", remoteEndPoint, recvData));
                    ParseData();
                    if (recvData != null)
                    {
                        //m_DataQueue.Enqueue(String.Format("{0}|{1}", remoteEndPoint.ToString(), recvData));
                        //m_CallBack(String.Format("{0}: {1}", remoteEndPoint.ToString(), recvData));

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        void ParseData()
        {
            if (m_DataQueue.Count > 0)
            {
                DeviceInfo device = new DeviceInfo();
                string[] arr = m_DataQueue.Dequeue().Split(new char[] { '|' }).Where(x => x != "").ToArray();
                if (arr.Length > 0)
                {
                    string[] epArgs = arr[0].Split(':').Where(x => x != "").ToArray();
                    if (epArgs.Length == 2)
                    {
                        device.ip = epArgs[0];
                        device.port = epArgs[1];
                    }
                    int action = Convert.ToInt32(arr[1]);
                    switch (action)
                    {
                        case (int)Command.Connect:
                            string[] arr_1 = arr[2].Split(',').Where(x => x != "").ToArray();
                            if (arr_1.Length != 2)
                                return;
                            device.model = arr_1[0];
                            device.sn = arr_1[1];
                            deviceInfo.Remove(deviceInfo.Find(x => x.sn == device.sn));
                            deviceInfo.Add(device);
                            if (m_RefreshCallBack != null)
                                m_RefreshCallBack();
                            break;
                        case (int)Command.Disconncet:
                            device.sn = arr[2];
                            deviceInfo.Remove(deviceInfo.Find(x => x.sn == device.sn));
                            if (m_RefreshCallBack != null)
                                m_RefreshCallBack();
                            break;
                        case (int)Command.Observing:
                            if (m_CallBack != null)
                                m_CallBack(arr[2]);
                            break;
                    }

                }
            }
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

        public void StartObserve(String sn)
        {
            DeviceInfo selected = deviceInfo.Find(x => x.sn == sn);
            if (selected == null)
                return;
            m_device = new DeviceObserve(this.m_LocalSocket, selected.sn);
            m_device.remote = new IPEndPoint(IPAddress.Parse(selected.ip), Convert.ToInt32(selected.port));
            m_device.Start();
        }

        public void StopObserve()
        {
            if (m_device == null)
                return;
            m_device.Stop();
            m_device = null;
        }

        public void SetCallBackFun(Action<String> callbackFun, Action refreshCallBack)
        {
            m_CallBack = callbackFun;
            m_RefreshCallBack = refreshCallBack;
        }

        public class DeviceInfo
        {
            public String model = "";
            public String sn = "";
            public String ip = "";
            public String port = "";
        }
    }

    class DeviceObserve
    {
        public String m_sn = "";
        Socket m_LocalSocket = null;
        public EndPoint remote;
        public DeviceObserve(Socket socket, String sn)
        {
            m_LocalSocket = socket;
            m_sn = sn;
        }

        public void Start()
        {
            if (m_LocalSocket != null)
            {
                try
                {
                    m_LocalSocket.SendTo(Encoding.UTF8.GetBytes(String.Format("{0}|{1}", (int)Command.Observe, m_sn)), remote);
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        public void Stop()
        {
            if (m_LocalSocket != null)
            {
                try
                {
                    m_LocalSocket.SendTo(Encoding.UTF8.GetBytes(String.Format("{0}|{1}", (int)Command.Stop_Observe, m_sn)), remote);
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
    }
}
