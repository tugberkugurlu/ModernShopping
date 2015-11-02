using Microsoft.Owin.Hosting;

namespace ModernShopping.Auth.Host.Console
{
    using Console = System.Console;

    internal class Program
    {
        private static void Main(string[] args)
        {
            const string url = "https://localhost:44333/core";
            Console.Title = "IdentityServer3 SelfHost";
            
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("\n\nServer listening at {0}. Press enter to stop", url);
                Console.ReadLine();
            }
        }
    }
}