using SharpPcap;
using System.Diagnostics;

namespace NetworkPacketSniffer
{
    public partial class mainwindow : Form
    {
        public CaptureDeviceList devices;
        public ICaptureDevice? curr_dev = null;

        public mainwindow()
        {
            InitializeComponent();
            devices = CaptureDeviceList.Instance;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            // Retrieve the device list
            devices = CaptureDeviceList.Instance;
            foreach (ICaptureDevice dev in devices)
            {
                comboBox1.Items.Add(dev.Description);
            }
            comboBox1.SelectedIndex = 0;

            // Print out the available network devices
            Debug.WriteLine("�鿴����");
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

            //https://www.codeproject.com/Articles/12458/SharpPcap-A-Packet-Capture-Framework-for-NET
            // Register our handler function to the 'packet arrival' event
            curr_dev = devices[i];
            curr_dev.OnPacketArrival +=
                new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
            curr_dev.Open(DeviceMode.Promiscuous);

            // Start the capturing process
            curr_dev.StartCapture();
            Debug.WriteLine("Listening on {0}", curr_dev.Description);

            button3.Enabled = false;
            button4.Enabled = true;

        }

        private static void device_OnPacketArrival(object sender, CaptureEventArgs packet)
        {
            DateTime time = packet.Packet.Timeval.Date;
            int len = packet.Packet.Data.Length;
            Debug.WriteLine("{0}:{1}:{2},{3} Len={4}",
                time.Hour, time.Minute, time.Second, time.Millisecond, len);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Stop the capturing process
            if (curr_dev == null)
            {
                MessageBox.Show("��δ����", "warning");
                return;
            }
            curr_dev.StopCapture();
            curr_dev.Close();
            curr_dev = null;
            button3.Enabled = true;
            button4.Enabled = false;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}