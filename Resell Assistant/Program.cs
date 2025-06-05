using Microsoft.EntityFrameworkCore;
using Resell_Assistant.Data;
using Resell_Assistant.Services;
using Resell_Assistant.Services.External;
using Resell_Assistant.Models.Configuration;
using Resell_Assistant.Middleware;
using Resell_Assistant.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Add global model validation filter
    options.Filters.Add<ValidateModelStateAttribute>();
});

// Add Entity Framework with DbContextFactory for concurrent operations
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching(true);
    options.EnableThreadSafetyChecks(true);
});

// Also register traditional DbContext for services that need a single instance
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching(true);
    options.EnableThreadSafetyChecks(true);
}, ServiceLifetime.Scoped);

// Register application services
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDealDiscoveryService, DealDiscoveryService>();

// Register credential management services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ICredentialService, CredentialService>();

// Configure eBay API settings
builder.Services.Configure<Resell_Assistant.Models.Configuration.EbayApiSettings>(
    builder.Configuration.GetSection("ApiKeys"));

// Register eBay API service
builder.Services.AddScoped<IEbayApiService, EbayApiService>();

// Facebook Marketplace API service temporarily disabled - do not register
// TODO: Re-enable when proper implementation is complete

// Add CORS policy for API calls
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Add global exception handling middleware (must be first)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Only serve static files in production (React dev server handles this in development)
if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}

app.UseRouting();

// Use CORS
app.UseCors("AllowReactApp");

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Map API controllers explicitly with priority routing
app.MapControllers();

// Only proxy non-API routes to the SPA development server
app.MapWhen(context => !context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseSpa(spa =>
    {
        spa.Options.SourcePath = "ClientApp";
        
        if (app.Environment.IsDevelopment())
        {
            spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
        }
    });
});

// Fallback for non-API routes when not in development
if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
