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
using System.Timers;

namespace GaskaApiService
{
    public partial class GaskaApiService: ServiceBase
    {
        #region AppConfig
        // Config values
        private readonly int _startProductsFetchHour = Convert.ToInt16(ConfigurationManager.AppSettings["StartProductsFetchHour"]);
        private readonly int _endProductsFetchHour = Convert.ToInt16(ConfigurationManager.AppSettings["EndProductsFetchHour"]);
        private readonly int _productsResponsePerRequest = Convert.ToInt32(ConfigurationManager.AppSettings["ProductsResponsePerRequest"]);
        private readonly int _logsExpirationDays = Convert.ToInt16(ConfigurationManager.AppSettings["LogsExpirationDays"]);

        // Database credentials
        private readonly string _dbUsername = ConfigurationManager.AppSettings["DbUsername"].ToString();
        private readonly string _dbPassword = ConfigurationManager.AppSettings["DbPassword"].ToString();
        private readonly string _dbName = ConfigurationManager.AppSettings["DbName"].ToString();
        private readonly string _dbTableName = ConfigurationManager.AppSettings["DbTableName"].ToString();
        private readonly string _dbIp = ConfigurationManager.AppSettings["DbIp"].ToString();

        // API credentials
        private readonly string _apiAcronym = ConfigurationManager.AppSettings["ApiAcronym"].ToString();
        private readonly string _apiPerson = ConfigurationManager.AppSettings["ApiPerson"].ToString();
        private readonly string _apiPassword = ConfigurationManager.AppSettings["ApiPassword"].ToString();
        private readonly string _apiKey = ConfigurationManager.AppSettings["ApiKey"].ToString();
        private readonly string _apiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"].ToString();
        #endregion

        private readonly ILogger _logger;
        private readonly APIService _apiHandler;
        private readonly DatabaseService _dbService;
        private Timer _timer;
        private DateTime _lastFetched = DateTime.Today.AddDays(-1);
        public GaskaApiService()
        {
            // Serilog configuration and initialization
            _logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", @"log.txt")
                , rollingInterval: RollingInterval.Day
                , retainedFileCountLimit: _logsExpirationDays)
                .CreateLogger();

            _logger.Information("Service is starting up...");

            // Services initialization
            _apiHandler = new APIService(_apiAcronym, _apiPerson, _apiPassword, _apiKey, _apiBaseUrl, _logger);
            _dbService = new DatabaseService(_dbUsername, _dbPassword, _dbIp, _dbName, _dbTableName, _logger);

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Information("Service started.");

            CheckAndFetchData();

            _timer = new Timer(10000); 
            _timer.Elapsed += (sender, e) => CheckAndFetchData();
            _timer.Start();
        }

        private void CheckAndFetchData()
        {
            var currentTime = DateTime.Now.Hour;
            if (currentTime >= _startProductsFetchHour && currentTime < _endProductsFetchHour && _lastFetched.Date < DateTime.Now.Date)
            {
                Task.Run(() => FetchData());
            }
        }


        private async Task FetchData()
        {
            try
            {
                _lastFetched = DateTime.Now.Date;
                DeleteOldJsonFiles(_logsExpirationDays);

                int pageNumber = 1;
                List<Product> allProducts = new List<Product>();

                _logger.Information("Starting product fetching...");
                while (true)
                {
                    List<Product> products = await _apiHandler.FetchProductsData(_productsResponsePerRequest, pageNumber);

                    if (!products.Any())
                    {
                        _logger.Information($"No more products found. Stopping at page {pageNumber}.");
                        break;
                    }

                    _logger.Information($"Fetched {products.Count} products on page {pageNumber}.");

                    allProducts.AddRange(products);
                    pageNumber++;
                }

                allProducts = allProducts.Distinct().ToList();

                _logger.Information($"Total products fetched: {allProducts.Count}");

                if (allProducts.Count > 0)
                {
                    _logger.Information("Updating database with all fetched products...");
                    int rowsAffected = await _dbService.UpdateProducts(allProducts);
                    _logger.Information($"Updated {rowsAffected} products in the database.");
                }
                else
                {
                    _logger.Information("No products to update in the database.");
                }
                
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Error while fetching products.");
            }
        }

        public void DeleteOldJsonFiles(int days)
        {
            try
            {
                string jsonLogsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "json");

                if (Directory.Exists(jsonLogsPath))
                {
                    string[] files = Directory.GetFiles(jsonLogsPath, "*.json");

                    foreach (var file in files)
                    {
                        DateTime lastModified = File.GetLastWriteTime(file);

                        if (lastModified < DateTime.Now.AddDays(days))
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during file cleanup:");
            }
        }
        protected override void OnStop()
        {
            _logger.Information("Service stopped.");
            Log.CloseAndFlush();
        }
    }
}
