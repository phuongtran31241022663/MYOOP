﻿namespace OOP.Application.Services.Interfaces
{
    public interface INotificationService
    {
        event Action<Guid, string>? OnPassengerNotified;
        event Action<Guid, string>? OnDriverNotified;
        event Action<Guid, string>? OnTripUpdated;

        Task NotifyPassenger(Guid passengerId, string message);
        Task NotifyDriver(Guid driverId, string message);
        Task NotifyTripUpdate(Guid tripId, string message);
    }
}