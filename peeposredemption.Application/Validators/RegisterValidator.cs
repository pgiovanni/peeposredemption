using FluentValidation;
using peeposredemption.Application.Features.Auth.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Validators
{

    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(32);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            // DOB validation disabled for now — re-enable when age gate goes live
            // RuleFor(x => x.DateOfBirth).NotEmpty()
            //     .Must(dob => dob.Value.AddYears(13) <= DateTime.UtcNow)
            //     .WithMessage("You must be at least 13 years old to create an account.");
        }
    }
}
