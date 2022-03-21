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
            ICaptureDevice dev = devices[index];
            Debug.WriteLine("��ʾ������Ϣ");

            MessageBox.Show(dev.ToString(), "������Ϣ");

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //��ʼ����

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //ֹͣ����

        }
    }
}