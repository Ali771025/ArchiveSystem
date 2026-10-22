using System.Threading;
using System.Threading.Tasks;
using ArchiveSystem.Application.Features.Authentication.DTOs;

namespace ArchiveSystem.Application.Features.Authentication.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthenticationResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    }
}
