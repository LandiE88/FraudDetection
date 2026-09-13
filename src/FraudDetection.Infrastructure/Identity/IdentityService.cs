using FraudDetection.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace FraudDetection.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<CreateUserResult> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);

        return result.Succeeded
            ? CreateUserResult.Success(user.Id)
            : CreateUserResult.Failure(result.Errors.Select(ToFieldError).ToList());
    }

    public async Task<Guid?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        // Uses SignInManager rather than UserManager.CheckPasswordAsync so repeated
        // failures actually count toward account lockout instead of being checked
        // with no memory of prior attempts.
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        return result.Succeeded ? user.Id : null;
    }

    private static IdentityFieldError ToFieldError(IdentityError error)
    {
        var field = error.Code switch
        {
            _ when error.Code.Contains("Password", StringComparison.Ordinal) => "Password",
            _ when error.Code.Contains("Email", StringComparison.Ordinal) => "Email",
            _ when error.Code.Contains("UserName", StringComparison.Ordinal) => "Email",
            _ => "Register"
        };

        return new IdentityFieldError(field, error.Description);
    }
}
