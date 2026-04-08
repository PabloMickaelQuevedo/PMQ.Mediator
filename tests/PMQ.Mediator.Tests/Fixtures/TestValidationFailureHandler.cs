using FluentValidation.Results;
using PMQ.Mediator;

namespace PMQ.Mediator.Tests.Fixtures;

public class TestValidationFailureHandler<TResponse> : IValidationFailureHandler<TResponse>
{
    public static List<ValidationFailure>? LastFailures { get; private set; }

    public TResponse HandleFailure(IEnumerable<ValidationFailure> failures)
    {
        LastFailures = failures.ToList();
        return default!;
    }

    public static void Reset() => LastFailures = null;
}
