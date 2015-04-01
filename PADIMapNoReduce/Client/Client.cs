using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    class Client : MarshalByRefObject, PADIMapNoReduce.IClient
    {
        public int[] processBytes(PADIMapNoReduce.Pair<int, int> byteInterval)
        {
            System.Console.WriteLine("Received bytes from " + byteInterval.First + " to " + byteInterval.Second);
            return null;
        }
    }
}
