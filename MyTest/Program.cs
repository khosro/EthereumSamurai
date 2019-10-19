using Lykke.Service.EthereumSamurai.Core.Settings;
using Lykke.Service.EthereumSamurai.Models.DebugModels;
using Lykke.Service.EthereumSamurai.Services;
using Nethereum.Geth;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DebugTransaction().GetAwaiter().GetResult();
        }

        static async Task DebugTransaction()
        {
            string ethereumRpcUrl = "https://ropsten.infura.io/v3/7238211010344719ad14a89db874158c";
            ethereumRpcUrl = "http://192.168.41.93:8510";
            var web3 = new Web3Decorator(new Web3());
            DebugDecorator debugDecorator = new DebugDecorator(new DebugApiService(web3.Client), new BaseSettings() { EthereumRpcUrl = ethereumRpcUrl });

            TraceResultModel traceResult = await debugDecorator.TraceTransactionAsync
                   (
                       "",
                       "",
                       "",
                      0,
                       "0x755babb47619dc781c3ad723946b41d12c8f8c01d677c8b5ae36630a0dd91f8b",
                       //"0x7cc0a70b5535ff6458b0658a2792591d2c014f677fe4762e7fae940923ce5d1c",
                       false,
                       true,
                       false
                   );
        }
        static void Test()
        {
            AppSettings appSettings = new AppSettings();
            appSettings.EthereumIndexer = new BaseSettings()
            {
                DB = new DB() { MongoDBConnectionString = "mongodb://localhsot:27017/admin" }
                ,
                EthereumRpcUrl = "http://localhost:9093",
                RabbitMq = new RabbitMq()
                {
                    ExternalHost = "http://localhost",
                    Port = 2020,
                    Username = "salam",
                    Password = "123456",
                    RoutingKey = "www",

                }
            };
            var json = JsonConvert.SerializeObject(appSettings);
        }
    }
}
