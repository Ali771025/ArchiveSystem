// src/ArchiveSystem.Infrastructure/DependencyInjection.cs (Updated)
using Amazon.Runtime;
using Amazon.S3;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Authentication.Otp;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using CommonSecurity = ArchiveSystem.Application.Common.Interfaces.Security;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.MovementLogs.Interfaces;
using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Behsaz.Interfaces;
using ArchiveSystem.Application.Features.Cases.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Auditing;
using ArchiveSystem.Infrastructure.Identity.Otp;
using ArchiveSystem.Infrastructure.Integrations.MeliPayamak;
using ArchiveSystem.Infrastructure.Logging;
using ArchiveSystem.Infrastructure.Repositories;
using ArchiveSystem.Infrastructure.Security;
using ArchiveSystem.Infrastructure.Services;
using ArchiveSystem.Infrastructure.Services.Behsaz;
using ArchiveSystem.Infrastructure.Services.Authentication;
using ArchiveSystem.Infrastructure.Services.Exports;
using ArchiveSystem.Infrastructure.Messaging;
using DomainFileRepository = ArchiveSystem.Domain.Repositories.IFileRepository;
using AppFileRepository = ArchiveSystem.Application.Interfaces.Persistence.IFileRepository;
using Polly;
using Polly.Extensions.Http;

