using FoodResturant.Data;
using FoodResturant.Models;
using FoodResturant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodResturant.Controllers
{
    // Thin HTTP layer — validates identity, calls CartService,
   
    [Authorize]
    public class OrderController : Controller
    {
        private readonly CartService _cartService;
        private readonly Repository<Product> _products;
        private readonly Repository<Order> _orders;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            CartService cartService,
            UserManager<ApplicationUser> userManager,
            ILogger<OrderController> logger)
        {
            _cartService = cartService;
            _products = new Repository<Product>(context);
            _orders = new Repository<Order>(context);
            _userManager = userManager;
            _logger = logger;
        }

    
        // GET /Order/Create  — menu page
     
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cart = _cartService.GetCart();

            // Always refresh the product list from DB
            cart.Products = await _products.GetAllAsync();

            return View(cart);
        }

      
        // POST /Order/AddItem
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int prodId, int prodQty)
        {
            var userId = _userManager.GetUserId(User) ?? "anonymous";

            var result = await _cartService.AddItemAsync(prodId, prodQty, userId);

            if (!result.IsValid)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Create));
            }

            // Fetch product name for the success toast
            // (CartService already validated it exists, so FindAsync won't be null here)
            var product = await _products.GetByIdAsync(prodId, new QueryOptions<Product>());
            TempData["Success"] = $"{product?.Name ?? "Item"} added to cart!";

            return RedirectToAction(nameof(Create));
        }

      
        // POST /Order/UpdateQuantity
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int prodId, int prodQty)
        {
            var userId = _userManager.GetUserId(User) ?? "anonymous";

            var result = _cartService.UpdateQuantity(prodId, prodQty, userId);

            if (!result.IsValid)
            {
                TempData["Error"] = result.ErrorMessage;

                // If cart was empty, send back to menu
                return _cartService.CartHasItems()
                    ? RedirectToAction(nameof(Cart))
                    : RedirectToAction(nameof(Create));
            }

            TempData["Success"] = prodQty == 0 ? "Item removed from cart." : "Cart updated.";
            return RedirectToAction(nameof(Cart));
        }

        // POST /Order/RemoveItem
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int prodId)
        {
            var userId = _userManager.GetUserId(User) ?? "anonymous";

            var result = _cartService.RemoveItem(prodId, userId);

            if (!result.IsValid)
            {
                TempData["Error"] = result.ErrorMessage;
            }
            else
            {
                TempData["Success"] = "Item removed from cart.";
            }

            // If cart is now empty, redirect to menu
            return _cartService.CartHasItems()
                ? RedirectToAction(nameof(Cart))
                : RedirectToAction(nameof(Create));
        }

    
        // GET /Order/Cart
   
        [HttpGet]
        public IActionResult Cart()
        {
            if (!_cartService.CartHasItems())
            {
                // Silent redirect — no error toast for navbar click on empty cart
                return RedirectToAction(nameof(Create));
            }

            var cart = _cartService.GetCart();
            return View(cart);
        }

 
        // POST /Order/PlaceOrder
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("PlaceOrder called with no authenticated user.");
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction(nameof(Create));
            }

            var result = await _cartService.PlaceOrderAsync(userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(
                    result.ErrorMessage.Contains("empty") ? nameof(Create) : nameof(Cart));
            }

            // Partial success — some items were unavailable
            if (result.Warnings.Any())
                TempData["Warning"] = string.Join(" ", result.Warnings);

            TempData["Success"] = "Order placed successfully! 🌮";
            return RedirectToAction(nameof(ViewOrders));
        }

     
        // GET /Order/ViewOrders
    
        [HttpGet]
        public async Task<IActionResult> ViewOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _orders.GetAllByIdAsync(userId, "UserId",
                new QueryOptions<Order>
                {
                    Includes = "OrderItems.Product"
                });

            return View(orders);
        }

        
        // GET /Order/OrderDetails/{id}
      
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (id <= 0)
                return BadRequest();

            var userId = _userManager.GetUserId(User);

            var order = await _orders.GetByIdAsync(id,
                new QueryOptions<Order>
                {
                    Includes = "OrderItems.Product"
                });

            if (order == null)
                return NotFound();

            // Prevent users from viewing each other's orders
            if (order.UserId != userId)
            {
                _logger.LogWarning(
                    "OrderDetails — user {UserId} attempted to view order {OrderId} " +
                    "belonging to user {OwnerId}.",
                    userId, id, order.UserId);
                return Forbid();
            }

            return View(order);
        }
    }
}