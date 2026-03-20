namespace OOP.Domain.Interfaces
{
    /// <summary>
    /// Interface for repositories that support cache refresh.
    /// </summary>
    public interface ICacheRefreshable
    {
        Task RefreshCacheAsync();
    }
}
