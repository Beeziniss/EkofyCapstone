using EkofyApp.Domain.Utils;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed class CreateArtistMemberRequestValidator : AbstractValidator<CreateArtistMemberRequest>
{
    public CreateArtistMemberRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(15).WithMessage("Phone number must not exceed 15 characters.")
            .Matches(HelperMethod.RegexPatternPhoneNumber()).WithMessage("Invalid phone number format.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender is required");
    }
}
