using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using PlanetWars.Shared;

namespace PlanetWars.Validators
{
    public class LogonRequestValidator : AbstractValidator<LogonRequest>
    {
        public LogonRequestValidator()
        {
            RuleFor(logon => logon.AgentName)
                .NotEmpty().WithMessage("Please use a non empty agent name, logon failed")
                .Length(1, 18).WithMessage("Please use an agent name between 1-18 characters, logon failed");

            RuleFor(logon => logon.MapGeneration)
                .Must(x => x == MapGenerationOption.Basic || x == MapGenerationOption.Random).WithMessage("Please use a valid Map Generation Option");
        }
    }
}