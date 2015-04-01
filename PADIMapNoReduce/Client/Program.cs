using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Title = "Client";
            UserLevelApp userApplication = new UserLevelApp();
            userApplication.execute();
        }
    }
}
