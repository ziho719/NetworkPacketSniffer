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
            listView1.Columns.Add("���", 80, HorizontalAlignment.Left);
            listView1.Columns.Add("ʱ��", 80, HorizontalAlignment.Left);
            listView1.Columns.Add("ԴMAC", 160, HorizontalAlignment.Left);
            listView1.Columns.Add("Ŀ��MAC", 160, HorizontalAlignment.Left);
            listView1.Columns.Add("����", 80, HorizontalAlignment.Left);
            listView1.Columns.Add("�����Э��", 80, HorizontalAlignment.Left);
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
            MessageBox.Show(dev.ToString(), "������Ϣ");
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
                    System.Threading.Thread.Sleep(250);
                }
                else
                {
                    var rawPacket = rawPacketList[count];
                    Packet packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                    ListViewItem item = new ListViewItem();
                    item.SubItems[0].Text = count.ToString();
                    item.SubItems.Add(rawPacket.Timeval.Date.ToLocalTime().ToString());
                    var eth = (EthernetPacket) packet;
                    item.SubItems.Add(eth.SourceHardwareAddress.ToString());
                    item.SubItems.Add(eth.DestinationHardwareAddress.ToString());
                    item.SubItems.Add(rawPacket.Data.Length.ToString());
                    item.SubItems.Add(eth.Type.ToString());


                    listView1.Items.Add(item);
                    count++;



                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (curr_dev == null)
            {
                MessageBox.Show("��δ����", "warning");
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

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
/*            var rawPacket = e.GetPacket();
            var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            Debug.WriteLine(packet);
            RawCapture rawPacket;
            lock (QueueLock)
            {
                rawPacket = 
            }*/
        }
    }
}