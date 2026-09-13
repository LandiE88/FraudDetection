using Microsoft.AspNetCore.Identity;

namespace FraudDetection.Infrastructure.Identity;

/// <summary>
/// The user account ASP.NET Core Identity authenticates against. Deliberately not a
/// domain concept — unlike <c>AccountHolder</c> (a person a transaction can be linked
/// to), this is purely "who is allowed to call the API", a framework/infrastructure
/// concern with no fraud-detection business meaning, so it lives here rather than in
/// FraudDetection.Domain.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
}
