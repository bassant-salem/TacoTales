using FoodResturant.Data;
using FoodResturant.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoodResturant.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private Repository<Product> _products;
        private Repository<Order> _orders;
        private readonly UserManager<ApplicationUser> _userManager;
        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _products = new Repository<Product>(_context);
            _orders = new Repository<Order>(_context);
            _userManager = userManager;
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderModel") ?? new OrderViewModel
            {
                OrderItems = new List<OrderItemViewModel>(),
                Products = await _products.GetAllAsync()
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddItem(int prodId, int prodQty)
        {
            var product = await _context.Products.FindAsync(prodId);
            if (product == null)
            {
                return NotFound();
            }
            // Create or retrieve the order model from session
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel") ?? new OrderViewModel
            {
                OrderItems = new List<OrderItemViewModel>(),
                Products = await _products.GetAllAsync()
            };
            // Check if the product already exists in the order items
            var existingItem = model.OrderItems.FirstOrDefault(oi => oi.ProductId == prodId);

            if (existingItem != null)
            {
                // If it exists, update the quantity
                existingItem.Quantity += prodQty;
            }
            else
            {
                // If it doesn't exist, add a new order item
                model.OrderItems.Add(new OrderItemViewModel
                {
                    ProductId = product.ProductId,
                    Price = product.Price,
                    Quantity = prodQty,
                    ProductName = product.Name
                });

            }
            //update the total amount
            model.TotalAmount = model.OrderItems.Sum(oi => oi.Price * oi.Quantity);
            // Save the updated model back to session
            HttpContext.Session.Set("OrderViewModel", model);
            TempData["Success"] = $"{product.Name} added to cart!";
            return RedirectToAction("Create", model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null || !model.OrderItems.Any())
            {
                return RedirectToAction("Create");
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var model = HttpContext.Session.Get<OrderViewModel>("OrderViewModel");
            if (model == null || model.OrderItems.Count() == 0)
            {

                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Create");
            }
            // create a new order
            Order order = new Order()
            {
                OrderDate = DateTime.Now,
                TotalAmount = model.TotalAmount,
                UserId = _userManager.GetUserId(User)

            };
            // Add order items to the order
            foreach (var item in model.OrderItems)
            {
                order.OrderItems.Add(new OrderItem()
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }
            // Save the order to the database
            await _orders.AddAsync(order);
            // Clear the session
            HttpContext.Session.Remove("OrderViewModel");
            // Redirect to a confirmation page or order history
            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("ViewOrders");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ViewOrders()
        {
            var userId = _userManager.GetUserId(User);

            var userOrders = await _orders.GetAllByIdAsync(userId, "UserId", new QueryOptions<Order>
            {
                Includes = "OrderItems.Product"
            });
            return View(userOrders);
        }
    }
}
