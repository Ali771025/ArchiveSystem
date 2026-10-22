using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Security.Claims;
using ArchiveSystem.Application.Common.Models.AdvancedSearch;
using ArchiveSystem.Application.Common.Models.MovementLogs;
using ArchiveSystem.Application.Features.Owners.Models;
using ArchiveSystem.Application;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Infrastructure;
using ArchiveSystem.Infrastructure.Integrations.MeliPayamak;
using ArchiveSystem.Infrastructure.Services.Exports;
using ArchiveSystem.Presentation.Api.Controllers;
using ArchiveSystem.Presentation.Api.Authorization;
using ArchiveSystem.Presentation.Api.HealthChecks;
using ArchiveSystem.Presentation.Api.Middleware;
using ArchiveSystem.Presentation.Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ArchiveSystem.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning);

    if (context.HostingEnvironment.IsDevelopment())
    {
        loggerConfiguration.WriteTo.Console();
        return;
    }

    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found for Serilog.");

    loggerConfiguration
        .WriteTo.Logger(lc => lc
            .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("LogCategory"))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: BuildSqlSinkOptions("Logs_System"),
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: BuildSystemColumnOptions()))
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(logEvent =>
                logEvent.Properties.TryGetValue("LogCategory", out var value) &&
                value.ToString().Contains("Security", StringComparison.OrdinalIgnoreCase))
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: BuildSqlSinkOptions("Logs_Security"),
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: BuildSecurityColumnOptions()));
});

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                                ?? context.HttpContext.TraceIdentifier;

            var payload = new ApiErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed.",
                CorrelationId = correlationId,
                Errors = errors
            };

            return new BadRequestObjectResult(payload);
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        var retryAfterSeconds = 120;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiResult<object>
            {
                Success = false,
                ErrorCode = "OTP_RATE_LIMITED",
                Message = $"لطفاً {retryAfterSeconds} ثانیه صبر کنید و سپس دوباره تلاش کنید.",
                Data = new
                {
                    RetryAfterSeconds = retryAfterSeconds,
                    NextAllowedAtUtc = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds)
                }
            },
            context.HttpContext.RequestAborted);
    };

    options.AddFixedWindowLimiter("otp-request", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(2);
        limiter.PermitLimit = 1;
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("otp-verify", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 5;
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.Configure<MeliPayamakSettings>(builder.Configuration.GetSection("MeliPayamak"));
builder.Services.AddOptions<BehsazSettings>()
    .Bind(builder.Configuration.GetSection(BehsazSettings.SectionName))
    .PostConfigure(options =>
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            options.ConnectionString = builder.Configuration.GetConnectionString("BehsazConnection") ?? string.Empty;
        }
    });
