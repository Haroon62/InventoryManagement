using InventoryManagement.Data;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register the DbContext with Dependency Injection.
// This tells ASP.NET Core: "When any class asks for ApplicationDbContext
// in its constructor, create one connected to SQL Server using this connection string."
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services.
// AddScoped means: one instance per HTTP request.
// When a Controller asks for IProductService, DI creates a ProductService.
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();

// Register MVC services: controllers, views, model binding, validation
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serves files from wwwroot (CSS, JS, images, etc.)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Default MVC route pattern:
// URL:  /Categories/Edit/5
// Maps: CategoriesController → Edit action → id = 5
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
