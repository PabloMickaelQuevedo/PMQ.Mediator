namespace PMQ.Mediator;

/// <summary>
/// Defines a mediator that combines <see cref="ISender"/> and <see cref="IPublisher"/> capabilities.
/// </summary>
public interface IMediator : ISender, IPublisher;
