using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Reports.Commands.LogEvent
{
    public sealed class LogEventCommand : IRequest<Unit>
    {
        public required MovementLogWriteModel Event { get; init; }
    }

    public sealed class LogEventCommandHandler : IRequestHandler<LogEventCommand, Unit>
    {
        private readonly IAuditWriter _auditWriter;

        public LogEventCommandHandler(IAuditWriter auditWriter)
        {
            _auditWriter = auditWriter;
        }

        public async Task<Unit> Handle(LogEventCommand request, CancellationToken cancellationToken)
        {
            await _auditWriter.WriteAsync(request.Event, cancellationToken);
            return Unit.Value;
        }
    }
}
