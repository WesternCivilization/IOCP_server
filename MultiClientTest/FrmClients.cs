using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MultiClientTest
{
    public partial class FrmClients : Form
    {
        public delegate void UpdateUIHandle(string msg);

        private Thread[] workers;
        private BackgroundWorker[] bgWorkers;
        private Socket[] clients;
        private UpdateUIHandle updateUIHandle;
        private bool isExist;
        private IPEndPoint remoteEP;

        public FrmClients()
        {
            InitializeComponent();
            updateUIHandle = new UpdateUIHandle(methordUpdateUI);
            isExist = false;
        }

        private void FrmClients_Load(object sender, EventArgs e)
        {
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            int counts = int.Parse(txtCounts.Text);
            remoteEP = new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            workers = new Thread[counts];
           
            clients = new Socket[counts];
            new Thread(new ThreadStart(startThreads)).Start();
        }

        private void startThreads()
        {
            for (int i = 0; i < workers.Length; i++)
            {
                clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                workers[i] = new Thread(new ParameterizedThreadStart(FrmClients_Connect));
                workers[i].IsBackground = true;
            }

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Start(i);
            }
        }


        void FrmClients_Connect(object index)
        {
            Socket client = clients[int.Parse(index.ToString())];
            try
            {
                client.Connect(remoteEP);
                string msg = string.Format("*************Connected************** {0}", index);
                this.Invoke(updateUIHandle, msg);


                
            }
            catch (Exception ex)
            {
                this.Invoke(updateUIHandle, ex.Message);
            }
            finally
            {
                //if (client.Connected)
                //    client.Close();
            }
        }


        private void methordUpdateUI(string msg)
        {
            if (!isExist)
            {
                richTextBox1.AppendText(msg + "\n");
            }
        }




        private void btnClear_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }

        private void btnStartAsync_Click(object sender, EventArgs e)
        {

            int counts = int.Parse(txtCounts.Text);
            remoteEP = new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            bgWorkers = new BackgroundWorker[counts];
            clients = new Socket[counts];

            for (int i = 0; i < counts; i++)
            {
                bgWorkers[i] = new BackgroundWorker();
                bgWorkers[i].DoWork += new DoWorkEventHandler(bgWorkers_onWork);
                clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            for (int i = 0; i < counts; i++)
            {
                bgWorkers[i].RunWorkerAsync((object)i);
            }
        }

        private void bgWorkers_onWork(object sender, DoWorkEventArgs e)
        {
            int index = int.Parse(e.Argument.ToString());
            try
            {
                clients[index].Connect(remoteEP);
                string msg = "*************Connected Async**************";
                this.Invoke(updateUIHandle, msg);

                //while (true)
                //{
                //    clients[index].Send(Encoding.Default.GetBytes("1"));
                //    this.Invoke(updateUIHandle, "send string 1");
                //    Thread.Sleep(100);
                //}
            }
            catch (Exception ex)
            {
                this.Invoke(updateUIHandle, ex.Message);
            }

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Application.DoEvents();

            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i] != null && clients[i].Connected == true)
                {
                    clients[i].Shutdown(SocketShutdown.Both);
                    clients[i].Close();
                    string msg = string.Format("*************DisConnected************** {0}", i);
                    this.Invoke(updateUIHandle, msg);
                }
            }
        }


        private void FrmClients_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
