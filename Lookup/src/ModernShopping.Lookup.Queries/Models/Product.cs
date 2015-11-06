using System;

namespace ModernShopping.Lookup.Queries.Entities
{
    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string PrimaryImageUrl { get; set; }
        public BrandReference Brand { get; set; }

        public Occurrence DeletedOn { get; set; }
    }

    public class Occurrence
    {
        public Occurrence() : this(DateTime.UtcNow)
        {
        }

        public Occurrence(DateTime occurredOn)
        {
        }

        public DateTime OccurredOn { get; private set; }
    }

    public class BrandReference
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }

    public class ProductRatingSummary
    {
        public long TotalCount { get; set; }
        public byte AverageRating { get; set; }
    }

    public class ProductReviewSummary
    {
        public long TotalCount { get; set; }
    }
}