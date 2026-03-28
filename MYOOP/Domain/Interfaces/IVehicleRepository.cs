using OOP.Domain.Entities;

namespace OOP.Domain.Interfaces
{
    public interface IVehicleRepository
    {
        Task Add(Vehicle vehicle);
        Task Update(Vehicle vehicle);
        Task Delete(Guid vehicleId);

        Task<Vehicle?> GetById(Guid id);
        Task<Vehicle?> GetByDriverId(Guid driverId);

        Task<List<Vehicle>> GetAll();
    }
}