builder.Services.AddHttpClient("MeliPayamakFastSendClient", httpClient =>
{
    httpClient.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Archive System API",
        Version = "v1",
        Description = "On-prem archive management API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "http://127.0.0.1:5173",
                  "http://localhost:3000",
                  "http://localhost:3001",
                  "http://localhost:8088")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    const string securityStampClaimType = "security_stamp";
    var idleTimeoutMinutes = builder.Configuration.GetValue("Jwt:IdleTimeoutMinutes", 20);
    if (idleTimeoutMinutes <= 0)
    {
        idleTimeoutMinutes = 20;
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? string.Empty))
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenSecurityStamp = context.Principal?.FindFirstValue(securityStampClaimType);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
            {
                context.Fail("Invalid session.");
                return;
            }

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive)
            {
                context.Fail("Invalid session.");
                return;
            }

            var currentSecurityStamp = await userManager.GetSecurityStampAsync(user);
            if (!string.Equals(tokenSecurityStamp, currentSecurityStamp, StringComparison.Ordinal))
            {
                context.Fail("Session has been superseded.");
                return;
            }

            var sessionCache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
            var sessionCacheKey = $"auth:session:{user.Id}:{tokenSecurityStamp}";
            var activeSession = await sessionCache.GetStringAsync(sessionCacheKey);
            if (string.IsNullOrWhiteSpace(activeSession))
            {
                context.Fail("Session has expired due to inactivity.");
                return;
            }

            await sessionCache.SetStringAsync(
                sessionCacheKey,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(idleTimeoutMinutes)
                });
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CaseReadPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("CaseWritePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services
    .AddHealthChecks()
    .AddCheck<InfrastructureHealthCheck>(
        "infrastructure",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

builder.Services.AddApplication();
builder.Services.AddScoped<IValidator<CreateOwnerDto>, CreateOwnerDtoValidator>();
builder.Services.AddScoped<IValidator<CreateMovementLogDto>, CreateMovementLogDtoValidator>();
builder.Services.AddScoped<IValidator<AdvancedSearchQueryDto>, AdvancedSearchQueryDtoValidator>();
var messagingEnabled = builder.Configuration.GetValue<bool?>("Messaging:Enabled") ?? !builder.Environment.IsDevelopment();
builder.Services.AddInfrastructure(builder.Configuration, includeMessaging: messagingEnabled);
builder.Services.AddHostedService<ExportJobBackgroundService>();

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Migration:ApplyOnStartup", true);
var migrateOnly = builder.Configuration.GetValue("Migration:MigrateOnly", false);
var failOnMigrationError = builder.Configuration.GetValue<bool?>("Migration:FailOnError") ?? !app.Environment.IsDevelopment();

if (applyMigrationsOnStartup || migrateOnly)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("ArchiveSystem.Migrations");

    try
    {
        logger.LogInformation(
            "Applying database migrations (ApplyOnStartup={ApplyOnStartup}, MigrateOnly={MigrateOnly})",
            applyMigrationsOnStartup,
            migrateOnly);

        var databaseStartup = scope.ServiceProvider.GetRequiredService<IDatabaseStartupService>();
        await databaseStartup.ApplyMigrationsAsync();

        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed.");
        if (migrateOnly || failOnMigrationError)
        {
            throw;
        }

        logger.LogWarning("Continuing startup without applying migrations because FailOnError is disabled.");
    }
}

if (!migrateOnly)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("ArchiveSystem.SchemaCompatibility");

    try
    {
        var databaseStartup = scope.ServiceProvider.GetRequiredService<IDatabaseStartupService>();
        await databaseStartup.EnsureSchemaCompatibilityAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Schema compatibility check failed.");
        if (failOnMigrationError)
        {
            throw;
        }

        logger.LogWarning("Continuing startup because Migration:FailOnError is disabled.");
    }
}

if (!migrateOnly)
{
    using var scope = app.Services.CreateScope();
    var scopedServices = scope.ServiceProvider;
    var startupLogger = scopedServices.GetRequiredService<ILoggerFactory>().CreateLogger("ArchiveSystem.Startup");
    var startupConfiguration = scopedServices.GetRequiredService<IConfiguration>();
    var failOnRoleBootstrapError = startupConfiguration.GetValue("BootstrapRoles:FailOnError", false);
    var failOnBootstrapError = startupConfiguration.GetValue("BootstrapAdmin:FailOnError", false);

    try
    {
        await EnsureIdentitySystemRolesAsync(scopedServices, startupLogger);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to bootstrap identity roles.");
        if (failOnRoleBootstrapError)
        {
            throw;
        }

        startupLogger.LogWarning("Continuing startup because BootstrapRoles:FailOnError is disabled.");
    }

    try
    {
        await EnsureBootstrapAdminUserAsync(scopedServices, startupConfiguration, startupLogger);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to bootstrap admin user.");
        if (failOnBootstrapError)
        {
            throw;
        }

        startupLogger.LogWarning("Continuing startup because BootstrapAdmin:FailOnError is disabled.");
    }
}

try
{
    var startupMarkerPath = Path.Combine(Path.GetTempPath(), "archive-api", "started");
    var startupMarkerDirectory = Path.GetDirectoryName(startupMarkerPath);
    if (!string.IsNullOrWhiteSpace(startupMarkerDirectory))
    {
        Directory.CreateDirectory(startupMarkerDirectory);
    }

    await File.WriteAllTextAsync(startupMarkerPath, DateTimeOffset.UtcNow.ToString("O"));
}
catch (Exception ex)
{
    app.Logger.LogDebug(ex, "Unable to write startup marker file.");
}

if (migrateOnly)
{
    return;
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = static (_, elapsed, ex) =>
        ex != null || elapsed > 1000 ? LogEventLevel.Warning : LogEventLevel.Information;
    options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        diagnosticContext.Set("UserId",
            httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous");
    };
});
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
});

app.MapControllers();

app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            durationMs = entry.Value.Duration.TotalMilliseconds
        }),
        durationMs = report.TotalDuration.TotalMilliseconds
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

static MSSqlServerSinkOptions BuildSqlSinkOptions(string tableName)
{
    return new MSSqlServerSinkOptions
    {
        TableName = tableName,
        AutoCreateSqlTable = true,
        SchemaName = "dbo"
    };
}

