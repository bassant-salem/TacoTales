using FoodResturant.Data;
using FoodResturant.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace TequliasRestaurant.Controllers
{
    public class ProductController : Controller
    {
        private Repository<Product> products;
        private Repository<Ingredient> ingredients;
        private Repository<Category> categories;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            products = new Repository<Product>(context);
            ingredients = new Repository<Ingredient>(context);
            categories = new Repository<Category>(context);
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            return View(await products.GetAllAsync());
        }

        // ✅ New action to serve images from database
        public async Task<IActionResult> GetImage(int id)
        {
            var product = await products.GetByIdAsync(id, new QueryOptions<Product>());
            if (product?.ImageData == null)
            {
                // Return placeholder if no image
                return Redirect("https://via.placeholder.com/150");
            }
            return File(product.ImageData, product.ImageMimeType ?? "image/jpeg");
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.Ingredients = await ingredients.GetAllAsync();
            ViewBag.Categories = await categories.GetAllAsync();
            if (id == 0)
            {
                ViewBag.Operation = "Add";
                return View(new Product());
            }
            else
            {
                Product product = await products.GetByIdAsync(id, new QueryOptions<Product>
                {
                    Includes = "ProductIngredients.Ingredient, Category"
                });
                ViewBag.Operation = "Edit";
                return View(product);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(Product product, int[] ingredientIds, int catId)
        {
            ViewBag.Ingredients = await ingredients.GetAllAsync();
            ViewBag.Categories = await categories.GetAllAsync();
            if (ModelState.IsValid)
            {
                if (product.ImageFile != null)
                {
                    // ✅ Save image to database instead of disk
                    using var memoryStream = new MemoryStream();
                    await product.ImageFile.CopyToAsync(memoryStream);
                    product.ImageData = memoryStream.ToArray();
                    product.ImageMimeType = product.ImageFile.ContentType;
                    product.ImageUrl = product.ImageFile.FileName;
                }

                if (product.ProductId == 0)
                {
                    product.CategoryId = catId;
                    foreach (int id in ingredientIds)
                    {
                        product.ProductIngredients?.Add(new ProductIngredient { IngredientId = id, ProductId = product.ProductId });
                    }
                    await products.AddAsync(product);
                    TempData["Success"] = "Product added successfully!";
                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    var existingProduct = await products.GetByIdAsync(product.ProductId, new QueryOptions<Product> { Includes = "ProductIngredients" });

                    if (existingProduct == null)
                    {
                        ModelState.AddModelError("", "Product not found.");
                        return View(product);
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.Stock = product.Stock;
                    existingProduct.CategoryId = catId;

                    // ✅ Only update image if a new one was uploaded
                    if (product.ImageFile != null)
                    {
                        existingProduct.ImageData = product.ImageData;
                        existingProduct.ImageMimeType = product.ImageMimeType;
                        existingProduct.ImageUrl = product.ImageUrl;
                    }

                    existingProduct.ProductIngredients?.Clear();
                    foreach (int id in ingredientIds)
                    {
                        existingProduct.ProductIngredients?.Add(new ProductIngredient { IngredientId = id, ProductId = product.ProductId });
                    }

                    try
                    {
                        await products.UpdateAsync(existingProduct);
                        TempData["Success"] = "Product updated successfully!";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error: {ex.GetBaseException().Message}");
                        return View(product);
                    }
                }
            }
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await products.DeleteAsync(id);
                TempData["Success"] = "Product deleted successfully!";
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("", "Product not found.");
                return RedirectToAction("Index");
            }
        }
    }
}