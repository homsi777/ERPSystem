using ERPSystem.Api.Auth;
using ERPSystem.Api.Endpoints;
using ERPSystem.Api.Services;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<ICurrentBranchService, HttpContextBranchService>();
builder.Services.AddSingleton<SalesInvoicePdfService>();
builder.Services.AddSingleton<ExpenseReportPdfService>();
builder.Services.AddSingleton<ReceiptVoucherPdfService>();
builder.Services.AddSingleton<PaymentVoucherPdfService>();
builder.Services.AddSingleton<CustomerAccountLedgerPdfService>();
builder.Services.AddSingleton<JournalEntryPdfService>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        var cors = policy.AllowAnyHeader().AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
        {
            // Allow any device on the local network (IP may change between Wi-Fi sessions).
            cors.SetIsOriginAllowed(static origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.Host is "localhost" or "127.0.0.1"
                    || uri.Host.StartsWith("192.168.", StringComparison.Ordinal)
                    || uri.Host.StartsWith("10.", StringComparison.Ordinal);
            });
        }
        else
        {
            // Production origins are supplied via configuration/environment so the
            // deployed domain can be added without recompiling.
            // Example (env): Cors__AllowedOrigins="https://alamal-ab.org,https://www.alamal-ab.org"
            string[] defaults =
            [
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174"
            ];

            var configured = builder.Configuration["Cors:AllowedOrigins"]
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            cors.WithOrigins([.. defaults, .. configured]);
        }
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddResponseCompression();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ERPSystem.Infrastructure.Persistence.ErpDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Apply any pending EF Core migrations on startup so a fresh/cloud database
    // is provisioned automatically without a separate migration step.
    logger.LogInformation("Applying database migrations...");
    await context.Database.MigrateAsync();

    // Full idempotent seed (company, branch, admin user, roles, permissions).
    // Safe to run on every startup; it no-ops once the database is seeded.
    await DatabaseSeeder.SeedAsync(context, logger, passwordHasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseSessionValidation();
app.UseAuthorization();

app.MapGet("/health", () => "OK").AllowAnonymous();
app.MapAuthEndpoints();
app.MapInventoryEndpoints();
app.MapCustomerEndpoints();
app.MapLookupEndpoints();
app.MapContainerEndpoints();
app.MapDetailingEndpoints();
app.MapDashboardEndpoints();
app.MapReceiptEndpoints();
app.MapPaymentVoucherEndpoints();
app.MapPurchaseInvoiceEndpoints();
app.MapFinanceEndpoints();
app.MapSalesEndpoints();
app.MapExpenseEndpoints();
app.MapAccountingEndpoints();
app.MapSettingsEndpoints();

app.Run();
