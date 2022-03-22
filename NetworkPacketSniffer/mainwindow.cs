using SharpPcap;
using System.Diagnostics;

namespace NetworkPacketSniffer
{
    public partial class mainwindow : Form
    {
        public CaptureDeviceList devices;
        public ICaptureDevice? curr_dev = null;
        private PacketArrivalEventHandler arrivalEventHandler;

        public mainwindow()
        {
            InitializeComponent();
            devices = CaptureDeviceList.Instance;
            foreach (ICaptureDevice dev in devices)
            {
                comboBox1.Items.Add(dev.Description);
            }
            comboBox1.SelectedIndex = 0;
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
            MessageBox.Show(dev.ToString(), "网卡信息");
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

        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            Debug.WriteLine(e.GetPacket());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Stop the capturing process
            if (curr_dev == null)
            {
                MessageBox.Show("尚未捕获", "warning");
                return;
            }
            curr_dev.StopCapture();
            curr_dev.Close();
            curr_dev.OnPacketArrival -= arrivalEventHandler;
            curr_dev = null;
            button3.Enabled = true;
            button4.Enabled = false;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}