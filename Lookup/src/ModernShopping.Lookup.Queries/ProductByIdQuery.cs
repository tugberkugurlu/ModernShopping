using Microsoft.Framework.Logging;
using ModernShopping.Lookup.Queries.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModernShopping.Lookup.Queries
{
    public class ProductByIdQuery
    {
        private readonly IMongoCollection<Product> _productCollection;
        private readonly ILogger<ProductByIdQuery> _logger;

        public ProductByIdQuery(IMongoCollection<Product> productCollection, ILogger<ProductByIdQuery> logger)
        {
            _productCollection = productCollection;
            _logger = logger;
        }

        public Task<ProductQueryResult> Execute(string productId)
        {
            throw new NotImplementedException();
        }
    }

    public class ProductQueryResult
    {
        public ProductQueryResult(Product product)
        {
            Product = product;
        }

        public Product Product { get; }
    }
}
