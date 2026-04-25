using FoodResturant.Data;
using FoodResturant.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodResturant.Services
{
   
    public class CartService
    {
        private const int MaxItemQty = 99;
        private const int MaxCartItems = 20;

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public CartService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string CurrentCartKey()
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning(
                    "CartService — could not resolve userId from claims. Using fallback key.");
                return "Cart_anonymous";
            }

            return $"Cart_{userId}";
        }

      
        public OrderViewModel GetCart()
        {
            var key = CurrentCartKey();
            var cart = Session.Get<OrderViewModel>(key);

            if (cart == null)
            {
                _logger.LogDebug("No cart found for key {CartKey} — returning empty cart.", key);
                cart = new OrderViewModel
                {
                    OrderItems = new List<OrderItemViewModel>()
                };
            }

            return cart;
        }

        
        private void SaveCart(OrderViewModel cart)
        {
            cart.TotalAmount = cart.OrderItems.Sum(i => i.Price * i.Quantity);
            Session.Set(CurrentCartKey(), cart);
        }

        
        public void ClearCart()
        {
            var key = CurrentCartKey();
            Session.Remove(key);
            _logger.LogInformation("Cart cleared for key {CartKey}.", key);
        }

        
        public bool CartHasItems()
        {
            var cart = Session.Get<OrderViewModel>(CurrentCartKey());
            return cart != null && cart.OrderItems.Any();
        }

       
        public CartValidationResult ValidateAddInput(int prodId, int prodQty)
        {
            if (prodId <= 0)
            {
                _logger.LogWarning("AddItem rejected — invalid prodId: {ProdId}", prodId);
                return CartValidationResult.Fail("Invalid product selected.");
            }

            if (prodQty <= 0)
            {
                _logger.LogWarning("AddItem rejected — quantity <= 0: {Qty}", prodQty);
                return CartValidationResult.Fail("Quantity must be at least 1.");
            }

            if (prodQty > MaxItemQty)
            {
                _logger.LogWarning("AddItem rejected — quantity {Qty} exceeds max {Max}.",
                    prodQty, MaxItemQty);
                return CartValidationResult.Fail($"You can add at most {MaxItemQty} units at a time.");
            }

            return CartValidationResult.Ok();
        }

       
        public CartValidationResult ValidateUpdateInput(int prodId, int prodQty)
        {
            if (prodId <= 0)
            {
                _logger.LogWarning("UpdateQuantity rejected — invalid prodId: {ProdId}", prodId);
                return CartValidationResult.Fail("Invalid product.");
            }

            
            if (prodQty < 0 || prodQty > MaxItemQty)
            {
                _logger.LogWarning(
                    "UpdateQuantity rejected — quantity {Qty} out of range [0,{Max}].",
                    prodQty, MaxItemQty);
                return CartValidationResult.Fail($"Quantity must be between 0 and {MaxItemQty}.");
            }

            return CartValidationResult.Ok();
        }

       
        public async Task<CartValidationResult> AddItemAsync(int prodId, int prodQty, string userId)
        {
            var inputCheck = ValidateAddInput(prodId, prodQty);
            if (!inputCheck.IsValid)
                return inputCheck;

            
            Product? product;
            try
            {
                product = await _context.Products.FindAsync(prodId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DB error looking up product {ProdId} during AddItem for user {UserId}.",
                    prodId, userId);
                return CartValidationResult.Fail("Something went wrong. Please try again.");
            }

            if (product == null)
            {
                _logger.LogWarning(
                    "AddItem — product {ProdId} not found in DB (user {UserId}).",
                    prodId, userId);
                return CartValidationResult.Fail("That menu item no longer exists.");
            }

            
            if (product.Stock <= 0)
            {
                _logger.LogInformation(
                    "AddItem blocked — {Name} is out of stock for user {UserId}.",
                    product.Name, userId);
                return CartValidationResult.Fail(
                    $"Sorry, {product.Name} is currently out of stock.");
            }

            var cart = GetCart();

            var existing = cart.OrderItems.FirstOrDefault(i => i.ProductId == prodId);

            if (existing != null)
            {
                
                if (existing.Price != product.Price)
                {
                    _logger.LogInformation(
                        "Price change detected for product {ProdId} ({Name}): " +
                        "cart had {OldPrice}, DB has {NewPrice}. Updating cart. User {UserId}.",
                        prodId, product.Name, existing.Price, product.Price, userId);

                    existing.Price = product.Price;
                }

                
                var newQty = existing.Quantity + prodQty;
                if (newQty > product.Stock)
                {
                    _logger.LogInformation(
                        "AddItem stock cap — user {UserId} tried {Add} of {Name} " +
                        "but only {Stock} available ({Existing} already in cart).",
                        userId, prodQty, product.Name, product.Stock, existing.Quantity);

                    return CartValidationResult.Fail(
                        $"Only {product.Stock} × {product.Name} in stock. " +
                        $"You already have {existing.Quantity} in your cart.");
                }

               
                if (newQty > MaxItemQty)
                {
                    return CartValidationResult.Fail(
                        $"You already have {existing.Quantity} × {product.Name} in your cart. " +
                        $"Maximum per item is {MaxItemQty}.");
                }

                existing.Quantity = newQty;

                _logger.LogInformation(
                    "AddItem merged — user {UserId} updated {Name} qty to {Qty}.",
                    userId, product.Name, existing.Quantity);
            }
            else
            {
                
                if (cart.OrderItems.Count >= MaxCartItems)
                {
                    _logger.LogInformation(
                        "AddItem blocked — user {UserId} already has {Count} distinct items (max {Max}).",
                        userId, cart.OrderItems.Count, MaxCartItems);

                    return CartValidationResult.Fail(
                        $"Your cart already has {MaxCartItems} different items. " +
                        "Please remove something before adding more.");
                }

                cart.OrderItems.Add(new OrderItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = prodQty
                });

                _logger.LogInformation(
                    "AddItem — user {UserId} added {Name} × {Qty} at {Price:C}.",
                    userId, product.Name, prodQty, product.Price);
            }

            SaveCart(cart);
            return CartValidationResult.Ok();
        }

     
        public CartValidationResult UpdateQuantity(int prodId, int prodQty, string userId)
        {
           
            var inputCheck = ValidateUpdateInput(prodId, prodQty);
            if (!inputCheck.IsValid)
                return inputCheck;

            
            var cart = Session.Get<OrderViewModel>(CurrentCartKey());
            if (cart == null || !cart.OrderItems.Any())
            {
                _logger.LogWarning(
                    "UpdateQuantity — cart not found in session for user {UserId}.", userId);
                return CartValidationResult.Fail("Your cart is empty.");
            }

            
            var item = cart.OrderItems.FirstOrDefault(i => i.ProductId == prodId);
            if (item == null)
            {
                _logger.LogWarning(
                    "UpdateQuantity — product {ProdId} not in cart for user {UserId}.",
                    prodId, userId);
                return CartValidationResult.Fail("That item is not in your cart.");
            }

            if (prodQty == 0)
            {
                cart.OrderItems.Remove(item);
                _logger.LogInformation(
                    "UpdateQuantity (qty=0) — removed {Name} from cart for user {UserId}.",
                    item.ProductName, userId);
            }
            else
            {
                _logger.LogInformation(
                    "UpdateQuantity — user {UserId} changed {Name} from {Old} to {New}.",
                    userId, item.ProductName, item.Quantity, prodQty);
                item.Quantity = prodQty;
            }

            SaveCart(cart);
            return CartValidationResult.Ok();
        }

       
        public CartValidationResult RemoveItem(int prodId, string userId)
        {
            if (prodId <= 0)
            {
                _logger.LogWarning(
                    "RemoveItem rejected — invalid prodId {ProdId} for user {UserId}.",
                    prodId, userId);
                return CartValidationResult.Fail("Invalid product.");
            }

            var cart = Session.Get<OrderViewModel>(CurrentCartKey());
            if (cart == null)
            {
                
                _logger.LogDebug(
                    "RemoveItem — no cart in session for user {UserId}. No-op.", userId);
                return CartValidationResult.Ok();
            }

            var item = cart.OrderItems.FirstOrDefault(i => i.ProductId == prodId);
            if (item == null)
            {
                
                _logger.LogDebug(
                    "RemoveItem — product {ProdId} not found in cart for user {UserId}. No-op.",
                    prodId, userId);
                return CartValidationResult.Ok();
            }

            cart.OrderItems.Remove(item);
            SaveCart(cart);

            _logger.LogInformation(
                "RemoveItem — user {UserId} removed {Name} from cart.",
                userId, item.ProductName);

            return CartValidationResult.Ok();
        }

        

        public class PlaceOrderResult
        {
            public bool Success { get; init; }
            public string ErrorMessage { get; init; } = string.Empty;
            public List<string> Warnings { get; init; } = new();
            public int OrderId { get; init; }
        }

       
        public async Task<PlaceOrderResult> PlaceOrderAsync(string userId)
        {
           
            var cart = Session.Get<OrderViewModel>(CurrentCartKey());
            if (cart == null || !cart.OrderItems.Any())
            {
                _logger.LogWarning(
                    "PlaceOrder — cart empty or missing for user {UserId}.", userId);
                return new PlaceOrderResult
                {
                    Success = false,
                    ErrorMessage = "Your cart is empty!"
                };
            }

            var warnings = new List<string>();

            try
            {
                var order = new Order
                {
                    OrderDate = DateTime.Now,
                    UserId = userId,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var cartItem in cart.OrderItems)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);

                    if (product == null)
                    {
                        
                        var msg = $"'{cartItem.ProductName}' is no longer available and was removed.";
                        warnings.Add(msg);
                        _logger.LogWarning(
                            "PlaceOrder — product {ProdId} ({Name}) no longer exists. " +
                            "Skipping for user {UserId}.",
                            cartItem.ProductId, cartItem.ProductName, userId);
                        continue;
                    }

                    
                    if (cartItem.Price != product.Price)
                    {
                        var msg = $"The price of '{product.Name}' changed from " +
                                  $"{cartItem.Price:C} to {product.Price:C}.";
                        warnings.Add(msg);
                        _logger.LogInformation(
                            "PlaceOrder price change — product {ProdId} ({Name}): " +
                            "{OldPrice:C} → {NewPrice:C} for user {UserId}.",
                            product.ProductId, product.Name,
                            cartItem.Price, product.Price, userId);
                    }

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = product.Price   
                    });

                   
                    product.Stock -= cartItem.Quantity;
                    if (product.Stock < 0) product.Stock = 0;
                    _context.Products.Update(product);

                    _logger.LogInformation(
                        "Stock decremented — product {ProdId} ({Name}): -{Qty}, remaining {Stock}.",
                        product.ProductId, product.Name, cartItem.Quantity, product.Stock);
                }

                if (!order.OrderItems.Any())
                {
                    _logger.LogWarning(
                        "PlaceOrder — all items unavailable for user {UserId}. " +
                        "Warnings: {Warnings}",
                        userId, string.Join(" | ", warnings));

                    return new PlaceOrderResult
                    {
                        Success = false,
                        ErrorMessage = "None of the items in your cart are available anymore. " +
                                       string.Join(" ", warnings),
                        Warnings = warnings
                    };
                }

                
                order.TotalAmount = order.OrderItems.Sum(i => i.Price * i.Quantity);

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

               
                ClearCart();

                _logger.LogInformation(
                    "PlaceOrder SUCCESS — order {OrderId} placed for user {UserId}. " +
                    "Total: {Total:C}. Items: {ItemCount}. Warnings: {WarningCount}.",
                    order.OrderId, userId, order.TotalAmount,
                    order.OrderItems.Count, warnings.Count);

                return new PlaceOrderResult
                {
                    Success = true,
                    OrderId = order.OrderId,
                    Warnings = warnings
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "PlaceOrder DB error for user {UserId}.", userId);
                return new PlaceOrderResult
                {
                    Success = false,
                    ErrorMessage = "Something went wrong saving your order. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "PlaceOrder unexpected error for user {UserId}.", userId);
                return new PlaceOrderResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred. Please try again."
                };
            }
        }

        public async Task RestoreStockAsync(Order order, string cancelledBy)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product == null)
                {
                    _logger.LogWarning(
                        "RestoreStock — product {ProdId} not found, skipping. Order {OrderId}.",
                        item.ProductId, order.OrderId);
                    continue;
                }

                var before = product.Stock;
                product.Stock += item.Quantity;
                _context.Products.Update(product);

                _logger.LogInformation(
                    "RestoreStock — product {ProdId} ({Name}): {Before} → {After}. " +
                    "Order {OrderId} cancelled by {CancelledBy}.",
                    product.ProductId, product.Name, before, product.Stock,
                    order.OrderId, cancelledBy);
            }

            await _context.SaveChangesAsync();
        }
    }
}