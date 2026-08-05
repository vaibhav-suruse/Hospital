//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using System;

//namespace WebApplicationSampleTest2
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Error()
//                .WriteTo.Console()
//                .WriteTo.File(
//                    "Logs/log-.txt",
//                    rollingInterval: RollingInterval.Day
//                )
//                .CreateLogger();

//            CreateHostBuilder(args).Build().Run();
//        }

//        public static IHostBuilder CreateHostBuilder(string[] args) =>
//            Host.CreateDefaultBuilder(args)
//                .UseSerilog()
//                .ConfigureWebHostDefaults(webBuilder =>
//                {
//                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
//                    webBuilder.UseUrls($"h{port}");
//                    webBuilder.UseStartup<Startup>();
//                });
//    }
//}




//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace WebApplicationSampleTest2
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {

//            //  Serilog Configuration
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Error()
//                .WriteTo.Console()
//                .WriteTo.File(
//                    "Logs/log-.txt",
//                    rollingInterval: RollingInterval.Day
//                )
//                .CreateLogger();
//            CreateHostBuilder(args).Build().Run();
//        }

//        public static IHostBuilder CreateHostBuilder(string[] args) =>
//            Host.CreateDefaultBuilder(args)
//             .UseSerilog()
//                .ConfigureWebHostDefaults(webBuilder =>
//                {
//                    webBuilder.UseStartup<Startup>();
//                });
//    }
//}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationSampleTest2
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //  Serilog Configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Console()
                .WriteTo.File(
                    "Logs/log-.txt",
                    rollingInterval: RollingInterval.Day
                )
                .CreateLogger();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

