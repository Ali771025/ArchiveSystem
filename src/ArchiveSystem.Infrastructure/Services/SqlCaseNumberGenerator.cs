using System.Data;
using ArchiveSystem.Application.Features.Cases.Interfaces;
using ArchiveSystem.Application.Features.Cases.Services;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Services;

public sealed class SqlCaseNumberGenerator : ICaseNumberGenerator
{
    private const string AllocateSerialSql = """
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DECLARE @NextSerial int;

        UPDATE [CaseNumberDailySequences] WITH (UPDLOCK, HOLDLOCK)
        SET @NextSerial = [LastSerial] = [LastSerial] + 1
        WHERE [DateKey] = @DateKey;

        IF @@ROWCOUNT = 0
        BEGIN
            SET @NextSerial = 1;
            INSERT INTO [CaseNumberDailySequences] ([DateKey], [LastSerial])
            VALUES (@DateKey, @NextSerial);
        END;

        IF @NextSerial > 99999
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50001, N'ظرفیت تولید شماره پرونده برای امروز تکمیل شده است.', 1;
        END;

        COMMIT TRANSACTION;
        SELECT @NextSerial;
        """;

    private readonly ArchiveDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SqlCaseNumberGenerator(ArchiveDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var iranTime = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), ResolveIranTimeZone());
        var dateKey = PersianCaseNumberFormat.GetDateKey(iranTime.DateTime);
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        var serial = await strategy.ExecuteAsync(async () =>
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = AllocateSerialSql;
            command.CommandType = CommandType.Text;

            var dateParameter = command.CreateParameter();
            dateParameter.ParameterName = "@DateKey";
            dateParameter.DbType = DbType.String;
            dateParameter.Size = 8;
            dateParameter.Value = dateKey;
            command.Parameters.Add(dateParameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        });

        return PersianCaseNumberFormat.Format(dateKey, serial);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
    }
}
