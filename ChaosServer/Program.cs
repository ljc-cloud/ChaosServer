using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ChaosServer {
    internal class Program {

        static void Main(string[] args)
        {
            ServerInterface.Start();
    
            Console.ReadLine();
        }
        
    }

    class Test
    {
        public int Test1()
        {
            Console.WriteLine("Test1");
            return 1;
        }

        public void Test2()
        {
            Console.WriteLine("Test2");
        }
    }
}
