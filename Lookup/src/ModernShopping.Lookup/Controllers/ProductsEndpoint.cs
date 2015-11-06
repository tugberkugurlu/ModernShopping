using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;
using ModernShopping.Lookup.Queries;
using System;
using System.Threading.Tasks;

namespace ModernShopping.Lookup.Controllers
{
    [Route("products")]
    public class ProductsEndpoint : Controller
    {
        private readonly ProductByIdQuery _byIdQuery;
        private readonly ILogger _logger;

        public ProductsEndpoint(ProductByIdQuery byIdQuery, ILoggerFactory loggerFactory)
        {
            if(loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (byIdQuery == null)
            {
                throw new ArgumentNullException(nameof(byIdQuery));
            }

            _byIdQuery = byIdQuery;
            _logger = loggerFactory.CreateLogger<ProductsEndpoint>();
        }

        [Route("{productId}")]
        public async Task<IActionResult> Get(string productId)
        {
            IActionResult result;
            var productResult = await _byIdQuery.ExecuteAsync(productId);

            if(productResult == null)
            {
                _logger.LogInformation("Product {productId} doesn't exist.", productId);
                result = HttpNotFound();
            }
            else if (productResult.Product.DeletedOn != null)
            {
                _logger.LogInformation("Product {productId} exists but has been marked as deleted.", productId);
                result = new HttpStatusCodeResult(410);
            }
            else
            {
                _logger.LogVerbose("Product {productId} has been found.", productId);
                result = Ok(productResult.Product);
            }

            return result;
        }

        [Route("{productId}/images")]
        public IActionResult GetImages(string productId)
        {
            throw new NotImplementedException();
        }
    }
}