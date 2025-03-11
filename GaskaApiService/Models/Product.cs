using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GaskaApiService
{
    public class Product
    {
        public int Id { get; set; }
        public string CodeGaska { get; set; }
        public string CodeCustomer { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string Ean { get; set; }
        public string Description { get; set; }
        public string TechnicalDetails { get; set; }
        public decimal NetPrice { get; set; }
        public decimal GrossPrice { get; set; }
        public string CurrencyPrice { get; set; }
        public decimal NetWeight { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal InStock { get; set; }
    }
}
