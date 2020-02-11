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
            SoupServer<Test> server = new SoupServer<Test>(true);
            server.Run();
            Console.Read();
        }
    }
}
