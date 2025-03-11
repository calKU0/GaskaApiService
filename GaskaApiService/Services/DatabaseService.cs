using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GaskaApiService.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _username;
        private readonly string _password;
        private readonly string _ip;
        private readonly string _dbName;
        private readonly string _tableName;
        private readonly ILogger _logger;

        public DatabaseService(string username, string password, string ip, string dbName, string tableName, ILogger logger)
        {;
            _username = username;
            _password = password;
            _ip = ip;
            _dbName = dbName;
            _tableName = tableName;
            _logger = logger;
            _connectionString = $"server={ip};database={dbName};user={username};password={password}";
        }

        public async Task<int> UpdateProducts(List<Product> products)
        {
            int updatedRows = 0;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = $@"
                    USE {_dbName};
                    INSERT INTO {_tableName} (
                        `id`, 
                        `codeGaska`, 
                        `codeCustomer`, 
                        `name`, 
                        `unit`, 
                        `ean`, 
                        `description`, 
                        `technicalDetails`,
                        `netPrice`, 
                        `grossPrice`, 
                        `currencyPrice`, 
                        `netWeight`, 
                        `grossWeight`, 
                        `inStock`, 
                        `updated_times`, 
                        `valid`
                    )
                    VALUES (
                        @id, @codeGaska, @codeCustomer, @name, @unit, @ean, @description, @technicalDetails,
                        @netPrice, @grossPrice, @currencyPrice, @netWeight, @grossWeight, @quantity, 1, 1
                    )
                    ON DUPLICATE KEY UPDATE
                        `codeGaska` = @codeGaska,
                        `codeCustomer` = @codeCustomer,
                        `name` = @name,
                        `unit` = @unit,
                        `ean` = @ean,
                        `description` = @description,
                        `technicalDetails` = @technicalDetails,
                        `netPrice` = @netPrice,
                        `grossPrice` = @grossPrice,
                        `currencyPrice` = @currencyPrice,
                        `netWeight` = @netWeight,
                        `grossWeight` = @grossWeight,
                        `inStock` = @quantity,
                        `updated_times` = `updated_times` + 1;";


                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (Product product in products)
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@id", product.Id);
                            command.Parameters.AddWithValue("@codeGaska", product.CodeGaska);
                            command.Parameters.AddWithValue("@codeCustomer", product.CodeCustomer);
                            command.Parameters.AddWithValue("@name", product.Name);
                            command.Parameters.AddWithValue("@unit", product.Unit);
                            command.Parameters.AddWithValue("@ean", product.Ean);
                            command.Parameters.AddWithValue("@description", product.Description);
                            command.Parameters.AddWithValue("@technicalDetails", product.TechnicalDetails);
                            command.Parameters.AddWithValue("@netPrice", product.NetPrice);
                            command.Parameters.AddWithValue("@grossPrice", product.GrossPrice);
                            command.Parameters.AddWithValue("@currencyPrice", product.CurrencyPrice);
                            command.Parameters.AddWithValue("@netWeight", product.NetWeight);
                            command.Parameters.AddWithValue("@grossWeight", product.GrossWeight);
                            command.Parameters.AddWithValue("@quantity", product.InStock);

                            int result = 0;
                            try
                            {
                                result = await command.ExecuteNonQueryAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "Error while trying to update/insert product: @product", product);
                            }

                            if (result == 1)
                            {
                                _logger.Information($"Insered new product {product.CodeGaska} to database");
                                updatedRows++;
                            }
                            else if (result == 2)
                            {
                                _logger.Information($"Updated product {product.CodeGaska}");
                                updatedRows++;
                            }
                            else
                            {
                                _logger.Information($"Didn't update product {product.CodeGaska}");
                            }
                        }
                    }
                }

            }
            catch
            {
                throw;
            }

            return updatedRows;
        }
    }
}
