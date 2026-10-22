using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ArchiveSystem.Application.Behaviors;
using ArchiveSystem.Application.Features.Authentication.Interfaces;
using ArchiveSystem.Application.Features.Authentication.Services;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using ArchiveSystem.Application.Features.UserManagement.Services;

namespace ArchiveSystem.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            });

            // FluentValidation
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IClassificationAccessAuthorizer, ClassificationAccessAuthorizer>();
            services.AddScoped<IFileAccessAuthorizer, FileAccessAuthorizer>();
            services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
            
            return services;
        }
    }
}
