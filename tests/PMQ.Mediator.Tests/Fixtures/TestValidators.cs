using FluentValidation;

namespace PMQ.Mediator.Tests.Fixtures;

public class PingValidator : AbstractValidator<Ping>
{
    public PingValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required.");
    }
}

public class PingMaxLengthValidator : AbstractValidator<Ping>
{
    public PingMaxLengthValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(10)
            .WithMessage("Message must be at most 10 characters.");
    }
}
