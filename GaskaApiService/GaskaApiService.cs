using GaskaApiService.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace GaskaApiService
{
    public partial class GaskaApiService: ServiceBase
    {
        #region AppConfig
        // Config values
        private readonly int _startProductsFetchHour = Convert.ToInt16(ConfigurationManager.AppSettings["StartProductsFetchHour"]);
        private readonly int _endProductsFetchHour = Convert.ToInt16(ConfigurationManager.AppSettings["EndProductsFetchHour"]);
        private readonly int _productsFetchingInterval = Convert.ToInt16(ConfigurationManager.AppSettings["ProductsFetchingInterval"]);
        private readonly int _productsResponsePerRequest = Convert.ToInt16(ConfigurationManager.AppSettings["ProductsResponsePerRequest"]);

        // API credentials
        private readonly string _apiAcronym = ConfigurationManager.AppSettings["ApiAcronym"].ToString();
        private readonly string _apiPerson = ConfigurationManager.AppSettings["ApiPerson"].ToString();
        private readonly string _apiPassword = ConfigurationManager.AppSettings["ApiPassword"].ToString();
        private readonly string _apiKey = ConfigurationManager.AppSettings["ApiKey"].ToString();
        private readonly string _apiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"].ToString();

        // Connection string 
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MySQLConnectionString"].ConnectionString;
        #endregion
        private readonly ILogger _logger;
        private readonly APIService _apiHandler;
        private readonly DatabaseService _dbService;
        public GaskaApiService()
        {
            // Serilog configuration and initialization
            _logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", @"log.txt")
                , rollingInterval: RollingInterval.Day
                , retainedFileCountLimit: 31)
                .CreateLogger();

            _logger.Information("Service is starting up...");

            // Services initialization
            _apiHandler = new APIService(_apiAcronym, _apiPerson, _apiPassword, _apiKey, _apiBaseUrl, _logger);
            _dbService = new DatabaseService(connectionString, _logger);

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Information("Service started.");

            Task.Run(() => FetchData());
        }

        private async Task FetchData()
        {
            try
            {
                var currentTime = DateTime.Now.Hour;

                if (currentTime >= _startProductsFetchHour && currentTime < _endProductsFetchHour)
                {
                    _logger.Information("Fetching products data...");
                    List<Product> products = await _apiHandler.FetchProductsData(_productsResponsePerRequest);
                    _logger.Information($"Fetched {products.Count} products.");

                    // TODO: 
                    // Save products to database


                    /*if (products.Count > 0)
                    {
                        Log.Information("Updating database...");
                        int rowsAffected = await _dbService.UpdateProducts(products);
                        Log.Information($"Updated {products.Count} products.");
                    }*/

                }

                /*var interval = TimeSpan.FromSeconds(_productsFetchingInterval);
                while (true)
                {
                    Task.Delay(interval).Wait();
                    currentTime = DateTime.Now.Hour;

                    if (currentTime >= _startProductsFetchHour && currentTime < _endProductsFetchHour)
                    {
                        _logger.Information("Fetching products data...");
                        List<Product> products = await _apiHandler.FetchProductsData(_productsResponsePerRequest);
                        _logger.Information($"Fetched {products.Count} products.");

                        if (products.Count > 0)
                        {
                            Log.Information("Updating database...");
                            int rowsAffected = await _dbService.UpdateProducts(products);
                            Log.Information($"Updated {products.Count} products.");
                        }

                    }
                }*/
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Error while fetching products.");
            }
        }
        protected override void OnStop()
        {
            _logger.Information("Service stopped.");
            Log.CloseAndFlush();
        }
    }
}
