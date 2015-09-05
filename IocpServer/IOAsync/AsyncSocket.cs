/**********************************************************************
 * 文件名称：AsyncSocket.cs
 * 文件功能：Socket网络通信的通道功能实现，
 *           连接远程终端、发送消息包、接收消息包、断开远程终端连接。
 * 文件作者：jyliu
 * 创建时间：2010-3-22 15:00
 * 项目名称：可扩展通信平台
 * 
 * 历史记录：
 * 编号 日期      作者    备注
 * 1.0  2010-3-22 yitian  创建
 * 
 * *********************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace IocpServer
{
    //接收断开socket处理委托
    public delegate void SocketEventHandler(AsyncSocket e);

    /// <summary>
    /// 接收消息处理委托
    /// </summary>
    /// <param name="e"></param>
    public delegate void ReceivedEventHandler(byte[] message); 

    public class AsyncSocket
    {
        private byte[] receivedBuffer ;
        // 发送缓冲区，每次发送，实际上只是加到这个流里，
        // 然后异步一次性把流里所有数据发送。
        private MemoryStream sendBuffer;
        public Socket socket;

        private bool isConnect;
        public bool IsConnect
        {
            get
            {
                return isConnect;
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

        private event SocketEventHandler disConnectRequestEvent;
        /// <summary>
        /// 连接请求处理事件
        /// </summary>
        public event SocketEventHandler DisConnectRequestEvent
        {
            add
            {
                disConnectRequestEvent += value;
            }
            remove
            {
                disConnectRequestEvent -= value;
            }
        }

        private event ReceivedEventHandler receivedRequestEvent;
        /// <summary>
        /// 接收数据包事件处理
        /// </summary>
        public event ReceivedEventHandler ReceivedRequestEvent
        {
            add
            {
                receivedRequestEvent += value;
            }
            remove
            {
                receivedRequestEvent -= value;
            }
        }

        private IPEndPoint remoteEndPoint;
        /// <summary>
        /// 通道信息
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get 
            {
                return remoteEndPoint;
            }
        }

        public AsyncSocket()
        {
            isConnect = false;
            socket = null;
            receivedBuffer = new byte[1024]; //一次最多接受1024字节
            sendBuffer = new MemoryStream();
        }
        
        /// <summary>
        /// 通道绑定到socket上
        /// </summary>
        /// <param name="socket"></param>
        public void Attach(Socket socket)
        {
            this.socket = socket;
            remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.socket.BeginReceive(receivedBuffer
                                    , 0
                                    , receivedBuffer.Length
                                    , SocketFlags.None
                                    , new AsyncCallback(OnReceiveMsg)
                                    , socket);
            this.isConnect = true;
        }

        /// <summary>
        /// 连接通道-异步连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip, int port)
        {
            try
            {
                IPHostEntry he = Dns.GetHostEntry(ip);
                if (he.AddressList != null)
                {
                    if (he.AddressList.Length > 0)
                    {
                        remoteEndPoint = new IPEndPoint(he.AddressList[0], port);
                        Connect(remoteEndPoint);
                        return;
                    }
                }
            }
            catch (SocketException e)
            {
                Disconnect();
                return;
            }

            Disconnect();
            return;
        }

        /// <summary>
        /// 连接通道-异步连接
        /// </summary>
        /// <param name="endPoint"></param>
        public void Connect(EndPoint endPoint)
        {
            remoteEndPoint = (IPEndPoint)endPoint;

            if (socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork
                                    , SocketType.Stream
                                    , ProtocolType.Tcp);
            }
            socket.BeginConnect(endPoint
                                , new AsyncCallback(OnConnected)
                                , socket);
           
        }

        /// <summary>
        /// 正在连接请求
        /// </summary>
        /// <param name="ar"></param>
        private void OnConnected(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState; //有必要传递吗，引用传递
            try
            {
                client.EndConnect(ar);
            }
            catch
            {
                Disconnect(); // 断开连接
                return;
            }
            this.isConnect = true;

            if (connectRequestEvent != null)
            {
                connectRequestEvent(this); // 回调连接处理方法
            }

            // 开始接收数据
            client.BeginReceive(receivedBuffer
                            , 0
                            , receivedBuffer.Length
                            , SocketFlags.None
                            , new AsyncCallback(OnReceiveMsg)
                            , client);
            return;
        }

        /// <summary>
        /// 发送消息包
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(byte[] buffer, int offset, int count)
        {
            lock (sendBuffer)
            {
                sendBuffer.Write(buffer, offset, count);
            }
            AsyncSend();
        }

        /// <summary>
        /// 发送数据(异步)
        /// </summary>
        private void AsyncSend()
        {
            try
            {
                lock (sendBuffer)
                {
                    if (sendBuffer.Length > 0)
                    {
                        byte[] buffer = sendBuffer.ToArray();
                        sendBuffer.SetLength(0);
                        socket.BeginSend(buffer
                                        , 0
                                        , buffer.Length
                                        , SocketFlags.None
                                        , new AsyncCallback(OnSend)
                                        , socket);
                    }
                }
            }
            catch
            {
                Disconnect();
                return;
            }

        }

        /// <summary>
        /// 正在发送数据处理
        /// </summary>
        /// <param name="ar"></param>
        private void OnSend(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            try
            {
                client.EndSend(ar);
            }
            catch 
            {
                Disconnect();
                return;
            }
            AsyncSend();
        }

        /// <summary>
        /// 正在接收数据包
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceiveMsg(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            int count = 0;
            try
            {
                count = client.EndReceive(ar);
            }
            catch  //客户端关闭，导致异常退出,执行Disconnect,触发disConnectRequestEvent事件
            {
                Disconnect();
                return;
            }

            // 正常退出
            if (count == 0)
            {
                Disconnect();
                return;
            }

            if (receivedRequestEvent != null)  //接受到的数据长度不为0，触发receivedRequestEvent事件
            {
                Array.Resize<byte>(ref receivedBuffer, count);        //去掉缓冲区多余的空字符
                receivedRequestEvent(receivedBuffer);               //触发receivedRequestEvent事件
                Array.Resize<byte>(ref receivedBuffer, 1024);         //恢复缓冲区初始大小
                Array.Clear(receivedBuffer, 0, receivedBuffer.Length);//清空缓冲区，全部赋值空字符
            }



            try
            {
                // 继续接收数据
                client.BeginReceive(receivedBuffer
                                    , 0
                                    , receivedBuffer.Length
                                    , SocketFlags.None
                                    , new AsyncCallback(OnReceiveMsg)
                                    , client);
            }
            catch
            {
                Disconnect();
                return;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (socket != null)
            {
                if (disConnectRequestEvent != null)
                {
                    disConnectRequestEvent(this);
                }
                
                if (isConnect)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                socket.Close();
                socket = null;
                isConnect = false;
                
            }
        }
    }
}
