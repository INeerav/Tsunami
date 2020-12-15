using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tsunami
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("URI please :");
            string url = Console.ReadLine();
            Console.WriteLine("Token please :");
            string token = Console.ReadLine();
            Console.WriteLine("Is Get?(Yes or No) :");
            string isGetString = Console.ReadLine();
            bool isGet = true;
            StringContent content = new StringContent("");
            if (!string.IsNullOrEmpty(isGetString) && isGetString.ToLower() != "yes") {
                isGet = false;
                Console.WriteLine("Body in json:");
                string body = Console.ReadLine();
                content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            Console.WriteLine("Concurrent Requests : 50 , Interval Time : 5 seconds");
            while (true)
            {
                Call(url, token, isGet, isGet ? null: content);
                Thread.Sleep(5000);
            }
        }

        static void Call(string url,string token,bool isGet = true, StringContent content = null)
        {
            TsunamiService randoService = new TsunamiService(url: url, token, maxConcurrentRequests: 100, isGet, content);

            for (int i = 0; i < 100; i++)
            {
                Task.Run(async () =>
                {
                    Console.WriteLine($"Flooding with reqeusts!! ");
                    Console.WriteLine(await randoService.GetResponse());
                });
            }           
        }
    }
}
