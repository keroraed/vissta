using FluentValidation;
using MediatR;

namespace VISSTA.Application.Features.Auth;

public sealed record RegisterCommand(string FullName, string Email, string Password, string PhoneNumber) : IRequest<bool>;
public sealed record LoginCommand(string Email, string Password, bool RememberMe) : IRequest<bool>;
public sealed record ForgotPasswordCommand(string Email) : IRequest<bool>;
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<bool>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}
