using Microsoft.Framework.Logging;
using ModernShopping.Lookup.Queries.Entities;
using MongoDB.Driver;
using System;
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

        public async Task<ProductQueryResult> ExecuteAsync(string productId)
        {
            if(productId == null)
            {
                throw new ArgumentNullException(nameof(productId));
            }

            var filter = Builders<Product>.Filter.Eq(p => p.Id, productId);
            var product = await _productCollection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);

            return product != null
                ? new ProductQueryResult(product)
                : null;
        }
    }
}
