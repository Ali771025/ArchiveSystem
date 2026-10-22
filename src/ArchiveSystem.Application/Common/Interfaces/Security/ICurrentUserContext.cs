namespace ArchiveSystem.Application.Common.Interfaces.Security
{
    public interface ICurrentUserContext
    {
        bool IsAuthenticated { get; }
        long? UserId { get; }
        bool IsInRole(string role);
    }
}
