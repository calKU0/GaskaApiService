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
        private readonly ILogger _logger;

        public DatabaseService(string connectionString, ILogger logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<int> UpdateProducts(List<Product> products)
        {
            int updatedRows = 0;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO `products` (
                                  `idGaska`, 
                                  `codeGaska`, 
                                  `name`, 
                                  `unit`, 
                                  `ean`, 
                                  `weight`, 
                                  `description`, 
                                  `price`, 
                                  `currency`, 
                                  `availabilityQty`, 
                                  `availability`, 
                                  `crawlts`, 
                                  `updated_times`, 
                                  `valid`
                                ) 
                                VALUES (
                                  @id, @code, @name, @unit, @ean, @weight, @description, @price, @currency, @quantity, 1, 1, 1, 1
                                )
                                ON DUPLICATE KEY UPDATE
                                  `codeGaska` = @code,
                                  `name` = @name,
                                  `unit` = @unit,
                                  `ean` = @ean,
                                  `weight` = @weight,
                                  `description` = @description,
                                  `price` = @price,
                                  `currency` = @currency,
                                  `availabilityQty` = @quantity,
                                  `availability` = 1,
                                  `crawlts` = 1,
                                  `updated_times` = updated_times + 1,
                                  `valid` = 1;
                                ";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (Product product in products)
                        {
                            command.Parameters.AddWithValue("@id", product.Id);
                            command.Parameters.AddWithValue("@code", product.Code);
                            command.Parameters.AddWithValue("@name", product.Name);
                            command.Parameters.AddWithValue("@unit", product.Unit);
                            command.Parameters.AddWithValue("@ean", product.Ean);
                            command.Parameters.AddWithValue("@weight", product.Weight);
                            command.Parameters.AddWithValue("@description", product.Description);
                            command.Parameters.AddWithValue("@price", product.Price);
                            command.Parameters.AddWithValue("@currency", product.Currency);
                            command.Parameters.AddWithValue("@quantity", product.Quantity);

                            int result = await command.ExecuteNonQueryAsync();
                            updatedRows += result;
                            if (result >= 1)
                            {
                                _logger.Information($"Updated product {product.Code}");
                            }
                            else
                            {
                                _logger.Information($"Couldn't update product {product.Code}");
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
