using FluentValidation;
using peeposredemption.Application.Features.Auth.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace peeposredemption.Application.Validators
{
    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
