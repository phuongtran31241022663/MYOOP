namespace OOP.Infrastructure.Storage
{
    public interface IStorage
    {
        Task<T?> LoadAsync<T>(string fileName);
        Task SaveAsync<T>(string fileName, T data);
    }
}