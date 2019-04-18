using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FileSenderViaAmazonAPI
{
    
    class Program
    {
        private static IAmazonSender sender;

        static void Main(string[] args)
        {
            GlobalConfiguring();

            if (!File.Exists(sender.filePath))
            {
                Console.WriteLine($"File {sender.filePath} not exists!");
                sender.logger.LogError($"File {sender.filePath} not exists!");
                //Console.ReadKey();
                //return;
            }

            Console.WriteLine($"Start sending file \nbucketName={sender.bucketName}\npath={sender.filePath}\nkeyName{sender.keyName}");
            sender.logger.LogInformation($"Start sending file: bucketName={sender.bucketName}; path={sender.filePath}; keyName{sender.keyName}");

            sender.UploadFileAsync().Wait();
            Console.ReadKey();
        }

        private static void GlobalConfiguring()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            sender = serviceProvider.GetService<IAmazonSender>();
            ConfigureLog(serviceProvider, sender);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
                .AddTransient<IAmazonSender, AmazonSender>();
        }

        private static void ConfigureLog(ServiceProvider serviceProvider, IAmazonSender _sender)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("FileSenderViaAmazonAPI.log")
                .CreateLogger();
            _sender.logger = serviceProvider.GetService<ILogger<IAmazonSender>>();
            _sender.logger.LogInformation("ConfigureLog() finished!");
        }
    }
}
