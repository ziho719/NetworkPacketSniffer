using SharpPcap;
using System.Diagnostics;

namespace NetworkPacketSniffer
{
    public partial class mainwindow : Form
    {
        public CaptureDeviceList devices;

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
            Debug.WriteLine("查看网卡");
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
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("显示网卡信息");

            MessageBox.Show(dev.ToString(), "网卡信息");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //开始捕获

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //停止捕获

        }
    }
}