using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace IocpServer
{
    class AsyncSocketPool
    {
        List<AsyncSocket> pool;
        Int32 capacity;


        public AsyncSocketPool(Int32 capacity)
        {
            this.pool = new List<AsyncSocket>(capacity);
            this.capacity = capacity;

        }

        public bool Add(AsyncSocket arg)
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

        public bool Del(AsyncSocket arg)
        {
            bool ret = false;
            if (arg != null)
            {
                lock (this.pool)
                {
                    pool.Remove(arg);
                    ret = true;
                }
            }
            else
            {
                ret = false;
            }
            return ret;
        }
    }
}
