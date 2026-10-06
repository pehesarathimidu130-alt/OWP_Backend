using Backend.Data;
using Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Allow DateTime with Kind=Unspecified to be written to PostgreSQL timestamp columns.
// Without this, form-submitted dates (which ASP.NET parses as Unspecified) cause a runtime error.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

// Configure Entity Framework and PostgreSQL
// Force IPv4 to avoid SSL handshake failures on IPv6 with Neon's pooler.
// Channel Binding is intentionally omitted — Neon's PgBouncer pooler does not support it.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=oleena;Username=postgres;Password=postgres";

var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);

// Neon Serverless Resiliency Settings:
// 1. Neon cold-starts can take 2-5 seconds to wake up compute. Allow up to 60s for handshake.
if (dataSourceBuilder.ConnectionStringBuilder.Timeout < 60)
{
    dataSourceBuilder.ConnectionStringBuilder.Timeout = 60;
}
if (dataSourceBuilder.ConnectionStringBuilder.CommandTimeout < 60)
{
    dataSourceBuilder.ConnectionStringBuilder.CommandTimeout = 60;
}
// 2. Keep connections alive so proxies (Neon / AWS / ISP routers) do not terminate idle TCP streams
dataSourceBuilder.ConnectionStringBuilder.KeepAlive = 30;
dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 60;
dataSourceBuilder.ConnectionStringBuilder.ConnectionPruningInterval = 10;
dataSourceBuilder.ConnectionStringBuilder.NoResetOnClose = true;

// 3. Channel Binding: Neon's PgBouncer pooler (-pooler) does not support it at all.
// Explicitly disable to prevent SSL handshake failures.
dataSourceBuilder.ConnectionStringBuilder.ChannelBinding = Npgsql.ChannelBinding.Disable;

// Build a shared data source so all EF connections reuse the same Npgsql pool
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

// Configure Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register Application Services (Combined from dev and feature branch)
builder.Services.AddScoped<Backend.Services.IAuthService, Backend.Services.AuthService>();
builder.Services.AddScoped<Backend.Services.IAdminManagementService, Backend.Services.AdminManagementService>();
builder.Services.AddScoped<Backend.Services.IVendorContentService, Backend.Services.VendorContentService>();
builder.Services.AddScoped<Backend.Services.IVendorProfileService, Backend.Services.VendorProfileService>();
builder.Services.AddScoped<Backend.Services.INotificationService, Backend.Services.NotificationService>();
builder.Services.AddScoped<Backend.Services.IAnalyticsService, Backend.Services.AnalyticsService>();
builder.Services.AddScoped<Backend.Services.ICustomerManagementService, Backend.Services.CustomerManagementService>();
builder.Services.AddScoped<Backend.Services.IVendorRegistrationService, Backend.Services.VendorRegistrationService>();
builder.Services.AddScoped<Backend.Services.IGoogleTokenVerifier, Backend.Services.GoogleTokenVerifier>();
builder.Services.AddScoped<Backend.Services.IReportAnalyticsService, Backend.Services.ReportAnalyticsService>();
builder.Services.AddScoped<Backend.Services.ICustomerAuthService, Backend.Services.CustomerAuthService>();
builder.Services.AddScoped<Backend.Services.IEmailService, Backend.Services.EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Backend.Services.IActivityLogService, Backend.Services.ActivityLogService>();
builder.Services.AddScoped<Backend.Services.IAnalyticsService, Backend.Services.AnalyticsService>();
builder.Services.AddScoped<Backend.Services.IVendorPerformanceService, Backend.Services.VendorPerformanceService>();
builder.Services.AddScoped<Backend.Services.IVendorRatingService, Backend.Services.VendorRatingService>();

// Configure File Storage (Local or Supabase)
builder.Services.AddHttpClient();
var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "Local";
if (storageProvider.Equals("Supabase", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<Backend.Services.IFileStorage, Backend.Services.SupabaseFileStorage>();
}
else
{
    builder.Services.AddScoped<Backend.Services.IFileStorage, Backend.Services.LocalFileStorage>();
}

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? "PlaceholderSecretKeyForDevelopmentNeedsToBeLongerThan32Chars!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

// Configure Role-based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("VendorOnly", policy => policy.RequireRole("Vendor"));
});

// Configure Swagger with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Oleena API", Version = "v1" });
    
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174",
                "http://localhost:5175",
                "http://127.0.0.1:5175",
                "https://owp-wedding-planner.netlify.app/",
                "http://localhost:3000")
              .SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oleena Wedding API v1");
    c.RoutePrefix = "swagger"; // Serves Swagger UI at /swagger/index.html
});

app.UseCors("AllowReactApp");

app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

// Seed/repair initial authentication data in background without blocking server startup
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2500);
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbInitializer.SeedAsync(dbContext, logger);
    }
    catch (ObjectDisposedException)
    {
        // Host shut down before/during seeding; ignore cleanly.
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DbInitializer Background Error]: {ex.Message}");
    }
});

app.Run();
