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
            listView1.Columns.Add("���", 50, HorizontalAlignment.Left);
            listView1.Columns.Add("ʱ��", 150, HorizontalAlignment.Left);
            listView1.Columns.Add("Դ��ַ", 140, HorizontalAlignment.Left);
            listView1.Columns.Add("Ŀ���ַ", 140, HorizontalAlignment.Left);
            listView1.Columns.Add("����", 80, HorizontalAlignment.Left);
            listView1.Columns.Add("��·��Э��", 90, HorizontalAlignment.Left);
            listView1.Columns.Add("�����Э��", 90, HorizontalAlignment.Left);
            listView1.Columns.Add("�Ự��Э��", 90, HorizontalAlignment.Left);
            listView1.Columns.Add("Ӧ�ò�Э��", 90, HorizontalAlignment.Left);
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("ѡ������:",dev.Description);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("����ѡ������", "warning");
                return;
            }
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("��ʾ������Ϣ");
            MessageBox.Show(dev.ToString(), "info");
        }


        private void button3_Click(object sender, EventArgs e)
        {
            //��ʼ����
            var i = comboBox1.SelectedIndex;
            if (i < 0)
            {
                MessageBox.Show("����ѡ������", "warning");
                return;
            }

            if(curr_dev != null)
            {
                MessageBox.Show("���ڲ�����", "warning");
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
            Debug.WriteLine("�߳̿���");
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
                    if (eth.HasPayloadPacket)
                    {
                        //��ʾip��ַ
                        if (eth.PayloadPacket is IPPacket)
                        {
                            IPPacket ippacket = (IPPacket)eth.PayloadPacket;
                            item.SubItems.Add(ippacket.SourceAddress.ToString());
                            item.SubItems.Add(ippacket.DestinationAddress.ToString());
                        }
                        else if(eth.PayloadPacket is ArpPacket)
                        {
                            ArpPacket arppacket = (ArpPacket)eth.PayloadPacket;
                            item.SubItems.Add(arppacket.SenderProtocolAddress.ToString());
                            item.SubItems.Add(arppacket.TargetProtocolAddress.ToString());
                        }
                    }
                    else //����mac��ַ
                    {
                        item.SubItems.Add(eth.SourceHardwareAddress.ToString());
                        item.SubItems.Add(eth.DestinationHardwareAddress.ToString());

                    }

                    item.SubItems.Add(rawPacket.Data.Length.ToString());
                    item.SubItems.Add(rawPacket.LinkLayerType.ToString());
                    item.SubItems.Add(eth.Type.ToString());
                    if (eth.PayloadPacket is IPPacket)
                    {
                        IPPacket ippacket = (IPPacket)eth.PayloadPacket;
                        item.SubItems.Add(ippacket.Protocol.ToString());
                    }

                    
                    item.SubItems.Add(judgeProtocol(packet)); //�ж�Ӧ�ò�Э��


                    listView1.Items.Add(item);
                    count++;



                }
            }
            Debug.WriteLine("�߳̽���");
        }

        private bool judgePort(ushort sourcePort, ushort destinationPort, ushort port)
        {
            return sourcePort == port || destinationPort == port;
        }

        private string judgeProtocol(Packet packet)
        {
            String protocol = "";
            TcpPacket tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcpPacket != null)
            {
                var D = tcpPacket.DestinationPort;
                var S = tcpPacket.SourcePort;
                if (judgePort(S, D, 80))
                    protocol = "HTTP";
                else if (judgePort(S, D, 443))
                    protocol = "HTTPS";
                else if (judgePort(S, D, 21))
                    protocol = "FTP����";
                else if (judgePort(S, D, 20))
                    protocol = "FTP����";
                else if (judgePort(S, D, 110))
                    protocol = "POP3";
                else if (judgePort(S, D, 143))
                    protocol = "IMAP";
                else if (judgePort(S, D, 25))
                    protocol = "SMTP";
                else if (judgePort(S, D, 23))
                    protocol = "Telnet";
                else if (judgePort(S, D, 53))
                    protocol = "DNS";
            }

            UdpPacket udpPacket = packet.Extract<PacketDotNet.UdpPacket>();
            if (udpPacket != null)
            {
                var D = udpPacket.DestinationPort;
                var S = udpPacket.SourcePort;
                if (judgePort(S, D, 53))
                    protocol = "DNS";
                else if (judgePort(S, D, 67))
                    protocol = "DHCP";
                else if (judgePort(S, D, 161))
                    protocol = "SNMP";
                else if (judgePort(S, D, 69))
                    protocol = "TFTP";
                else if (judgePort(S, D, 1701))
                    protocol = "L2TP";
            }
            return protocol;
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
            Debug.WriteLine("������listview change����");
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
            //��·����Ϣ
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
                MessageBox.Show("������������Э����" + ethpacket.PayloadPacket.GetType().ToString(), "info");
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
                MessageBox.Show("������������Э����" + ethpacket.PayloadPacket.GetType().ToString(), "info");
            

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
                MessageBox.Show("����tcp������" + ippacket.Protocol.ToString(), "info");
            }
            else
                MessageBox.Show("������������Э����" + ethpacket.PayloadPacket.GetType().ToString() +"��������ip", "info");

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
                MessageBox.Show("����udp������" + ippacket.Protocol.ToString(), "info");
            }
            else
                MessageBox.Show("������������Э����" + ethpacket.PayloadPacket.GetType().ToString() + "��������ip", "info");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            RawCapture rawPacket = rawPacketList[select_index];

            byte[] data = rawPacket.Data;
            string text = "";
            for (int i = 0; i < data.Length; i++)
            {
                text += data[i].ToString("X2");
            }
            MessageBox.Show(text, "info");
        }
    }
}