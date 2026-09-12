namespace FraudDetection.Application.Common.Models;

/// <summary>API-facing shape of a page of results, decoupled from the domain's <c>PagedList&lt;T&gt;</c>.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
