using Google.Protobuf.Compiler;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;


namespace GaskaApiService
{
    public class APIService
    {
        private readonly string _username;
        private readonly string _acronym;
        private readonly string _person;
        private readonly string _password;
        private readonly string _key;
        private readonly string _baseUrl;
        private readonly ILogger _logger;

        public APIService(string acronym, string person, string password, string key, string baseUrl, ILogger logger)
        {
            _acronym = acronym;
            _person = person;
            _password = password;
            _key = key;
            _baseUrl = baseUrl;
            _logger = logger;
            _username = $"{acronym}|{person}";
        }

        public async Task<List<Product>> FetchProductsData(int productsResponsePerRequest)
        {
            List<Product> products = new List<Product>();
            try
            {
                var client = GetClientDefaultHeaders();
                string action = "products";

                var request = new RestRequest(action, Method.Get);
                request.AddParameter("lng", "pl");
                request.AddParameter("page", 1); // TODO: increment
                request.AddParameter("perPage", productsResponsePerRequest);

                _logger.Information($"Sending /products request with parameters: perPage={productsResponsePerRequest}, lng=pl");
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.Error($"Request Failed: {response.StatusCode} - {response.ErrorMessage}");
                    return products;
                }
                else
                {
                    string json = JsonPrettify(response.Content);
                    string productsJson = JsonConvert.DeserializeObject<dynamic>(json).products.ToString();

                    products = JsonConvert.DeserializeObject<List<Product>>(productsJson);
                    SaveJsonToFile(json, action);
                }
            }
            catch
            {
                throw;
            }

            return products;
        }

        private string GetSignature()
        {
            string body = $"acronym={_acronym}&person={_person}&password={_password}&key={_key}";
            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(body));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private RestClient GetClientDefaultHeaders()
        {
            var client = new RestClient(_baseUrl);
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            client.AddDefaultHeader("Authorization", $"Basic {credentials}");
            client.AddDefaultHeader("X-Signature", GetSignature());

            return client;
        }
        private string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }
        private void SaveJsonToFile(string jsonContent, string action)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                string fileName = $"{action}_{timestamp}.json";
                string jsonLogsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "json");

                if (!Directory.Exists(jsonLogsPath))
                {
                    Directory.CreateDirectory(jsonLogsPath);
                }

                string filePath = Path.Combine(jsonLogsPath, fileName);

                File.WriteAllText(filePath, jsonContent);
                _logger.Information("Request Succedeed. JSON response saved to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving JSON to file");
            }
        }
    }
}
