/**********************************************************************
 * �ļ����ƣ�Listener.cs
 * �ļ����ܣ�����socket����
 * �ļ����ߣ�jyliu
 * ����ʱ�䣺2010-3-15 19:00
 * ��Ŀ���ƣ�����չͨ��ƽ̨
 * 
 * ��ʷ��¼��
 * ��� ����      ����    ��ע
 * 1.0  2010-3-15 yitian  ����
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
        private Int32 numMaxConnections;//���������
        private AsyncSocketPool clientPool;

        public MainForm mainForm;//��Ҫ�����渳ֵ
        /// <summary>
        /// ��ǰ�����Ƿ���
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

        public AsyncServer(Int32 numMaxConnections, Int32 bufferSize)
        {
            this.numMaxConnections = numMaxConnections;
            this.clientPool = new AsyncSocketPool(numMaxConnections);
            isStart = false;
        }
        
        /// <summary>
        /// ��������
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
        /// �رռ���
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
        /// �����յ���������
        /// </summary>
        private void AcceptConnectRequest(IAsyncResult ar)
        {
            //������ʱ��ִ�лص�����
            //serverִ��Closeʱ��ֹͣ�첽���ӣ�Ҳ��ִ�д˻ص�����
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
                    string outStr = String.Format("�ͻ� {0} ����, ���� {1} �����ӡ�", client.RemoteEndPoint.ToString(), this.numConnectedSockets);
                    mainForm.Invoke(mainForm.setlistboxcallback, outStr);
                }
                else
                {
                    //�ﵽ���������
                    socket.Disconnect();
                    mainForm.Invoke(mainForm.setlistboxcallback, "�ﵽ���������,��������");
                    return;
                }
                listener.BeginAccept(new AsyncCallback(AcceptConnectRequest), listener);//��ѭ���ȴ�������������
            }
            catch (SocketException sex)
            {
                mainForm.Invoke(mainForm.setlistboxcallback, "��������" + sex.Message);
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
            string outStr = String.Format("�ͻ� {0} �Ͽ�, ���� {1} �����ӡ�", e.socket.RemoteEndPoint.ToString(), this.numConnectedSockets);
            mainForm.Invoke(mainForm.setlistboxcallback, outStr);

        }

      

       
    }
}
