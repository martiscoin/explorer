namespace Martiscoin.Explorer
{
   using Microsoft.AspNetCore.Hosting;
   using Microsoft.Extensions.Hosting;

   /// <summary>
   /// The application program.
   /// </summary>
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
         .ConfigureAppConfiguration(config =>
         {
            config.AddMartiscoin("Blockore Explorer", args);
         })
         .ConfigureWebHostDefaults(webBuilder =>
         {
            webBuilder.UseStartup<Startup>();
         });
   }
}
