using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IocpServer
{
    public sealed  class SyncServer
    {

        private TcpListener listener;
        private static Mutex mutex = new Mutex();
        private Int32 bufferSize;

        private object objectForNum=new object();
        private Int32 numConnectedSockets;//当前连接数
        private Int32 numMaxConnections;//最大连接数
        private TcpClientPool clientPool;

        public MainForm mainForm;//需要在外面赋值

        public SyncServer(Int32 numMaxConnections, Int32 bufferSize)
        {
            this.numConnectedSockets = 0;
            this.numMaxConnections = numMaxConnections;
            this.bufferSize = bufferSize;

            this.clientPool = new TcpClientPool(numMaxConnections);
        }

        public void Start(Int32 port)
        {
            mutex.WaitOne();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            this.listener = new TcpListener(localEndPoint);
            this.listener.Server.ReceiveBufferSize = this.bufferSize;
            this.listener.Server.SendBufferSize = this.bufferSize;
            this.listener.Start(this.numMaxConnections);

            Thread acceptThread = new Thread(new ThreadStart(processAccept));
            acceptThread.Start();
        }

        private void processAccept()
        {
            try
            {
                while (true)
                {
                    TcpClient client = this.listener.AcceptTcpClient();
                    bool ret = this.clientPool.Add(client);

                    if (ret)
                    {
                        Interlocked.Increment(ref this.numConnectedSockets);

                        string outStr = String.Format("客户 {0} 连入, 共有 {1} 个连接。", client.Client.RemoteEndPoint.ToString(), this.numConnectedSockets);
                        mainForm.Invoke(mainForm.setlistboxcallback, outStr);

                        Thread thread = new Thread(new ParameterizedThreadStart(processReveive));
                        thread.Start(client);
                    }
                    else
                    {
                        mainForm.Invoke(mainForm.setlistboxcallback, "达到最大连接数,监听结束");
                        client.Close();
                        break;
                    }
                }
            }
            catch(SocketException sex)
            {
                mainForm.Invoke(mainForm.setlistboxcallback, "监听结束" + sex.Message); 
            }
            catch(Exception ex)
            {
                mainForm.Invoke(mainForm.setlistboxcallback, ex.Message);
            }
        }

        private void processReveive(object e)
        {
            TcpClient client = e as TcpClient;
            byte[] buffer;

            try
            {
                NetworkStream ns = client.GetStream();

                while (true)
                {
                    buffer = new byte[100];
                    int count = ns.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        string msg = Encoding.Default.GetString(buffer, 0, count);
                        ns.Write(buffer, 0, count);
                    }
                    else//远程主机正常关闭
                    {
                        Interlocked.Decrement(ref this.numConnectedSockets);
                        string outStr = String.Format("客户 {0} 断开, 共有 {1} 个连接。", client.Client.RemoteEndPoint.ToString(), this.numConnectedSockets);
                        mainForm.Invoke(mainForm.setlistboxcallback, outStr);
                        this.clientPool.Del(client);
                        return;
                    }
                }
            }
            catch (IOException iex)
            {
                //远程客户端强制关闭连接
                if (iex.Message.Equals("无法从传输连接中读取数据: 远程主机强迫关闭了一个现有的连接。。"))
                {
                    Interlocked.Decrement(ref this.numConnectedSockets);
                    string outStr = String.Format("客户 {0} 断开, 共有 {1} 个连接。", client.Client.RemoteEndPoint.ToString(), this.numConnectedSockets);
                    mainForm.Invoke(mainForm.setlistboxcallback, outStr);
                    this.clientPool.Del(client);
                    return;
                }
                //本地服务器正常关闭
                if (iex.Message.Equals("无法从传输连接中读取数据: 一个封锁操作被对 WSACancelBlockingCall 的调用中断。。"))
                {

                }
            }
            catch (Exception ex)//未处理的异常
            {
                mainForm.Invoke(mainForm.setlistboxcallback, ex.Message);
            }
        }

        public void Stop()
        {
            this.listener.Stop();
            mutex.ReleaseMutex();
            this.clientPool.Clear();
            
        }

    }
}
