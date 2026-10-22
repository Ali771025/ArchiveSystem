using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArchiveSystem.Infrastructure.Services
{
    public interface IBehsazIntegrationService
    {
        bool IsEnabled { get; }
        Task<bool> ProcessCaseAsync(long caseId, string caseNumber, CancellationToken cancellationToken = default);
        Task<bool> ProcessFileAsync(long fileId, string fileName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BehsazSearchResult>> SearchCasesAsync(BehsazSearchQuery query, CancellationToken cancellationToken = default);
    }

    public class BehsazIntegrationService : IBehsazIntegrationService
    {
        private readonly string _connectionString;
        private readonly int _commandTimeoutSeconds;
        private readonly int _retryCount;
        private readonly int _circuitBreakerFailureThreshold;
        private readonly TimeSpan _circuitBreakerOpenDuration;
        private readonly string _searchStoredProcedure;
        private readonly string _processCaseStoredProcedure;
        private readonly string _processFileStoredProcedure;
        private readonly ILogger<BehsazIntegrationService> _logger;
        private readonly object _circuitStateLock = new();
        private int _consecutiveFailures;
        private DateTime? _circuitOpenedAtUtc;
        private bool _halfOpenProbeInFlight;

        public bool IsEnabled { get; }

        public BehsazIntegrationService(IConfiguration configuration, ILogger<BehsazIntegrationService> logger)
        {
            var behsazConnection = configuration.GetConnectionString("BehsazConnection");
            var defaultConnection = configuration.GetConnectionString("DefaultConnection");
            _connectionString = !string.IsNullOrWhiteSpace(behsazConnection)
                ? behsazConnection
                : (!string.IsNullOrWhiteSpace(defaultConnection)
                    ? defaultConnection
                    : string.Empty);

            IsEnabled = configuration.GetValue<bool>("Integrations:Behsaz:IsEnabled", false);
            _commandTimeoutSeconds = Math.Max(1, configuration.GetValue<int?>("Integrations:Behsaz:TimeoutSeconds") ?? 30);
            _retryCount = Math.Clamp(configuration.GetValue<int?>("Integrations:Behsaz:RetryCount") ?? 2, 0, 5);
            _circuitBreakerFailureThreshold = Math.Clamp(
                configuration.GetValue<int?>("Integrations:Behsaz:CircuitBreaker:FailureThreshold") ?? 5,
                1,
                100);
            _circuitBreakerOpenDuration = TimeSpan.FromSeconds(Math.Clamp(
                configuration.GetValue<int?>("Integrations:Behsaz:CircuitBreaker:OpenDurationSeconds") ?? 30,
                5,
                3600));
            _searchStoredProcedure = configuration.GetValue<string>("Integrations:Behsaz:SearchStoredProcedure") ?? "sp_Behsaz_SearchCases";
            _processCaseStoredProcedure = configuration.GetValue<string>("Integrations:Behsaz:ProcessCaseStoredProcedure") ?? "sp_Behsaz_ProcessCase";
            _processFileStoredProcedure = configuration.GetValue<string>("Integrations:Behsaz:ProcessFileStoredProcedure") ?? "sp_Behsaz_ProcessFile";
            _logger = logger;
        }

        public async Task<bool> ProcessCaseAsync(long caseId, string caseNumber, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger.LogInformation("Behsaz integration is disabled");
                return true;
            }

            return await ExecuteWithResiliencyAsync("ProcessCase", async () =>
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new SqlCommand(_processCaseStoredProcedure, connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandTimeout = _commandTimeoutSeconds
                };

                command.Parameters.AddWithValue("@CaseId", caseId);
                command.Parameters.AddWithValue("@CaseNumber", caseNumber.Trim());

                var result = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Behsaz integration processed case {CaseId} with result {Result}", caseId, result);

                return result > 0;
            }, cancellationToken);
        }

        public async Task<bool> ProcessFileAsync(long fileId, string fileName, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                _logger.LogInformation("Behsaz integration is disabled");
                return true;
            }

            return await ExecuteWithResiliencyAsync("ProcessFile", async () =>
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new SqlCommand(_processFileStoredProcedure, connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandTimeout = _commandTimeoutSeconds
                };

                command.Parameters.AddWithValue("@FileId", fileId);
                command.Parameters.AddWithValue("@FileName", fileName.Trim());

                var result = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Behsaz integration processed file {FileId} with result {Result}", fileId, result);

                return result > 0;
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<BehsazSearchResult>> SearchCasesAsync(BehsazSearchQuery query, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return Array.Empty<BehsazSearchResult>();
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger.LogWarning("Behsaz integration is enabled but connection string is missing. Returning empty search result.");
                return Array.Empty<BehsazSearchResult>();
            }

            try
            {
                return await ExecuteWithResiliencyAsync("SearchCases", async () =>
                {
                    using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);

                    using var command = new SqlCommand(_searchStoredProcedure, connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure,
                        CommandTimeout = _commandTimeoutSeconds
                    };

                    command.Parameters.AddWithValue("@Text", query.Text.Trim());
                    command.Parameters.AddWithValue("@FromDate", (object?)query.FromDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ToDate", (object?)query.ToDate ?? DBNull.Value);

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    var items = new List<BehsazSearchResult>();
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        items.Add(new BehsazSearchResult
                        {
                            ExternalCaseNumber = reader["ExternalCaseNumber"]?.ToString() ?? string.Empty,
                            Title = reader["Title"]?.ToString() ?? string.Empty,
                            OwnerName = reader["OwnerName"]?.ToString() ?? string.Empty,
                            NationalCode = reader["NationalCode"]?.ToString() ?? string.Empty,
                            RegisteredAt = reader["RegisteredAt"] is DateTime dt ? dt : DateTime.MinValue
                        });
                    }

                    return (IReadOnlyList<BehsazSearchResult>)items;
                }, cancellationToken);
            }
            catch (SqlException ex) when (ex.Number == 2812)
            {
                _logger.LogWarning(ex, "Behsaz stored procedure {StoredProcedure} was not found. Returning empty result for UI continuity.", _searchStoredProcedure);
                return Array.Empty<BehsazSearchResult>();
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Behsaz SQL failure in SearchCases. Returning empty result for UI continuity.");
                return Array.Empty<BehsazSearchResult>();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Behsaz integration failed in SearchCases. Returning empty result for UI continuity.");
                return Array.Empty<BehsazSearchResult>();
            }
        }

        private async Task<T> ExecuteWithResiliencyAsync<T>(string operation, Func<Task<T>> action, CancellationToken cancellationToken)
        {
            EnsureCircuitAllowsExecution(operation);

            var attempt = 0;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await action();
                    stopwatch.Stop();
                    RecordSuccessfulExecution(operation, stopwatch.ElapsedMilliseconds);
                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < _retryCount)
                {
                    attempt++;
                    _logger.LogWarning(
                        LogEvents.RetryAttempt,
                        ex,
                        "Behsaz operation retry. Operation={Operation} Attempt={Attempt} MaxRetries={MaxRetries} CircuitState={CircuitState} FailureThreshold={FailureThreshold}",
                        operation,
                        attempt,
                        _retryCount,
                        GetCircuitState(),
                        _circuitBreakerFailureThreshold);
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    RecordFailedExecution(operation, ex, attempt + 1, stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }

        private void EnsureCircuitAllowsExecution(string operation)
        {
            lock (_circuitStateLock)
            {
                if (!_circuitOpenedAtUtc.HasValue)
                {
                    return;
                }

                var reopenAt = _circuitOpenedAtUtc.Value.Add(_circuitBreakerOpenDuration);
                var now = DateTime.UtcNow;
                if (now < reopenAt)
                {
                    var remainingMs = (long)Math.Max(0, (reopenAt - now).TotalMilliseconds);
                    _logger.LogWarning(
                        LogEvents.CircuitOpenRejected,
                        "Behsaz circuit breaker rejected call. Operation={Operation} RemainingOpenMs={RemainingOpenMs} ConsecutiveFailures={ConsecutiveFailures} FailureThreshold={FailureThreshold}",
                        operation,
                        remainingMs,
                        _consecutiveFailures,
                        _circuitBreakerFailureThreshold);
                    throw new InvalidOperationException($"Behsaz circuit breaker is open for operation '{operation}'.");
                }

                if (_halfOpenProbeInFlight)
                {
                    _logger.LogWarning(
                        LogEvents.CircuitHalfOpenRejected,
                        "Behsaz circuit breaker half-open probe already in-flight. Operation={Operation}",
                        operation);
                    throw new InvalidOperationException($"Behsaz circuit breaker is half-open for operation '{operation}'.");
                }

                _halfOpenProbeInFlight = true;
                _logger.LogInformation(
                    LogEvents.CircuitHalfOpenProbe,
                    "Behsaz circuit breaker switching to half-open probe. Operation={Operation}",
                    operation);
            }
        }

        private void RecordSuccessfulExecution(string operation, long elapsedMs)
        {
            lock (_circuitStateLock)
            {
                var hadFailures = _consecutiveFailures > 0 || _circuitOpenedAtUtc.HasValue || _halfOpenProbeInFlight;
                _consecutiveFailures = 0;
                _circuitOpenedAtUtc = null;
                _halfOpenProbeInFlight = false;

                if (hadFailures)
                {
                    _logger.LogInformation(
                        LogEvents.CircuitClosed,
                        "Behsaz circuit breaker closed after successful call. Operation={Operation} DurationMs={DurationMs}",
                        operation,
                        elapsedMs);
                }
            }
        }

        private void RecordFailedExecution(string operation, Exception exception, int attempts, long elapsedMs)
        {
            lock (_circuitStateLock)
            {
                _consecutiveFailures++;
                var shouldOpenCircuit = _consecutiveFailures >= _circuitBreakerFailureThreshold;
                _halfOpenProbeInFlight = false;

                if (shouldOpenCircuit)
                {
                    _circuitOpenedAtUtc = DateTime.UtcNow;
                    _logger.LogError(
                        LogEvents.CircuitOpened,
                        exception,
                        "Behsaz circuit opened after failure. Operation={Operation} Attempts={Attempts} DurationMs={DurationMs} ConsecutiveFailures={ConsecutiveFailures} FailureThreshold={FailureThreshold} OpenDurationSeconds={OpenDurationSeconds}",
                        operation,
                        attempts,
                        elapsedMs,
                        _consecutiveFailures,
                        _circuitBreakerFailureThreshold,
                        (int)_circuitBreakerOpenDuration.TotalSeconds);
                }
                else
                {
                    _logger.LogError(
                        LogEvents.OperationFailure,
                        exception,
                        "Behsaz operation failed after retries. Operation={Operation} Attempts={Attempts} DurationMs={DurationMs} ConsecutiveFailures={ConsecutiveFailures} FailureThreshold={FailureThreshold}",
                        operation,
                        attempts,
                        elapsedMs,
                        _consecutiveFailures,
                        _circuitBreakerFailureThreshold);
                }
            }
        }

        private string GetCircuitState()
        {
            lock (_circuitStateLock)
            {
                if (_circuitOpenedAtUtc.HasValue)
                {
                    return _halfOpenProbeInFlight ? "HalfOpen" : "Open";
                }

                return "Closed";
            }
        }

        private static class LogEvents
        {
            public static readonly EventId RetryAttempt = new(8101, nameof(RetryAttempt));
            public static readonly EventId OperationFailure = new(8102, nameof(OperationFailure));
            public static readonly EventId CircuitOpened = new(8103, nameof(CircuitOpened));
            public static readonly EventId CircuitClosed = new(8104, nameof(CircuitClosed));
            public static readonly EventId CircuitOpenRejected = new(8105, nameof(CircuitOpenRejected));
            public static readonly EventId CircuitHalfOpenProbe = new(8106, nameof(CircuitHalfOpenProbe));
            public static readonly EventId CircuitHalfOpenRejected = new(8107, nameof(CircuitHalfOpenRejected));
        }
    }

    public sealed class BehsazSearchQuery
    {
        public string Text { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public sealed class BehsazSearchResult
    {
        public string ExternalCaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
