namespace ArchiveSystem.Application.Common.Interfaces.Repositories
{
    public interface IUserRoleRepository
    {
        Task<IReadOnlyCollection<long>> GetRoleIdsByUserIdAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<long>> GetUserIdsByRoleIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);
        Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default);
    }
}
