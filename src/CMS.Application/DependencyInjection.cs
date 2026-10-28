using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Behaviors;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.SharedKernel.Results;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Application;

// Register the command pipeline explicitly so its transaction order remains reviewable.
public static class DependencyInjection
{
    // Application registration contains no infrastructure or HTTP services.
    public static IServiceCollection AddCmsApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        AddCommand<CreateTenantCommand>(services);
        AddCommand<CreateLocaleCommand>(services);
        AddCommand<CMS.Application.Identity.CreateRoleCommand>(services);
        AddCommand<CMS.Application.Identity.AssignRoleCommand>(services);
        AddCommand<CMS.Application.Content.CreateContentTypeCommand>(services);
        AddCommand<CMS.Application.Content.SaveEntryCommand>(services);
        AddCommand<CMS.Application.Content.TransitionEntryCommand>(services);
        AddCommand<CMS.Application.Content.RestoreEntryCommand>(services);
        return services;
    }

    private static void AddCommand<TCommand>(IServiceCollection services) where TCommand : class, ICommand
    {
        services.AddTransient<IPipelineBehavior<TCommand, Result<Guid>>, ValidationBehavior<TCommand>>();
        services.AddTransient<IPipelineBehavior<TCommand, Result<Guid>>, IdempotencyBehavior<TCommand>>();
        services.AddTransient<IPipelineBehavior<TCommand, Result<Guid>>, TransactionBehavior<TCommand>>();
    }
}
