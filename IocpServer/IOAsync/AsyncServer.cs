/**********************************************************************
 * 文件名称：Listener.cs
 * 文件功能：监听socket连接
 * 文件作者：jyliu
 * 创建时间：2010-3-15 19:00
 * 项目名称：可扩展通信平台
 * 
 * 历史记录：
 * 编号 日期      作者    备注
 * 1.0  2010-3-15 yitian  创建
 * 
 * *********************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace IocpServer
{

    public class AsyncServer
    {
        private Socket listener;
        private bool isStart;
        private static Mutex mutex = new Mutex();
        private object objectForNum = new object();
        private Int32 numConnectedSockets;
        private Int32 numMaxConnections;//最大连接数
        private AsyncSocketPool clientPool;

        public MainForm mainForm;//需要在外面赋值
        /// <summary>
        /// 当前监听是否开启
        /// </summary>
        public bool IsStart
        {
            get
            {
                return isStart;
            }
        }

        private event SocketEventHandler connectRequestEvent;
        /// <summary>
        /// 连接请求处理事件
        /// </summary>
        public event SocketEventHandler ConnectRequestEvent
        {
            add
            {
                connectRequestEvent += value;
            }
            remove
            {
                connectRequestEvent -= value;
            }
        }

        public AsyncServer(Int32 numMaxConnections, Int32 bufferSize)
        {
            this.numMaxConnections = numMaxConnections;
            this.clientPool = new AsyncSocketPool(numMaxConnections);
            isStart = false;
        }
        
        /// <summary>
        /// 启动监听
        /// </summary>
        public void Start(int port)
        {
            mutex.WaitOne();
            isStart = true;

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            listener.Bind(localEndPoint);
            listener.Listen(numMaxConnections);
            listener.BeginAccept(new AsyncCallback(AcceptConnectRequest), listener);
        }

        /// <summary>
        /// 关闭监听
        /// </summary>
        public void Stop()
        {
            mutex.ReleaseMutex();
            isStart = false;
            if (listener != null)
            {
                listener.Close();
            }            
        }

        /// <summary>
        /// 处理收到连接请求
        /// </summary>
        private void AcceptConnectRequest(IAsyncResult ar)
        {
            //有连接时会执行回调函数
            //server执行Close时，停止异步连接，也会执行此回调函数
            if (!isStart)  //isStart==false
            {
                return;
            }

            try
            {
                Socket listener = (Socket)ar.AsyncState;

                Socket client = listener.EndAccept(ar);
                
               
                AsyncSocket socket = new AsyncSocket();
                socket.Attach(client);

                if (clientPool.Add(socket))
                {
                    socket.DisConnectRequestEvent += new SocketEventHandler(AsyncSocket_DisConnectRequestEvent);

                    Interlocked.Increment(ref this.numConnectedSockets);
                    string outStr = String.Format("客户 {0} 连入, 共有 {1} 个连接。", client.RemoteEndPoint.ToString(), this.numConnectedSockets);
                    mainForm.Invoke(mainForm.setlistboxcallback, outStr);
                }
                else
                {
                    //达到最大连接数
                    socket.Disconnect();
                    mainForm.Invoke(mainForm.setlistboxcallback, "达到最大连接数,监听结束");
                    return;
                }
                listener.BeginAccept(new AsyncCallback(AcceptConnectRequest), listener);//死循环等待处理连接请求
            }
            catch (SocketException sex)
            {
                mainForm.Invoke(mainForm.setlistboxcallback, "监听结束" + sex.Message);
            }
            catch (Exception ex)
            {
                mainForm.Invoke(mainForm.setlistboxcallback, ex.Message);
            }
            
        }




        private void AsyncSocket_DisConnectRequestEvent(AsyncSocket e)
        {
            this.clientPool.Del(e);

            Interlocked.Decrement(ref this.numConnectedSockets);
            string outStr = String.Format("客户 {0} 断开, 共有 {1} 个连接。", e.socket.RemoteEndPoint.ToString(), this.numConnectedSockets);
            mainForm.Invoke(mainForm.setlistboxcallback, outStr);

        }

      

       
    }
}
