using Elysium.Service.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Playground
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Blad().Wait();
        }

        public static async Task Blad()
        {
            var c = new ServiceClient(new Uri("http://agrospr-dev.azurewebsites.net"));
            //c.Connection.Credentials = new Elysium.Credentials()
            var q = await c.InvokeApiAsync<JToken>("/api/City");
        }
    }
}