using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace IocpServer
{
    class TcpClientPool
    {
        private List<TcpClient> pool;
        private Int32 capacity;


        public TcpClientPool(Int32 capacity)
        {
            this.pool = new List<TcpClient>(capacity);
            this.capacity = capacity;
        }

        public bool Add(TcpClient arg)
        {
            bool ret = false;
            lock (this.pool)
            {
                if (arg != null && this.pool.Count < capacity)
                {
                    pool.Add(arg);
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }
            return ret;

        }

        public bool Del(TcpClient arg)
        {
            if (arg != null)
            {
                try
                {
                    arg.Close();
                }
                catch (Exception)
                {
                }

                lock (this.pool)
                {
                    this.pool.Remove(arg);
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        public void Clear()
        {
            lock (this.pool)
            {
                for (int i = 0; i < this.pool.Count; i++)
                {
                    pool[i].Close();
                }
                pool.Clear();

            }

        }
    }
}
