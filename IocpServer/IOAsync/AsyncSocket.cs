/**********************************************************************
 * �ļ����ƣ�AsyncSocket.cs
 * �ļ����ܣ�Socket����ͨ�ŵ�ͨ������ʵ�֣�
 *           ����Զ���նˡ�������Ϣ����������Ϣ�����Ͽ�Զ���ն����ӡ�
 * �ļ����ߣ�jyliu
 * ����ʱ�䣺2010-3-22 15:00
 * ��Ŀ���ƣ�����չͨ��ƽ̨
 * 
 * ��ʷ��¼��
 * ��� ����      ����    ��ע
 * 1.0  2010-3-22 yitian  ����
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
    //���նϿ�socket����ί��
    public delegate void SocketEventHandler(AsyncSocket e);

    /// <summary>
    /// ������Ϣ����ί��
    /// </summary>
    /// <param name="e"></param>
    public delegate void ReceivedEventHandler(byte[] message); 

    public class AsyncSocket
    {
        private byte[] receivedBuffer ;
        // ���ͻ�������ÿ�η��ͣ�ʵ����ֻ�Ǽӵ�������
        // Ȼ���첽һ���԰������������ݷ��͡�
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
        /// �����������¼�
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
        /// �����������¼�
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
        /// �������ݰ��¼�����
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
        /// ͨ����Ϣ
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
            receivedBuffer = new byte[1024]; //һ��������1024�ֽ�
            sendBuffer = new MemoryStream();
        }
        
        /// <summary>
        /// ͨ���󶨵�socket��
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
        /// ����ͨ��-�첽����
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
        /// ����ͨ��-�첽����
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
        /// ������������
        /// </summary>
        /// <param name="ar"></param>
        private void OnConnected(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState; //�б�Ҫ���������ô���
            try
            {
                client.EndConnect(ar);
            }
            catch
            {
                Disconnect(); // �Ͽ�����
                return;
            }
            this.isConnect = true;

            if (connectRequestEvent != null)
            {
                connectRequestEvent(this); // �ص����Ӵ�����
            }

            // ��ʼ��������
            client.BeginReceive(receivedBuffer
                            , 0
                            , receivedBuffer.Length
                            , SocketFlags.None
                            , new AsyncCallback(OnReceiveMsg)
                            , client);
            return;
        }

        /// <summary>
        /// ������Ϣ��
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
        /// ��������(�첽)
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
        /// ���ڷ������ݴ���
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
        /// ���ڽ������ݰ�
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
            catch  //�ͻ��˹رգ������쳣�˳�,ִ��Disconnect,����disConnectRequestEvent�¼�
            {
                Disconnect();
                return;
            }

            // �����˳�
            if (count == 0)
            {
                Disconnect();
                return;
            }

            if (receivedRequestEvent != null)  //���ܵ������ݳ��Ȳ�Ϊ0������receivedRequestEvent�¼�
            {
                Array.Resize<byte>(ref receivedBuffer, count);        //ȥ������������Ŀ��ַ�
                receivedRequestEvent(receivedBuffer);               //����receivedRequestEvent�¼�
                Array.Resize<byte>(ref receivedBuffer, 1024);         //�ָ���������ʼ��С
                Array.Clear(receivedBuffer, 0, receivedBuffer.Length);//��ջ�������ȫ����ֵ���ַ�
            }



            try
            {
                // ������������
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
        /// �Ͽ�����
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
