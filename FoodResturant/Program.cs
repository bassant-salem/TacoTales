using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodResturant.Data;
using FoodResturant.Models;
var builder = WebApplication.CreateBuilder(args);
//  Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
// Combined Identity registration 
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
// Cart service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FoodResturant.Services.CartService>();

// Register HttpContextAccessor and CartService for session-based cart management
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FoodResturant.Services.CartService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
// Middleware order Session -> Auth -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
//  Seed the database
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}
app.Run();