static ColumnOptions BuildSystemColumnOptions()
{
    var columnOptions = new ColumnOptions();
    columnOptions.Store.Remove(StandardColumn.Properties);
    columnOptions.Store.Add(StandardColumn.LogEvent);

    columnOptions.AdditionalColumns = new Collection<SqlColumn>
    {
        new() { ColumnName = "UserId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 64, AllowNull = true },
        new() { ColumnName = "ClientIpAddress", DataType = System.Data.SqlDbType.NVarChar, DataLength = 64, AllowNull = true },
        new() { ColumnName = "MachineName", DataType = System.Data.SqlDbType.NVarChar, DataLength = 128, AllowNull = true }
    };

    return columnOptions;
}

static ColumnOptions BuildSecurityColumnOptions()
{
    var columnOptions = BuildSystemColumnOptions();
    columnOptions.AdditionalColumns.Add(
        new SqlColumn { ColumnName = "LogCategory", DataType = System.Data.SqlDbType.NVarChar, DataLength = 32, AllowNull = true });
    columnOptions.AdditionalColumns.Add(
        new SqlColumn { ColumnName = "EventType", DataType = System.Data.SqlDbType.NVarChar, DataLength = 120, AllowNull = true });
    columnOptions.AdditionalColumns.Add(
        new SqlColumn { ColumnName = "ActorUserId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 64, AllowNull = true });
    columnOptions.AdditionalColumns.Add(
        new SqlColumn { ColumnName = "TargetUserId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 64, AllowNull = true });
    columnOptions.AdditionalColumns.Add(
        new SqlColumn { ColumnName = "SecurityMetadata", DataType = System.Data.SqlDbType.NVarChar, DataLength = -1, AllowNull = true });
    return columnOptions;
}

static async Task EnsureBootstrapAdminUserAsync(
    IServiceProvider services,
    IConfiguration configuration,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var enabled = configuration.GetValue("BootstrapAdmin:Enabled", true);
    if (!enabled)
    {
        logger.LogInformation("Bootstrap admin provisioning is disabled.");
        return;
    }

    var username = configuration["BootstrapAdmin:Username"]?.Trim();
    var password = configuration["BootstrapAdmin:Password"];
    var email = configuration["BootstrapAdmin:Email"]?.Trim();

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogInformation("Bootstrap admin credentials are not configured; skipping admin provisioning.");
        return;
    }

    var userManager = services.GetRequiredService<UserManager<ArchiveSystem.Infrastructure.Identity.ApplicationUser>>();

    var user = await userManager.FindByNameAsync(username);
    if (user is null)
    {
        var candidateEmail = string.IsNullOrWhiteSpace(email)
            ? $"{username.ToLowerInvariant()}@local.archive"
            : email;

        user = new ArchiveSystem.Infrastructure.Identity.ApplicationUser
        {
            UserName = username,
            Email = candidateEmail,
            FirstName = "System",
            LastName = "Administrator",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            LockoutEnabled = true
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to create bootstrap admin user: {errors}");
        }

        logger.LogInformation("Bootstrap admin user '{Username}' was created.", username);
    }

    if (!user.IsActive)
    {
        user.IsActive = true;
        user.ModifiedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to activate bootstrap admin user: {errors}");
        }
    }

    if (!await userManager.GetLockoutEnabledAsync(user))
    {
        var lockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutResult.Succeeded)
        {
            var errors = string.Join("; ", lockoutResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to enable lockout for bootstrap admin user: {errors}");
        }
    }

    var roles = await userManager.GetRolesAsync(user);
    if (!roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
    {
        var addRoleResult = await userManager.AddToRoleAsync(user, "Admin");
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addRoleResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to assign Admin role to bootstrap user: {errors}");
        }
    }

    logger.LogInformation("Bootstrap admin provisioning completed for '{Username}'.", username);
}

static async Task EnsureIdentitySystemRolesAsync(
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
    var requiredRoles = new[] { "Admin", "Viewer" };

    foreach (var requiredRole in requiredRoles)
    {
        if (await roleManager.RoleExistsAsync(requiredRole))
        {
            continue;
        }

        var roleCreateResult = await roleManager.CreateAsync(new IdentityRole<long>(requiredRole));
        if (!roleCreateResult.Succeeded)
        {
            var errors = string.Join("; ", roleCreateResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to create {requiredRole} role: {errors}");
        }
    }

    logger.LogInformation("Identity role bootstrap completed.");
}