namespace ArchiveSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool includeMessaging = true)
        {
            AuditTrailConfigurator.Configure(configuration);

            // Database configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            services.AddDbContext<ArchiveDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    sqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
                }));
            services.AddScoped<ArchiveSystem.Application.Interfaces.Persistence.IArchiveDbContext>(provider =>
                provider.GetRequiredService<ArchiveDbContext>());
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICaseNumberGenerator, SqlCaseNumberGenerator>();

            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 8;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole<long>>()
                .AddSignInManager<SignInManager<ApplicationUser>>()
                .AddEntityFrameworkStores<ArchiveDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<NumericOtpTokenProvider>(NumericOtpTokenProviderOptions.ProviderName);

            services.Configure<NumericOtpTokenProviderOptions>(options =>
            {
                options.Name = NumericOtpTokenProviderOptions.ProviderName;
                options.TokenLifespan = TimeSpan.FromMinutes(2);
                options.Digits = 5;
            });

            services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));

            // Add HTTP Context Accessor for audit logging
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = "archive-system:";
                });
            }

            services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
            services.Configure<StorageProviderOptions>(configuration.GetSection(StorageProviderOptions.SectionName));
            services.Configure<SqlFileStreamOptions>(configuration.GetSection(SqlFileStreamOptions.SectionName));
            services.AddOptions<MeliPayamakOptions>()
                .Bind(configuration.GetSection(MeliPayamakOptions.SectionName))
                .PostConfigure(options =>
                {
                    var legacySection = configuration.GetSection("MeliPayamak");

                    options.BaseUrl = FirstConfigured(options.BaseUrl, legacySection["BaseUrl"], "https://rest.payamak-panel.com/");
                    options.PatternEndpoint = FirstConfigured(options.PatternEndpoint, legacySection["PatternEndpoint"], "api/SendSMS/BaseServiceNumber");
                    options.Username = FirstConfigured(options.Username, legacySection["Username"]);
                    options.Password = FirstConfigured(options.Password, legacySection["Password"]);
                    options.From = FirstConfigured(
                        configuration["MELIPAYAMAK_FROM"],
                        options.From,
                        legacySection["From"],
                        legacySection["SenderNumber"]);
                    options.LoginOtpPatternCode = FirstConfigured(options.LoginOtpPatternCode, legacySection["LoginOtpPatternCode"], legacySection["LoginPatternCode"]);
                    options.TimeoutSeconds = Math.Max(1, options.TimeoutSeconds);
                    options.RetryCount = Math.Max(0, options.RetryCount);
                    options.CircuitBreakDurationSeconds = Math.Max(5, options.CircuitBreakDurationSeconds);
                    options.CircuitBreakFailureThreshold = Math.Max(2, options.CircuitBreakFailureThreshold);
                });

            // Repositories with audit integration
            services.AddScoped<DomainFileRepository>(provider =>
            {
                var context = provider.GetRequiredService<ArchiveDbContext>();
                var config = provider.GetRequiredService<IConfiguration>();
                var auditService = provider.GetRequiredService<IAuditService>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var logger = provider.GetRequiredService<ILogger<SqlFileRepository>>();
                
                return new SqlFileRepository(context, config, auditService, httpContextAccessor, logger);
            });

            services.AddScoped<ICaseRepository>(provider =>
            {
                var context = provider.GetRequiredService<ArchiveDbContext>();
                var auditService = provider.GetRequiredService<IAuditService>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var logger = provider.GetRequiredService<ILogger<SqlCaseRepository>>();
                
                return new SqlCaseRepository(context, auditService, httpContextAccessor, logger);
            });

            services.AddScoped<IClassificationRepository, SqlClassificationRepository>();
            services.AddScoped<IAuditLogRepository, SqlAuditLogRepository>();
            services.AddScoped<AppFileRepository, EfFileRepository>();
            services.AddScoped<IFileVersionRepository, EfFileVersionRepository>();
            services.AddScoped<IUploadSessionRepository, EfUploadSessionRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<IDynamicTemplateRepository, SqlDynamicTemplateRepository>();
            services.AddScoped<ICaseDynamicFieldValueRepository, SqlCaseDynamicFieldValueRepository>();
            services.AddScoped<IDossierAdvancedSearchRepository, EfDossierAdvancedSearchRepository>();
            services.AddScoped<IFileSearchRepository, EfFileSearchRepository>();
            services.AddScoped<ICaseLoanRepository, EfCaseLoanRepository>();
            services.AddScoped<ITaxpayerRepository, EfTaxpayerRepository>();
            services.AddScoped<IRoleRepository, EfRoleRepository>();
            services.AddScoped<IPermissionRepository, EfPermissionRepository>();
            services.AddScoped<IUserRoleRepository, EfUserRoleRepository>();
            services.AddScoped<IClassificationAclRepository, EfClassificationAclRepository>();
            services.AddScoped<ITagAclRepository, EfTagAclRepository>();
            services.AddScoped<CommonRepositories.IRoleRepository>(provider =>
                (CommonRepositories.IRoleRepository)provider.GetRequiredService<IRoleRepository>());
            services.AddScoped<CommonRepositories.IPermissionRepository>(provider =>
                (CommonRepositories.IPermissionRepository)provider.GetRequiredService<IPermissionRepository>());
            services.AddScoped<CommonRepositories.IUserRoleRepository>(provider =>
                (CommonRepositories.IUserRoleRepository)provider.GetRequiredService<IUserRoleRepository>());
            services.AddScoped<CommonRepositories.IClassificationAclRepository>(provider =>
                (CommonRepositories.IClassificationAclRepository)provider.GetRequiredService<IClassificationAclRepository>());
            services.AddScoped<CommonRepositories.ITagAclRepository>(provider =>
                (CommonRepositories.ITagAclRepository)provider.GetRequiredService<ITagAclRepository>());
            services.AddScoped<IFileTagRepository, EfFileTagRepository>();
            services.AddScoped<IAccessControlLookupRepository, EfAccessControlLookupRepository>();
            services.AddScoped<ITagRepository, EfTagRepository>();
            services.AddScoped<IMovementLogRepository, EfMovementLogRepository>();
            services.AddScoped<IPhysicalMovementLogRepository, EfPhysicalMovementLogRepository>();
            services.AddScoped<IOwnerRepository, EfOwnerRepository>();
            services.AddScoped<IReportLogRepository, EfReportLogRepository>();
            services.AddScoped<IExportJobRepository, EfExportJobRepository>();
            services.AddScoped<IFileExportQueryRepository, EfFileExportQueryRepository>();
            services.AddSingleton<IAuthorizationCache, AuthorizationMemoryCache>();
            services.AddScoped<IAccessControlService, AccessControlService>();
            services.AddScoped<IAccessAdministrationService, AccessAdministrationService>();
            services.AddScoped<CommonSecurity.IAccessControlService>(provider =>
                provider.GetRequiredService<IAccessControlService>());

            // Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IDatabaseStartupService, DatabaseStartupService>();
            services.AddScoped<ArchiveSystem.Application.Features.UserManagement.Interfaces.IUserManagementService, UserManagementService>();
            services.AddScoped<ArchiveSystem.Application.Common.Interfaces.Security.IIdentityService, IdentityService>();
            services.AddScoped<ArchiveSystem.Application.Common.Interfaces.Security.IIdentityManagementService, IdentityManagementService>();
            services.AddScoped<ArchiveSystem.Application.Common.Interfaces.Security.ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<ArchiveSystem.Application.Features.AccessControl.Interfaces.IIdentityRoleService, IdentityRoleService>();
            services.AddScoped<IDynamicFieldService, DynamicFieldService>();
            services.AddSingleton<IBehsazIntegrationService, BehsazIntegrationService>();
            services.AddScoped<IBehsazSearchRepository, BehsazSearchRepository>();
            services.AddScoped<IBehsazRepository, BehsazRepository>();
            services.AddScoped<IAuditWriter, MovementAuditWriter>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IActivityExcelExporter, ActivityExcelExporter>();
            services.AddScoped<ArchiveSystem.Application.Features.Cases.Queries.ExportCaseWorkbook.ICaseWorkbookExporter, CaseWorkbookExporter>();
            services.AddScoped<IFilesZipExporter, FilesZipExporter>();
            services.AddScoped<IExportJobExecutionService, ExportJobExecutionService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<ICaseAvatarService, CaseAvatarService>();
            services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
            services.AddScoped<ILoginOtpUserService, LoginOtpIdentityService>();
            services.AddScoped<IAuthSlidingWindowRateLimiter, RedisSlidingWindowRateLimiter>();
            services.AddScoped<ILoginOtpOutboxService, LoginOtpOutboxService>();
            services.AddScoped<IPhoneOtpIdentityService, PhoneOtpIdentityService>();
            services.AddScoped<IRefreshTokenIssuer, DistributedCacheRefreshTokenIssuer>();
            services.AddScoped<ISmsSender, MeliPayamakSmsService>();

            services.AddHttpClient<IMeliPayamakService, MeliPayamakService>((serviceProvider, httpClient) =>
            {
                var meliPayamakOptions = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MeliPayamakOptions>>()
                    .Value;

                httpClient.BaseAddress = new Uri(meliPayamakOptions.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, meliPayamakOptions.TimeoutSeconds));
            })
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var meliPayamakOptions = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MeliPayamakOptions>>()
                    .Value;

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(message => (int)message.StatusCode == 429)
                    .WaitAndRetryAsync(
                        retryCount: Math.Max(1, meliPayamakOptions.RetryCount),
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            })
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var meliPayamakOptions = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<MeliPayamakOptions>>()
                    .Value;

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: Math.Max(2, meliPayamakOptions.CircuitBreakFailureThreshold),
                        durationOfBreak: TimeSpan.FromSeconds(Math.Max(5, meliPayamakOptions.CircuitBreakDurationSeconds)));
            });
            // Storage service configuration
            var storageProvider = configuration.GetValue<string>($"{StorageProviderOptions.SectionName}:Provider") ?? "MinIO";
            var minioSection = configuration.GetSection("MinIO");
            var minioEndpoint = minioSection["Endpoint"] ?? "http://localhost:9000";
            var accessKey = minioSection["AccessKey"];
            var secretKey = minioSection["SecretKey"];
            var bucketName = minioSection["BucketName"] ?? "archive-system";
            var regionName = minioSection["Region"];
            var useSsl = bool.TryParse(minioSection["UseSSL"], out var parsedUseSsl) && parsedUseSsl;
            var forcePathStyle = !bool.TryParse(minioSection["ForcePathStyle"], out var parsedForcePathStyle) || parsedForcePathStyle;

            services.Configure<ObjectStorageOptions>(options =>
            {
                options.BucketName = bucketName;
            });

            if (string.Equals(storageProvider, "SqlFileStream", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<SqlFileStreamObjectStorage>();
                services.AddSingleton<IObjectStorage>(provider => provider.GetRequiredService<SqlFileStreamObjectStorage>());
                services.AddSingleton<IFileContentStore>(provider => provider.GetRequiredService<SqlFileStreamObjectStorage>());
                services.AddScoped<IStorageService, SqlFileStreamStorageService>();
            }
            else
            {
                var awsCredentials = new BasicAWSCredentials(accessKey, secretKey);
                var normalizedRegionName = string.IsNullOrWhiteSpace(regionName)
                    ? "us-east-1"
                    : regionName.Trim();

                services.AddSingleton<IAmazonS3>(provider =>
                {
                    var serviceUrl = NormalizeServiceUrl(minioEndpoint, useSsl);

                    var config = new AmazonS3Config
                    {
                        ServiceURL = serviceUrl,
                        ForcePathStyle = forcePathStyle,
                        UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    };

                    // RegionEndpoint and ServiceURL must not be set together.
                    // For custom S3-compatible endpoints (MinIO, etc.) keep ServiceURL and set auth region explicitly.
                    config.AuthenticationRegion = normalizedRegionName;

                    return new AmazonS3Client(awsCredentials, config);
                });

                services.AddScoped<IStorageService>(provider =>
                    new MinioStorageService(provider.GetRequiredService<IAmazonS3>(), bucketName));
                services.AddSingleton<MinioMultipartObjectStorage>();
                services.AddSingleton<IObjectStorage>(provider => provider.GetRequiredService<MinioMultipartObjectStorage>());
                services.AddSingleton<IFileContentStore>(provider => provider.GetRequiredService<MinioMultipartObjectStorage>());
            }

            // OpenSearch configuration
            var openSearchSection = configuration.GetSection("OpenSearch");
            var openSearchUri = new Uri(openSearchSection["Uri"] ?? "http://localhost:9200");

            var settings = new ConnectionSettings(openSearchUri)
                .DefaultIndex("files")
                .DisableDirectStreaming();

            services.AddSingleton<IOpenSearchClient>(provider =>
            {
                return new OpenSearchClient(settings);
            });

            services.AddScoped<ISearchService, OpenSearchService>();
            services.AddScoped<IAdvancedFileSearchService, OpenSearchService>();

            if (includeMessaging)
            {
                var rabbitMqSection = configuration.GetSection("RabbitMQ");
                var rabbitMqHost = rabbitMqSection["Host"] ?? "localhost";
                var rabbitMqUsername = rabbitMqSection["Username"] ?? "guest";
                var rabbitMqPassword = rabbitMqSection["Password"] ?? "guest";

                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(rabbitMqHost, h =>
                        {
                            h.Username(rabbitMqUsername);
                            h.Password(rabbitMqPassword);
                        });

                        cfg.ConfigureEndpoints(context);
                    });
                });

                services.AddScoped<IMessageBus, RabbitMqMessageBus>();
            }
            else
            {
                services.AddSingleton<IMessageBus, NoOpMessageBus>();
            }

            return services;
        }

        private static string NormalizeServiceUrl(string endpoint, bool useSsl)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return useSsl ? "https://localhost:9000" : "http://localhost:9000";
            }

            var trimmed = endpoint.Trim().TrimEnd('/');
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var scheme = useSsl ? "https://" : "http://";
            return $"{scheme}{trimmed}";
        }

        private static string FirstConfigured(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}
