using Soup;
using SoupTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            SoupServer<TestServer> server = new SoupServer<TestServer>(true, hostUrl: "http://*:8090/");
            server.Run();
            Console.Read();
        }
    }
}
