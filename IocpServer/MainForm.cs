using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IocpServer 
{
    public partial class MainForm : Form
    {
        public delegate void SetListBoxCallBack(string str);
        public SetListBoxCallBack setlistboxcallback;

        private IocpServer iocp = new IocpServer(6000, 1024);
        //private SyncServer iocp = new SyncServer(2000, 1024);
       // private AsyncServer iocp = new AsyncServer(2000, 1024);

        public MainForm()
        {
            InitializeComponent();
            setlistboxcallback = new SetListBoxCallBack(SetListBox);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            startBtn_Click(sender, e);
        }
        public void SetListBox(string str)
        {
            infoList.Items.Insert(0, str);
            infoList.SelectedIndex = 0;
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            iocp.Start(9900);
            iocp.mainForm = this;
            startBtn.Enabled = false;
            stopBtn.Enabled = true;
            SetListBox("监听开启...");
        }

        private void stopBtn_Click(object sender, EventArgs e)
        {
            iocp.Stop();
            startBtn.Enabled = true;
            stopBtn.Enabled = false;
            SetListBox("监听停止...");
        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            if (stopBtn.Enabled)
                iocp.Stop();
            this.Close();
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            infoList.Items.Clear();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (stopBtn.Enabled)
            {
                e.Cancel=true;
                MessageBox.Show("先关闭监听");
            }
                
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int count = iocp.ioContextPool.boundary;
            MessageBox.Show(count.ToString());
        }



    }
}
