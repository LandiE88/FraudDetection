using System.Reflection;
using FluentValidation;
using FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;
using FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.AccountHolders.Queries.GetAccountHolderById;
using FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;
using FraudDetection.Application.Common.Events;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Common.Models;
using FraudDetection.Application.Transactions.Commands.IngestTransaction;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Application.Transactions.EventHandlers;
using FraudDetection.Application.Transactions.Queries.GetTransactionById;
using FraudDetection.Application.Transactions.Queries.GetTransactions;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Transactions.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetection.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        AddHandlers(services);
        AddDomainEventDispatch(services);
        AddFraudRuleEngine(services);

        return services;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        // Each use case is registered against its own interface and resolved by the
        // controller directly — there is no bus to route through.
        services.AddScoped<ICommandHandler<IngestTransactionCommand, TransactionResponse>, IngestTransactionCommandHandler>();
        services.AddScoped<IQueryHandler<GetTransactionByIdQuery, TransactionResponse>, GetTransactionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>>, GetTransactionsQueryHandler>();
        services.AddScoped<IQueryHandler<SearchAccountHoldersQuery, PagedResult<AccountHolderResponse>>, SearchAccountHoldersQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountHolderByIdQuery, AccountHolderResponse>, GetAccountHolderByIdQueryHandler>();
        services.AddScoped<ICommandHandler<CreateAccountHolderCommand, AccountHolderResponse>, CreateAccountHolderCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountHolderCommand, AccountHolderResponse>, UpdateAccountHolderCommandHandler>();
    }

    private static void AddDomainEventDispatch(IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<TransactionFlaggedForFraudEvent>, TransactionFlaggedForFraudEventHandler>();
    }

    private static void AddFraudRuleEngine(IServiceCollection services)
    {
        // Every fraud rule the engine should apply. Adding a new criterion is a matter
        // of implementing IFraudRule and listing it here — nothing else needs to change.
        services.AddSingleton<IFraudRule, LargeAmountRule>();
        services.AddSingleton<IFraudRule, HighVelocityRule>();
        services.AddSingleton<IFraudRule, UnusualHoursRule>();
        services.AddSingleton<IFraudRule, StructuringRule>();
        services.AddSingleton<IFraudRule, DuplicateTransactionRule>();

        services.AddSingleton<FraudRuleEngine>();
    }
}
