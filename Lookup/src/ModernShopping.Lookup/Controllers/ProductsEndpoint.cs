using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Logging;
using System;

namespace ModernShopping.Lookup.Controllers
{
    [Route("products")]
    public class ProductsEndpoint : Controller
    {
        private readonly ILogger _logger;

        public ProductsEndpoint(ILoggerFactory loggerFactory)
        {
            if(loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<ProductsEndpoint>();
        }

        public IActionResult Get()
        {
            return Ok(new[]
            {
                "Product 1",
                "Product 2"
            });
        }

        [Route("{productId}")]
        public IActionResult Get(string productId)
        {
            throw new NotImplementedException();
        }

        [Route("{productId}/images")]
        public IActionResult GetImages(string productId)
        {
            throw new NotImplementedException();
        }
    }
}