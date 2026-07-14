using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Api.Middleware;
using Payment.Application;
using Payment.Infrastructure;
using Serilog;

// Bootstrap logger used before the DI container is built
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog integrated with the host for structured logging throughout the app
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // JWT Bearer authentication with symmetric key from configuration
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtConfig = builder.Configuration.GetSection("Jwt");
            var secret = jwtConfig["Secret"]
                ?? throw new InvalidOperationException("JWT:Secret is required");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtConfig["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // CORS policy with origins from configuration (defaults to localhost:3000)
    builder.Services.AddCors(options =>
    {
        var corsConfig = builder.Configuration.GetSection("Cors:AllowedOrigins");
        var origins = corsConfig.Get<string[]>() ?? ["http://localhost:3000"];

        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // Rate limiting: per-user token bucket ("UserPayment") and fixed-window ("Strict")
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;

        options.AddPolicy<string, Payment.Api.RateLimiting.UserRateLimiter>("UserPayment");

        var strictConfig = builder.Configuration.GetSection("RateLimiting:Strict");
        options.AddFixedWindowLimiter("Strict", opt =>
        {
            opt.PermitLimit = strictConfig.GetValue<int>("PermitLimit", 3);
            opt.Window = TimeSpan.FromMinutes(strictConfig.GetValue<int>("WindowInMinutes", 1));
            opt.QueueLimit = strictConfig.GetValue<int>("QueueLimit", 0);
        });
    });

    // Register application (CQRS/Use-Cases) and infrastructure (DB, messaging) services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // 1 MB request size limit applied to all controllers
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(1_048_576));
    });

    var app = builder.Build();

    // -- Middleware pipeline (order matters) --

    app.UseSerilogRequestLogging();            // 1. Request logging
    app.UseMiddleware<CorrelationIdMiddleware>(); // 2. Correlation ID propagation
    app.UseMiddleware<ExceptionHandlingMiddleware>(); // 3. Global exception handler
    app.UseMiddleware<OriginValidationMiddleware>(); // 4. CSRF origin validation

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();                            // 5. CORS headers
    app.UseRateLimiter();                     // 6. Rate limiting
    app.UseAuthentication();                  // 7. JWT authentication
    app.UseMiddleware<JwtUserMiddleware>();   // 8. Extract user ID into HttpContext
    app.UseAuthorization();                   // 9. Authorization policies
    app.MapControllers();                     // 10. Map endpoint routes

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
