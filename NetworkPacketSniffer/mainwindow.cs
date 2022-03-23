using SharpPcap;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PacketDotNet;


namespace NetworkPacketSniffer
{
    public partial class mainwindow : Form
    {
        public CaptureDeviceList devices;
        public ICaptureDevice? curr_dev = null;
        private PacketArrivalEventHandler arrivalEventHandler;

        private List<RawCapture> rawPacketList = new List<RawCapture>();
        private object QueueLock = new object();
        private System.Threading.Thread backgroundThread;
        private bool BackgroundThreadStop;
        

        public mainwindow()
        {
            InitializeComponent();
            devices = CaptureDeviceList.Instance;
            foreach (ICaptureDevice dev in devices)
            {
                comboBox1.Items.Add(dev.Description);
            }
            comboBox1.SelectedIndex = 0;
            listView1.View = View.Details;
            listView1.Columns.Add("序号", 50, HorizontalAlignment.Left);
            listView1.Columns.Add("时间", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("源MAC", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("目标MAC", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("长度", 80, HorizontalAlignment.Left);
            listView1.Columns.Add("链路层协议", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("网络层协议", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("会话层协议", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("应用层协议", 100, HorizontalAlignment.Left);
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("选择网卡:",dev.Description);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("首先选择网卡", "warning");
                return;
            }
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("显示网卡信息");
            MessageBox.Show(dev.ToString(), "info");
        }


        private void button3_Click(object sender, EventArgs e)
        {
            //开始捕获
            var i = comboBox1.SelectedIndex;
            if (i < 0)
            {
                MessageBox.Show("首先选择网卡", "warning");
                return;
            }

            if(curr_dev != null)
            {
                MessageBox.Show("正在捕获中", "warning");
                return;
            }

            rawPacketList = new List<RawCapture>();
            BackgroundThreadStop = false;
            backgroundThread = new System.Threading.Thread(BackgroundThread);
            backgroundThread.Start();

            curr_dev = devices[i];
            arrivalEventHandler = new PacketArrivalEventHandler(device_OnPacketArrival);
            curr_dev.OnPacketArrival += arrivalEventHandler;
            curr_dev.Open();

            // Start the capturing process
            curr_dev.StartCapture();
            Debug.WriteLine("Listening on {0}", curr_dev.Description);

            button3.Enabled = false;
            button4.Enabled = true;

        }

        void device_OnPacketArrival(object sender, PacketCapture e)
        {
            lock (QueueLock)
            {
                rawPacketList.Add(e.GetPacket());
            }
            
        }

        private void BackgroundThread()
        {
            Debug.WriteLine("线程开启");
            Control.CheckForIllegalCrossThreadCalls = false;
            listView1.Items.Clear();
            var count = 0;


            while (!BackgroundThreadStop)
            {
                bool shouldSleep = true;
                lock (QueueLock)
                {
                    if (count < rawPacketList.Count)  
                        shouldSleep = false;                   
                }
                if (shouldSleep)
                {
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    var rawPacket = rawPacketList[count];
                    Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                    ListViewItem item = new ListViewItem();
                    item.SubItems[0].Text = count.ToString();
                    item.SubItems.Add(rawPacket.Timeval.Date.ToLocalTime().TimeOfDay.ToString());
                    var eth = (EthernetPacket)packet;
                    item.SubItems.Add(eth.SourceHardwareAddress.ToString());
                    item.SubItems.Add(eth.DestinationHardwareAddress.ToString());
                    item.SubItems.Add(rawPacket.Data.Length.ToString());
                    item.SubItems.Add(rawPacket.LinkLayerType.ToString());
                    item.SubItems.Add(eth.Type.ToString());
                    if (eth.PayloadPacket is IPPacket)
                    {
                        IPPacket ippacket = (IPPacket)eth.PayloadPacket;
                        item.SubItems.Add(ippacket.Protocol.ToString());
                    }



                    listView1.Items.Add(item);
                    count++;



                }
            }
            Debug.WriteLine("线程结束");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (curr_dev == null)
            {
                return;
            }
            curr_dev.StopCapture();
            curr_dev.Close();
            curr_dev.OnPacketArrival -= arrivalEventHandler;
            curr_dev = null;
            button3.Enabled = true;
            button4.Enabled = false;

            BackgroundThreadStop = true;
            backgroundThread.Join();

        }

        private int select_index = -1;


        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("触发了listview change函数");
            int count = listView1.SelectedItems.Count;
            if (count > 0)
            {
                select_index = int.Parse(listView1.SelectedItems[0].SubItems[0].Text);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //链路层信息
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            MessageBox.Show(packet.ToString(), "info");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //arp
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethpacket = (EthernetPacket)packet;
            if (ethpacket.PayloadPacket is ArpPacket)
            {
                ArpPacket arppacket = (ArpPacket)ethpacket.PayloadPacket;
                MessageBox.Show(arppacket.ToString(), "info");
            }
            else
                MessageBox.Show("这个包的网络层协议是" + ethpacket.PayloadPacket.GetType().ToString(), "info");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //ip
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethpacket = (EthernetPacket)packet;
            if (ethpacket.PayloadPacket is IPPacket)
            {
                IPPacket ippacket = (PacketDotNet.IPPacket)ethpacket.PayloadPacket;
                MessageBox.Show(ippacket.ToString(), "info");
            }
            else
                MessageBox.Show("这个包的网络层协议是" + ethpacket.PayloadPacket.GetType().ToString(), "info");
            

        }

        private void button7_Click(object sender, EventArgs e)
        {
            //tcp
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethpacket = (EthernetPacket)packet;
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            
            if (tcpPacket != null)
            {
                MessageBox.Show(tcpPacket.ToString(StringOutputType.VerboseColored), "info");

                var ipPacket = (PacketDotNet.IPPacket)tcpPacket.ParentPacket;
                System.Net.IPAddress srcIp = ipPacket.SourceAddress;
                System.Net.IPAddress dstIp = ipPacket.DestinationAddress;
                int srcPort = tcpPacket.SourcePort;
                int dstPort = tcpPacket.DestinationPort;

      
                Debug.WriteLine("{0}:{1} -> {2}:{3}",
                    srcIp, srcPort, dstIp, dstPort);
            }
            else if(ethpacket.PayloadPacket is IPPacket)
            {
                IPPacket ippacket = (PacketDotNet.IPPacket)ethpacket.PayloadPacket;
                MessageBox.Show("不是tcp，而是" + ippacket.Protocol.ToString(), "info");
            }
            else
                MessageBox.Show("这个包的网络层协议是" + ethpacket.PayloadPacket.GetType().ToString() +"，而不是ip", "info");

        }

        private void button8_Click(object sender, EventArgs e)
        {
            //udp
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethpacket = (EthernetPacket)packet;
            var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();

            if (udpPacket != null)
            {
                MessageBox.Show(udpPacket.ToString(StringOutputType.VerboseColored), "info");
            }
            else if (ethpacket.PayloadPacket is IPPacket)
            {
                IPPacket ippacket = (PacketDotNet.IPPacket)ethpacket.PayloadPacket;
                MessageBox.Show("不是udp，而是" + ippacket.Protocol.ToString(), "info");
            }
            else
                MessageBox.Show("这个包的网络层协议是" + ethpacket.PayloadPacket.GetType().ToString() + "，而不是ip", "info");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethpacket = (EthernetPacket)packet;
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            //rawPacket.Data.ToString();
            MessageBox.Show(tcpPacket.PayloadData.ToString(), "info");
        }
    }
}