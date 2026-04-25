using Microsoft.EntityFrameworkCore;
using FoodResturant.Data;
using FoodResturant.Models;
using FoodResturant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodResturant.Controllers
{
   
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
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
            _context = context;
            _cartService = cartService;
            _products = new Repository<Product>(context);
            _orders = new Repository<Order>(context);
            _userManager = userManager;
            _logger = logger;
        }

      
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cart = _cartService.GetCart();

            
            cart.Products = await _products.GetAllAsync();

            return View(cart);
        }

        
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

            
            var product = await _products.GetByIdAsync(prodId, new QueryOptions<Product>());
            TempData["Success"] = $"{product?.Name ?? "Item"} added to cart!";

            return RedirectToAction(nameof(Create));
        }

     
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

       
        [HttpGet]
        public IActionResult Cart()
        {
            if (!_cartService.CartHasItems())
            {
                return RedirectToAction(nameof(Create));
            }

            var cart = _cartService.GetCart();
            return View(cart);
        }

      
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

            
            if (result.Warnings.Any())
                TempData["Warning"] = string.Join(" ", result.Warnings);

            TempData["Success"] = "Order placed successfully! 🌮";
            return RedirectToAction(nameof(ViewOrders));
        }

     
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

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            if (id <= 0)
                return BadRequest();

            var userId = _userManager.GetUserId(User);

            
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

         
            if (order.UserId != userId)
            {
                _logger.LogWarning(
                    "CancelOrder — user {UserId} attempted to cancel order {OrderId} " +
                    "belonging to user {OwnerId}.",
                    userId, id, order.UserId);
                return Forbid();
            }

           
            if (!order.CanCancel())
            {
                _logger.LogWarning(
                    "CancelOrder blocked — order {OrderId} is {Status}, not Pending.",
                    id, order.Status);
                TempData["Error"] =
                    $"Order #{id} cannot be cancelled — it is already {order.StatusLabel()}.";
                return RedirectToAction(nameof(ViewOrders));
            }

            order.Status = OrderStatus.Cancelled;

            try
            {
                await _orders.UpdateAsync(order);

                
                await _cartService.RestoreStockAsync(order, userId!);

                _logger.LogInformation(
                    "Order {OrderId} cancelled by user {UserId}.", id, userId);

                TempData["Success"] = $"Order #{id} has been cancelled and stock restored.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to cancel order {OrderId} for user {UserId}.", id, userId);
                TempData["Error"] =
                    "Something went wrong cancelling your order. Please try again.";
            }

            return RedirectToAction(nameof(ViewOrders));
        }

       
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