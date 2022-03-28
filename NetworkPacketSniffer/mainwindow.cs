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
            listView1.Columns.Add("�����Э��", 90, HorizontalAlignment.Left);
            listView1.Columns.Add("Ӧ�ò�Э��", 90, HorizontalAlignment.Left);

            listView2.View = View.Details;
            listView2.Columns.Add("���", 50, HorizontalAlignment.Left);
            listView2.Columns.Add("ʱ��", 150, HorizontalAlignment.Left);
            listView2.Columns.Add("Դ��ַ", 140, HorizontalAlignment.Left);
            listView2.Columns.Add("Ŀ���ַ", 140, HorizontalAlignment.Left);
            listView2.Columns.Add("����", 80, HorizontalAlignment.Left);
            listView2.Columns.Add("��·��Э��", 90, HorizontalAlignment.Left);
            listView2.Columns.Add("�����Э��", 90, HorizontalAlignment.Left);
            listView2.Columns.Add("�����Э��", 90, HorizontalAlignment.Left);
            listView2.Columns.Add("Ӧ�ò�Э��", 90, HorizontalAlignment.Left);
            listView2.Visible = false;

            comboBox2.Items.Add("and");
            comboBox2.Items.Add("or");
            comboBox2.SelectedIndex = 0;

            comboBox3.Items.Add("����");
            comboBox3.Items.Add("ip");
            comboBox3.Items.Add("arp");

            comboBox4.Items.Add("����");
            comboBox4.Items.Add("tcp");
            comboBox4.Items.Add("udp");


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

        private string filter = "";

        private void button10_Click(object sender, EventArgs e)
        {
            string f = "";
            if (filter != "")
            {
                if (comboBox2.SelectedIndex == 1)
                    f += " or ";
                else
                    f += " and ";

            }
            if (checkBox1.Checked)
                f += "not ";
            f += textBox1.Text;
            filter += f;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            MessageBox.Show(filter, "info");
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
            curr_dev.Filter = filter;
            // Start the capturing process
            curr_dev.StartCapture();
            Debug.WriteLine("Listening on {0}", curr_dev.Description);

            button3.Enabled = false;
            button4.Enabled = true;
            button10.Enabled = false;
            button12.Enabled = false;
            listView1.Visible = true;
            listView2.Visible = false;

        }

        void device_OnPacketArrival(object sender, PacketCapture e)
        {
            lock (QueueLock)
            {
                rawPacketList.Add(e.GetPacket());
            }
            
        }
        private List<string> proto = new List<string>();
        private void BackgroundThread()
        {
            Debug.WriteLine("�߳̿���");
            Control.CheckForIllegalCrossThreadCalls = false;
            listView1.Items.Clear();
            var count = 0;


            while(true)
            {
                bool shouldSleep = true;
                lock (QueueLock)
                {
                    if (count < rawPacketList.Count)  
                        shouldSleep = false;                   
                }
                if (shouldSleep)
                {
                    if (BackgroundThreadStop == true)
                        break;
                    else
                        System.Threading.Thread.Sleep(50);
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

                    string protocol = judgeProtocol(packet);
                    if (protocol != null && protocol!= "")
                    {
                        if (!proto.Contains(protocol))
                        {
                            proto.Add(protocol);
                            comboBox5.Items.Add(protocol);
                        }
                    }
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
            button10.Enabled = true;
            button12.Enabled = true;

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
                show_text();
                show_detail();
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

            MessageBox.Show(System.Text.Encoding.Default.GetString(data), "info");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            filter = "";
        }

        private void show_detail()
        {
            RawCapture rawPacket = rawPacketList[select_index];
            Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket eth = (EthernetPacket)packet;
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();

            int count = 0;
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add("ԭʼ���ݰ�:" + select_index.ToString());
            treeView1.Nodes[count].Nodes.Add("��·�����ͣ�" + rawPacket.LinkLayerType.ToString());
            treeView1.Nodes[count].Nodes.Add("����ʱ�䣺" + rawPacket.Timeval.Date.ToLocalTime().TimeOfDay);
            treeView1.Nodes[count].Nodes.Add("���ȣ�" + rawPacket.Data.Length.ToString());
            count++;

            treeView1.Nodes.Add("��·��");
            treeView1.Nodes[count].Nodes.Add("ԴMAC��ַ��" + eth.SourceHardwareAddress.ToString());
            treeView1.Nodes[count].Nodes.Add("Ŀ��MAC��ַ��" + eth.DestinationHardwareAddress.ToString());
            treeView1.Nodes[count].Nodes.Add("�ϲ�Э�飺" + eth.Type.ToString());
            count++;

            treeView1.Nodes.Add("�����");
            if (eth.PayloadPacket is PacketDotNet.ArpPacket)
            {
                ArpPacket arppacket = (ArpPacket)eth.PayloadPacket;
                treeView1.Nodes[count].Nodes.Add("Э�����ͣ�ARP");
                treeView1.Nodes[count].Nodes.Add("������" + arppacket.Operation);
                treeView1.Nodes[count].Nodes.Add("Э���ַ���ͣ�" + arppacket.ProtocolAddressType);
                treeView1.Nodes[count].Nodes.Add("Э���ַ���ȣ�" + arppacket.ProtocolAddressLength);
                treeView1.Nodes[count].Nodes.Add("������Э���ַ��" + arppacket.SenderProtocolAddress.ToString());
                treeView1.Nodes[count].Nodes.Add("Ŀ��Э���ַ��" + arppacket.TargetProtocolAddress.ToString());
                count++;
            }

            if (eth.PayloadPacket is IPPacket)
            {
                IPPacket ippacket = (PacketDotNet.IPPacket)eth.PayloadPacket;
                treeView1.Nodes[count].Nodes.Add("Э�����ͣ�" + eth.Type.ToString());
                treeView1.Nodes[count].Nodes.Add("���ݰ����ȣ�" + ippacket.TotalLength);
                treeView1.Nodes[count].Nodes.Add("�汾��" + ippacket.Version);
                treeView1.Nodes[count].Nodes.Add("�ײ����ȣ�" + ippacket.HeaderLength);
                treeView1.Nodes[count].Nodes.Add("Դip��ַ��" + ippacket.SourceAddress.ToString());
                treeView1.Nodes[count].Nodes.Add("Ŀ��ip��ַ��" + ippacket.DestinationAddress.ToString());
                treeView1.Nodes[count].Nodes.Add("����ʱ�䣺" + ippacket.TimeToLive.ToString());
                treeView1.Nodes[count].Nodes.Add("�ϲ�Э�飺" + ippacket.Protocol.ToString());

                if (ippacket is PacketDotNet.IPv4Packet)
                {
                    IPv4Packet ipv4 = (PacketDotNet.IPv4Packet)ippacket;
                    treeView1.Nodes[count].Nodes.Add("IPv4���ݰ�");
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("У���룺" + ipv4.Checksum);
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("У����״̬��" + ipv4.ValidChecksum.ToString());
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("��Ƭ��־��" + ipv4.FragmentFlags);
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("Ƭƫ�ƣ�" + ipv4.FragmentOffset);
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("��ʶ��" + ipv4.Id);
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("TOS�������ͣ�" + ipv4.TypeOfService);
                }
                if (ippacket is PacketDotNet.IPv6Packet)
                {
                    IPv6Packet ipv6 = (PacketDotNet.IPv6Packet)ippacket;
                    treeView1.Nodes[count].Nodes.Add("IPv6���ݰ���");
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("�������ͣ�" + ipv6.TrafficClass);
                    treeView1.Nodes[count].Nodes[8].Nodes.Add("����ǩ��" + ipv6.FlowLabel);
                }
                count++;
            }

            treeView1.Nodes.Add("�����");
            if (tcpPacket != null)
            {
                treeView1.Nodes[count].Nodes.Add("Э�����ͣ�Tcp");
                treeView1.Nodes[count].Nodes.Add("Դ�˿ڣ�" + tcpPacket.SourcePort);
                treeView1.Nodes[count].Nodes.Add("Ŀ�Ķ˿ڣ�" + tcpPacket.DestinationPort);
                treeView1.Nodes[count].Nodes.Add("���ȣ�" + tcpPacket.TotalPacketLength);
                treeView1.Nodes[count].Nodes.Add("���кţ�" + tcpPacket.SequenceNumber);
                treeView1.Nodes[count].Nodes.Add("ACK���к�" + tcpPacket.AcknowledgmentNumber);
                treeView1.Nodes[count].Nodes.Add("flag��" + tcpPacket.Flags.ToString("x"));
                treeView1.Nodes[count].Nodes.Add("ƫ������" + tcpPacket.DataOffset);
                int temp = treeView1.Nodes[count].Nodes.Count - 1;
                var flags = Convert.ToString(tcpPacket.Flags, 2).PadLeft(8, '0');
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[0] + "] congestion window reduced");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[1] + "] Ecn - echo");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[2] + "] urgent");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[3] + "] acknowledgement");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[4] + "] push");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[5] + "] reset");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[6] + "] syn");
                treeView1.Nodes[count].Nodes[temp].Nodes.Add("[" + flags[7] + "] fin");

            }

            if (udpPacket != null)
            {
                treeView1.Nodes[count].Nodes.Add("Э�����ͣ�Udp");
                treeView1.Nodes[count].Nodes.Add("Դ�˿ڣ�" + udpPacket.SourcePort);
                treeView1.Nodes[count].Nodes.Add("Ŀ�Ķ˿ڣ�" + udpPacket.DestinationPort);
                treeView1.Nodes[count].Nodes.Add("���ȣ�" + udpPacket.Length);
            }
            treeView1.EndUpdate();
        }

        private List<RawCapture> filterRawPacket = new List<RawCapture>();
        private void update_filter()
        {
            string protocol_selected_1 = "";
            string protocol_selected_2 = "";
            string protocol_selected_3 = "";
            if(comboBox3.SelectedIndex > 0)
            {
                if (comboBox3.SelectedIndex == 1)
                    protocol_selected_1 = "ip";
                if (comboBox3.SelectedIndex == 2)
                    protocol_selected_1 = "arp";
            }
            if (comboBox4.SelectedIndex > 0)
            {
                if (comboBox4.SelectedIndex == 1)
                    protocol_selected_2 = "Tcp";
                if (comboBox4.SelectedIndex == 2)
                    protocol_selected_2 = "Udp";
            }
            if (comboBox5.SelectedIndex > 0)
            {
                protocol_selected_3 = proto[comboBox5.SelectedIndex-1];
            }
            string SourceAddr = textBox2.Text;
            string DestinationAddr = textBox3.Text;
            int SourcePort = -1;
            int DestinationPort = -1;
            try
            {
                if (textBox4.Text != "")
                    SourcePort = int.Parse(textBox4.Text);
                if (textBox5.Text != "")
                    DestinationPort = int.Parse(textBox5.Text);
            }
            catch
            {
                MessageBox.Show("������ȷ�Ķ˿ں�","error");
                return;
            }

            filterRawPacket = new List<RawCapture>();
            int count;
            lock (QueueLock)
            {
                count = rawPacketList.Count;
            }
            for (int i = 0; i < count; i++)
            {
                RawCapture rawPacket = rawPacketList[i];
                Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                EthernetPacket eth = (EthernetPacket)packet;
                var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
                var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();

                if (protocol_selected_1 != "")
                {
                    if (protocol_selected_1 == "ip" && eth.PayloadPacket is not IPPacket)
                        continue;
                    if (protocol_selected_1 == "arp" && eth.PayloadPacket is not ArpPacket)
                        continue;
                }
                if(protocol_selected_2 != "")
                {
                    if (eth.PayloadPacket is IPPacket)
                    {
                        IPPacket ippacket = (IPPacket)eth.PayloadPacket;
                        if (protocol_selected_2 != ippacket.Protocol.ToString())
                            continue;
                    }
                }
                if(protocol_selected_3 != "")
                {
                    if (protocol_selected_3 != judgeProtocol(packet))
                        continue;
                }
                if(SourceAddr != "")
                {
                    string S;
                    if (eth.PayloadPacket is IPPacket)
                        S = ((IPPacket)eth.PayloadPacket).SourceAddress.ToString();
                    else if (eth.PayloadPacket is ArpPacket)
                        S = ((ArpPacket)eth.PayloadPacket).SenderProtocolAddress.ToString();
                    else
                        continue;
                    if (SourceAddr != S)
                        continue;
                }
                if (DestinationAddr != "")
                {
                    string D;
                    if (eth.PayloadPacket is IPPacket)
                        D = ((IPPacket)eth.PayloadPacket).DestinationAddress.ToString();
                    else if (eth.PayloadPacket is ArpPacket)
                        D = ((ArpPacket)eth.PayloadPacket).TargetProtocolAddress.ToString();
                    else
                        continue;
                    if (DestinationAddr != D)
                        continue;
                }
                if(SourcePort != -1)
                {
                    int P;
                    if (tcpPacket != null)
                        P = tcpPacket.SourcePort;
                    else if (udpPacket != null)
                        P = udpPacket.SourcePort;
                    else
                        continue;
                    if (SourcePort != P)
                        continue;
                }
                if (DestinationPort != -1)
                {
                    int P;
                    if (tcpPacket != null)
                        P = tcpPacket.DestinationPort;
                    else if (udpPacket != null)
                        P = udpPacket.DestinationPort;
                    else
                        continue;
                    if (DestinationPort != P)
                        continue;
                }
                filterRawPacket.Add(rawPacket);

                ListViewItem item = new ListViewItem();
                item.SubItems[0].Text = i.ToString();
                item.SubItems.Add(rawPacket.Timeval.Date.ToLocalTime().TimeOfDay.ToString());
                if (eth.HasPayloadPacket)
                {
                    //��ʾip��ַ
                    if (eth.PayloadPacket is IPPacket)
                    {
                        IPPacket ippacket = (IPPacket)eth.PayloadPacket;
                        item.SubItems.Add(ippacket.SourceAddress.ToString());
                        item.SubItems.Add(ippacket.DestinationAddress.ToString());
                    }
                    else if (eth.PayloadPacket is ArpPacket)
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
                listView2.Items.Add(item);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //ȷ��ɸѡ
            listView2.Items.Clear();
            update_filter();
            listView1.Visible = false;
            listView2.Visible = true;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            //ȡ��ɸѡ
            listView1.Visible = true;
            listView2.Visible = false;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("������listview2 change����");
            int count = listView2.SelectedItems.Count;
            if (count > 0)
            {
                select_index = int.Parse(listView2.SelectedItems[0].SubItems[0].Text);
                show_text();
                show_detail();
            }
        }

        private void show_text()
        {
            RawCapture rawPacket = rawPacketList[select_index];
            string[] packetDetailString = new string[rawPacket.Data.Length / 16 + 1];

            for (int i = 0; i <= (rawPacket.Data.Length - 1) / 16; i++)
            {
                packetDetailString[i] = i.ToString("X4");
                int j;
                for (j = 0; j < 16; j++)
                {
                    if (j % 8 == 0)
                        packetDetailString[i] += "  ";
                    if (i * 16 + j < rawPacket.Data.Length)
                        packetDetailString[i] += rawPacket.Data[i * 16 + j].ToString("X2") + " ";
                    else
                        packetDetailString[i] += "     ";
                }
                packetDetailString[i] += "      ";
                for (j = 0; j < 16; j++)
                {
                    if (i * 16 + j < rawPacket.Data.Length)
                    {
                        if (rawPacket.Data[i * 16 + j] < 32 || rawPacket.Data[i * 16 + j] > 126)
                            packetDetailString[i] += ".";
                        else
                            packetDetailString[i] += Convert.ToChar(rawPacket.Data[i * 16 + j]);
                    }
                    else
                        packetDetailString[i] += " ";
                }
            }
            richTextBox1.Text = "";
            for (int i = 0; i <= rawPacket.Data.Length / 16; i++)
            {
                richTextBox1.Text += packetDetailString[i] + "\n";
            }
        }

        

        private void button15_Click(object sender, EventArgs e)
        {
            //����

        }
    }
}