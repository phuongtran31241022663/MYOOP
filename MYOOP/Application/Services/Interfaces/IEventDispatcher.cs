using OOP.Domain.Events;

namespace OOP.Application.Services.Interfaces
{
    /// <summary>
    /// Interface for dispatching domain events to their handlers.
    /// </summary>
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    }
}
