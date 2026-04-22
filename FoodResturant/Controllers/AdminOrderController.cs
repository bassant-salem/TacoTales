using FoodResturant.Data;
using FoodResturant.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodResturant.Controllers
{
    /// <summary>
    /// Admin-only controller for managing all customer orders.
    /// Handles status advancement (state machine) and cancellation.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Repository<Order> _orders;
        private readonly ILogger<AdminOrderController> _logger;

        public AdminOrderController(
            ApplicationDbContext context,
            ILogger<AdminOrderController> logger)
        {
            _context = context;
            _orders = new Repository<Order>(context);
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        // GET /AdminOrder/Index — all orders dashboard
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string? statusFilter = null)
        {
            // Use context directly so we can Include navigation properties
            // (Repository.GetAllAsync has no overload for QueryOptions)
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .AsQueryable();

            // Optional status filter
            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<OrderStatus>(statusFilter, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
            }

            var orders = await query.ToListAsync();

            // Sort: active orders first, then by date descending
            orders = orders
                .OrderBy(o => o.Status == OrderStatus.Delivered ||
                               o.Status == OrderStatus.Cancelled ? 1 : 0)
                .ThenByDescending(o => o.OrderDate)
                .ToList();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.StatusOptions = Enum.GetValues<OrderStatus>();

            return View(orders);
        }

        // ─────────────────────────────────────────────
        // POST /AdminOrder/Advance/{id}
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Advance(int id)
        {
            if (id <= 0)
                return BadRequest();

            var order = await _orders.GetByIdAsync(id, new QueryOptions<Order>());
            if (order == null)
                return NotFound();

            if (!order.CanAdvance())
            {
                _logger.LogWarning(
                    "Advance blocked — order {OrderId} is already in terminal status {Status}.",
                    id, order.Status);

                TempData["Error"] =
                    $"Order #{id} cannot be advanced — it is already {order.StatusLabel()}.";
                return RedirectToAction(nameof(Index));
            }

            var previousStatus = order.Status;
            order.Advance();

            try
            {
                await _orders.UpdateAsync(order);

                _logger.LogInformation(
                    "Order {OrderId} advanced: {From} → {To} by admin.",
                    id, previousStatus, order.Status);

                TempData["Success"] = $"Order #{id} updated to {order.StatusLabel()}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to advance order {OrderId} from {Status}.", id, previousStatus);

                TempData["Error"] =
                    $"Something went wrong updating order #{id}. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // POST /AdminOrder/Cancel/{id}
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (id <= 0)
                return BadRequest();

            var order = await _orders.GetByIdAsync(id, new QueryOptions<Order>());
            if (order == null)
                return NotFound();

            if (!order.CanCancel())
            {
                _logger.LogWarning(
                    "Cancel blocked — order {OrderId} is in status {Status}, not Pending.",
                    id, order.Status);

                TempData["Error"] =
                    $"Order #{id} cannot be cancelled — it is already {order.StatusLabel()}.";
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderStatus.Cancelled;

            try
            {
                await _orders.UpdateAsync(order);

                _logger.LogInformation("Order {OrderId} cancelled by admin.", id);

                TempData["Success"] = $"Order #{id} has been cancelled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {OrderId}.", id);

                TempData["Error"] =
                    $"Something went wrong cancelling order #{id}. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // GET /AdminOrder/Details/{id}
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
