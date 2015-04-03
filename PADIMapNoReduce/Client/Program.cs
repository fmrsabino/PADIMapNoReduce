using System;

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
