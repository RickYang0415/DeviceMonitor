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
            listView1.CheckBoxes = true;
            server = new Server();
            this.label1.Text = string.Format("{0}: {1}:{2}", "IP Address: ", server.GetLocalAddress(), Convert.ToString(host));
            server.Start(IPAddress.Any, host);
            server.SetCallBackFun(RefreshDevice);
            this.CenterToScreen();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            server.Stop();
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
                foreach (DeviceInfo item in Server.deviceInfo)
                {
                    String[] arg = { item.sn, item.model };
                    ListViewItem listItem = new ListViewItem(arg);
                    listView1.Items.Add(listItem);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<String> checkList = new List<string>();
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Checked)
                {
                    checkList.Add(item.Text);
                }
            }
            if (checkList.Count != 0)
            {
                server.StartObserve(checkList.ToArray());
                Form2 form2 = new Form2(checkList);
                form2.SetCallBack(StopObserve);
                form2.Show();
                this.button1.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopObserve();
            this.Close();
        }

        void StopObserve()
        {
            server.StopObserve();
            this.button1.Enabled = true;
        }
    }
}
