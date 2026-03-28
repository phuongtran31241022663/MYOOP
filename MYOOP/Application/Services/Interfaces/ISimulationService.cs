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
        
        // Kiểm tra xem simulation có đang chạy cho trip này không
        // true = đang chạy (driver đang di chuyển)
        // false = đã hoàn thành hoặc không có simulation
        bool IsSimulationActive(Guid tripId);
    }
}
