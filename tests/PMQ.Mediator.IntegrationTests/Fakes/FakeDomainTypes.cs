using FluentValidation;
using FluentValidation.Results;
using PMQ.Mediator;

namespace PMQ.Mediator.IntegrationTests.Fakes;

// ── Simulates a real domain command (like CreateOrder) ──

public record CreateOrderCommand(string CustomerName, decimal Total) : IRequest<CreateOrderResult>;

public record CreateOrderResult(int OrderId, string Status);

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public static bool WasCalled { get; private set; }
    public static void Reset() => WasCalled = false;

    public Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(new CreateOrderResult(1, "Created"));
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("Total must be greater than zero.");
    }
}

// ── Simulates a void command (like DeleteOrder) ──

public record DeleteOrderCommand(int OrderId) : IRequest;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
{
    public static bool WasCalled { get; private set; }
    public static void Reset() => WasCalled = false;

    public Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.CompletedTask;
    }
}

// ── Simulates domain notifications (like OrderCreatedEvent) ──

public record OrderCreatedEvent(int OrderId, string CustomerName) : INotification;

public class SendEmailOnOrderCreated : INotificationHandler<OrderCreatedEvent>
{
    public static readonly List<int> ProcessedOrders = [];
    public static void Reset() => ProcessedOrders.Clear();

    public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        ProcessedOrders.Add(notification.OrderId);
        return Task.CompletedTask;
    }
}

public class UpdateDashboardOnOrderCreated : INotificationHandler<OrderCreatedEvent>
{
    public static readonly List<int> ProcessedOrders = [];
    public static void Reset() => ProcessedOrders.Clear();

    public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        ProcessedOrders.Add(notification.OrderId);
        return Task.CompletedTask;
    }
}

// ── Simulates a stream request (like GetOrderUpdates) ──

public record GetOrderUpdatesStream(int OrderId) : IStreamRequest<string>;

public class GetOrderUpdatesStreamHandler : IStreamRequestHandler<GetOrderUpdatesStream, string>
{
    public async IAsyncEnumerable<string> Handle(GetOrderUpdatesStream request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return $"Order {request.OrderId}: Pending";
        await Task.Yield();
        yield return $"Order {request.OrderId}: Processing";
        await Task.Yield();
        yield return $"Order {request.OrderId}: Completed";
    }
}

// ── Simulates notification-pattern failure handler (like PMQ.Notifications) ──

public class FakeNotificationContext
{
    public List<(string Property, string Message)> Notifications { get; } = [];

    public void AddNotification(string property, string message)
        => Notifications.Add((property, message));
}

public class NotificationFailureHandler<TResponse>(FakeNotificationContext context) : IValidationFailureHandler<TResponse>
{
    public TResponse HandleFailure(IEnumerable<ValidationFailure> failures)
    {
        foreach (var failure in failures)
            context.AddNotification(failure.PropertyName, failure.ErrorMessage);

        return default!;
    }
}
