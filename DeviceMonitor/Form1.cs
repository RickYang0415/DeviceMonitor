using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace DeviceMonitor
{
    public partial class Form1 : Form
    {
        Server server;
        const int host = 5488;
        delegate void UICallBack(string myStr);
        delegate void UI_Refresh_CallBack();
        public Form1()
        {
            InitializeComponent();
            listView1.View = View.Details;
            listView1.Columns.Add("SN", 150);
            listView1.Columns.Add("Model", 150);

            server = new Server();
            this.label1.Text = string.Format("{0}: {1}:{2}", "IP Address: ", server.GetLocalAddress(), Convert.ToString(host));
            server.Start(IPAddress.Any, host);
            server.SetCallBackFun(UpdateUI, RefreshDevice);

            timer1.Enabled = false;
            this.CenterToScreen();
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            server.Stop();
        }

        public void UpdateUI(string myStr)
        {
            if (this.InvokeRequired)
            {
                UICallBack ui = new UICallBack(UpdateUI);
                this.Invoke(ui, myStr);
            }
            else
            {
                this.textBox1.Text += myStr + "\r\n";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            foreach (Server.DeviceInfo item in server.deviceInfo)
            {
                String[] arg = { item.model, item.sn };
                ListViewItem listItem = new ListViewItem(arg);
                listView1.Items.Add(listItem);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //RefreshDevice();
            this.textBox1.Text = "";
        }

        public void RefreshDevice()
        {
            if (this.InvokeRequired)
            {
                UI_Refresh_CallBack ui = new UI_Refresh_CallBack(RefreshDevice);
                this.Invoke(ui);
            }
            else
            {
                listView1.Items.Clear();
                foreach (Server.DeviceInfo item in server.deviceInfo)
                {
                    String[] arg = { item.sn, item.model };
                    ListViewItem listItem = new ListViewItem(arg);
                    listView1.Items.Add(listItem);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.button1.Text.Equals("Start"))
            {
                if (listView1.SelectedItems.Count != 0)
                {
                    String sn = listView1.SelectedItems[0].Text;
                    server.StartObserve(sn);
                    this.listView1.Enabled = false;
                    this.button1.Text = "Stop";
                }
            }
            else
            {
                server.StopObserve();
                this.listView1.Enabled = true;
                this.button1.Text = "Start";
            }
        }
    }
}
