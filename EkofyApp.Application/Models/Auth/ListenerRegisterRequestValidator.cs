using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Auth;
public sealed class ListenerRegisterRequestValidator : AbstractValidator<ListenerRegisterRequest>
{
    public ListenerRegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm Password is required")
            .MinimumLength(8).WithMessage("Confirm Password must be at least 8 characters long")
            .MaximumLength(100).WithMessage("Confirm Password must not exceed 100 characters")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Birth Date is required")
            .GreaterThan(DateTime.MinValue).WithMessage("Birth Date must be in the past")
            .LessThan(HelperMethod.GetUtcPlus7Time().AddYears(-1)).WithMessage("Birth Date must be in the past");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender must be Male or Female or Other");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters long")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
    }
}
