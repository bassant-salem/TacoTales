using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodResturant.Models
{
    public class Product
    {
        public Product()
        {
            ProductIngredients = new List<ProductIngredient>();
        }

        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }

        // Image stored in database (survives redeploys)
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }

        // Keep this for reference/filename only
        public string? ImageUrl { get; set; }

        // Not saved to DB - just for upload form
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [ValidateNever]
        public Category? Category { get; set; }

        [ValidateNever]
        public ICollection<OrderItem>? OrderItems { get; set; }

        [ValidateNever]
        public ICollection<ProductIngredient>? ProductIngredients { get; set; }
    }
}