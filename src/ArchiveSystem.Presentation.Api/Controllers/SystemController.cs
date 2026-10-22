using ArchiveSystem.Application.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [ApiController]
    [Route("api/system")]
    public sealed class SystemController : ControllerBase
    {
        private readonly IOptions<BehsazSettings> _behsazSettings;

        public SystemController(IOptions<BehsazSettings> behsazSettings)
        {
            _behsazSettings = behsazSettings;
        }

        [HttpGet("settings")]
        [ProducesResponseType(typeof(SystemSettingsResponse), StatusCodes.Status200OK)]
        public ActionResult<SystemSettingsResponse> GetSettings()
        {
            return Ok(new SystemSettingsResponse
            {
                BehsazIntegration = new BehsazIntegrationSettingsResponse
                {
                    IsEnabled = _behsazSettings.Value.IsEnabled,
                    UserCode = _behsazSettings.Value.UserCode
                }
            });
        }
    }

    public sealed class SystemSettingsResponse
    {
        public required BehsazIntegrationSettingsResponse BehsazIntegration { get; init; }
    }

    public sealed class BehsazIntegrationSettingsResponse
    {
        public bool IsEnabled { get; init; }
        public int UserCode { get; init; }
    }
}
