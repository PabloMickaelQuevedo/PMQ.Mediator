# PMQ.Mediator

A lightweight mediator pattern implementation for .NET applications with FluentValidation integration, request/response pipeline behaviors, streaming support, and notification publishing.

## Features

- 🚀 **Request/Response** - Send commands and queries with typed responses
- 📡 **Notifications** - Fan-out pub/sub with multiple handlers per event
- 🔄 **Async Streams** - `IAsyncEnumerable<T>` support via `IStreamRequest<T>`
- ✅ **Validation** - Built-in FluentValidation pipeline behavior
- 📋 **Logging** - Built-in logging behavior with execution time tracking
- 🔗 **Pipeline Behaviors** - Middleware-style composition for requests and streams
- 🔍 **Assembly Scanning** - Auto-discovers handlers with prefix filtering
- ⚙️ **Highly Configurable** - Lifetime, culture, custom validation failure handling

## Installation

```bash
dotnet add package PMQ.Mediator
```

## Quick Start

### 1. Register the Mediator

In your `Program.cs`:

```csharp
builder.Services.AddPmqMediator(options =>
{
    options.RegisterServicesFromAssemblies(typeof(CreateOrderCommand).Assembly);
    options.UseValidationBehavior = true;
    options.UseLoggingBehavior = true;
});
```

### 2. Define a Request and Handler

```csharp
public record CreateOrderCommand(string CustomerName, decimal Amount) : IRequest<OrderResult>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResult>
{
    public Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // your logic here
        return Task.FromResult(new OrderResult(Guid.NewGuid()));
    }
}
```

### 3. Send the Request

```csharp
public class OrderController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }
}
```

## Core Concepts

### Request/Response

Use `IRequest<TResponse>` for commands/queries that return a value, or `IRequest` for fire-and-forget:

```csharp
// With response
public record GetUserQuery(int Id) : IRequest<UserDto>;

// Void (no response)
public record DeleteUserCommand(int Id) : IRequest;
```

### Notifications

Notifications are dispatched to all registered handlers (fan-out):

```csharp
public record OrderCreatedEvent(Guid OrderId) : INotification;

public class SendEmailHandler : INotificationHandler<OrderCreatedEvent>
{
    public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // send email
        return Task.CompletedTask;
    }
}

// Publish
await mediator.Publish(new OrderCreatedEvent(orderId));
```

### Async Streams

Stream results using `IAsyncEnumerable<T>`:

```csharp
public record GetItemsQuery : IStreamRequest<ItemDto>;

public class GetItemsHandler : IStreamRequestHandler<GetItemsQuery, ItemDto>
{
    public async IAsyncEnumerable<ItemDto> Handle(GetItemsQuery request, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in GetItemsAsync(cancellationToken))
            yield return item;
    }
}

// Consume
await foreach (var item in mediator.CreateStream(new GetItemsQuery()))
{
    Console.WriteLine(item);
}
```

### Pipeline Behaviors

Create custom behaviors that wrap request handling (like middleware):

```csharp
public class MyCustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, 
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // before
        var response = await next();
        // after
        return response;
    }
}
```

## Configuration

```csharp
builder.Services.AddPmqMediator(options =>
{
    // Assembly scanning
    options.RegisterServicesFromAssemblies(assembly1, assembly2);
    options.RegisterServicesFromAssemblyContaining<MyHandler>();
    options.RegisterServicesFromAssemblyPrefixes("MyCompany.", "MyProject");

    // Service lifetime (default: Scoped)
    options.Lifetime = ServiceLifetime.Scoped;

    // Built-in pipeline behaviors
    options.UseValidationBehavior = true;
    options.UseLoggingBehavior = true;

    // FluentValidation culture
    options.ValidatorCulture = new CultureInfo("pt-BR");

    // Custom validation failure handler
    options.ValidationFailureHandlerType = typeof(CustomFailureHandler<>);
});
```

| Option | Default | Description |
|--------|---------|-------------|
| `Lifetime` | `Scoped` | DI lifetime for IMediator |
| `UseValidationBehavior` | `false` | Enable automatic FluentValidation |
| `UseLoggingBehavior` | `false` | Enable request logging with timing |
| `ValidatorCulture` | `null` | Culture for validation error messages |
| `ValidationFailureHandlerType` | `null` | Custom handler for validation failures |

## Validation

When `UseValidationBehavior` is enabled, all registered `IValidator<TRequest>` validators are executed before the handler. If validation fails, a `ValidationException` is thrown — unless a custom `IValidationFailureHandler<TResponse>` is registered:

```csharp
public class NotificationFailureHandler<TResponse> : IValidationFailureHandler<TResponse>
{
    public TResponse HandleFailure(IEnumerable<ValidationFailure> failures)
    {
        // Transform failures into your domain error response
    }
}
```

## License

This project is licensed under the [MIT License](LICENSE.txt).
