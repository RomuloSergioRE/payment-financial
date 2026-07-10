using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Api.Middleware;
using Payment.Application;
using Payment.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<Payment.Infrastructure.Persistence.PaymentDbContext>((sp, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("PaymentDatabase");
        options.UseNpgsql(connectionString);
    });

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

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;

        var paymentConfig = builder.Configuration.GetSection("RateLimiting:Payment");
        options.AddFixedWindowLimiter("Payment", opt =>
        {
            opt.PermitLimit = paymentConfig.GetValue<int>("PermitLimit", 10);
            opt.Window = TimeSpan.FromMinutes(paymentConfig.GetValue<int>("WindowInMinutes", 1));
            opt.QueueLimit = paymentConfig.GetValue<int>("QueueLimit", 2);
        });

        var strictConfig = builder.Configuration.GetSection("RateLimiting:Strict");
        options.AddFixedWindowLimiter("Strict", opt =>
        {
            opt.PermitLimit = strictConfig.GetValue<int>("PermitLimit", 3);
            opt.Window = TimeSpan.FromMinutes(strictConfig.GetValue<int>("WindowInMinutes", 1));
            opt.QueueLimit = strictConfig.GetValue<int>("QueueLimit", 0);
        });
    });

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<JwtUserMiddleware>();
    app.UseAuthorization();
    app.MapControllers();

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
