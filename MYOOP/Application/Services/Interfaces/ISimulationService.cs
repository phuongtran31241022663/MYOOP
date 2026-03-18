namespace OOP.Application.Services.Interfaces
{
    public interface ISimulationService
    {
        Task SimulateDriverToPickup(Guid tripId);
        Task SimulateTripToDestination(Guid tripId);
        Task Tick();
        Task StopSimulation(Guid tripId);
        Task UpdateDriverLocations();
        Task SimulateTripProgress(Guid tripId);
    }
}
