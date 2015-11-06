using ModernShopping.Lookup.Queries.Entities;
using System;

namespace ModernShopping.Lookup.Queries
{
    public class ProductQueryResult
    {
        public ProductQueryResult(Product product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            Product = product;
        }

        public Product Product { get; }
    }
}
