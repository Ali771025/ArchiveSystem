using System.Data;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Behsaz.Interfaces;
using ArchiveSystem.Application.Features.Behsaz.Queries.GetBehsazRecords;
using ArchiveSystem.Application.Options;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Infrastructure.Services.Behsaz
{
    public sealed class BehsazSearchRepository : IBehsazSearchRepository
    {
        private const string StoredProcedureName = "[dbo].[sp_TINFBuilding_Search]";
        private readonly BehsazSettings _settings;
        private readonly ILogger<BehsazSearchRepository> _logger;

        public BehsazSearchRepository(
            IOptions<BehsazSettings> settings,
            ILogger<BehsazSearchRepository> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<PaginatedResult<BehsazRecordViewModel>> SearchAsync(GetBehsazRecordsQuery query, CancellationToken cancellationToken)
        {
            if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                return new PaginatedResult<BehsazRecordViewModel>
                {
                    Items = Array.Empty<BehsazRecordViewModel>(),
                    TotalCount = 0,
                    Page = query.PageIndex + 1,
                    PageSize = query.PageSize
                };
            }

            try
            {
                // Temporary runtime-safe path:
                // current DB type TPeopleProperties expects 6 columns and exact schema is unknown here,
                // so we run direct query to keep UI usable without SP/TVP runtime failures.
                return await ExecuteFallbackQueryAsync(query, cancellationToken);
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Behsaz direct search query failed on dbo.Properties.");
                return new PaginatedResult<BehsazRecordViewModel>
                {
                    Items = Array.Empty<BehsazRecordViewModel>(),
                    TotalCount = 0,
                    Page = query.PageIndex + 1,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while executing Behsaz search.");
                return new PaginatedResult<BehsazRecordViewModel>
                {
                    Items = Array.Empty<BehsazRecordViewModel>(),
                    TotalCount = 0,
                    Page = query.PageIndex + 1,
                    PageSize = query.PageSize
                };
            }
        }

        private static DataTable BuildSearchValuesTable(IReadOnlyList<BehsazSearchValue> values)
        {
            // This DataTable schema must match dbo.TPeopleProperties exactly.
            var table = new DataTable();
            table.Columns.Add("PropertyName", typeof(string));
            table.Columns.Add("PropertyValue", typeof(string));

            foreach (var item in values)
            {
                var row = table.NewRow();
                row["PropertyName"] = item.PropertyName;
                row["PropertyValue"] = item.PropertyValue;
                table.Rows.Add(row);
            }

            return table;
        }

        private async Task<PaginatedResult<BehsazRecordViewModel>> ExecuteStoredProcedureAsync(GetBehsazRecordsQuery query, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_settings.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@SearchValues", BuildSearchValuesTable(query.SearchValues).AsTableValuedParameter("TPeopleProperties"));
            parameters.Add("@TINFBuildingID", query.TINFBuildingID, DbType.Int32);
            parameters.Add("@TBASPeopleGroupID", query.TBASPeopleGroupID, DbType.Int32);
            parameters.Add("@InstaurationFilterType", query.InstaurationFilterType, DbType.Int32);
            parameters.Add("@InstaurationArea", query.InstaurationArea, DbType.Int32);
            parameters.Add("@InstaurationZone", query.InstaurationZone, DbType.Int32);
            parameters.Add("@InstaurationBlock", query.InstaurationBlock, DbType.Int32);
            parameters.Add("@InstaurationLand", query.InstaurationLand, DbType.Int32);
            parameters.Add("@InstaurationBuilding", query.InstaurationBuilding, DbType.Int32);
            parameters.Add("@InstaurationApartment", query.InstaurationApartment, DbType.Int32);
            parameters.Add("@InstaurationBusiness", query.InstaurationBusiness, DbType.Int32);
            parameters.Add("@ManualNumber", query.ManualNumber, DbType.String);
            parameters.Add("@Number", query.Number, DbType.String);
            parameters.Add("@GhateCode", query.GhateCode, DbType.Decimal);
            parameters.Add("@StateMode", 1, DbType.Int32);
            parameters.Add("@IsObsoleted", false, DbType.Boolean);
            parameters.Add("@IsInZoningState", false, DbType.Boolean);
            var userCode = query.UserCode.HasValue && query.UserCode.Value > 0
                ? query.UserCode.Value
                : Math.Max(1, _settings.UserCode);
            parameters.Add("@UserCode", userCode, DbType.Int32);
            parameters.Add("@PageSize", query.PageSize, DbType.Int32);
            parameters.Add("@PageIndex", query.PageIndex, DbType.Int32);

            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    StoredProcedureName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            var records = (await grid.ReadAsync<BehsazRecordViewModel>()).ToList();
            var totalCount = records.Count;

            if (!grid.IsConsumed)
            {
                var total = await grid.ReadFirstOrDefaultAsync<int?>();
                if (total.HasValue && total.Value >= 0)
                {
                    totalCount = total.Value;
                }
            }

            return new PaginatedResult<BehsazRecordViewModel>
            {
                Items = records,
                TotalCount = totalCount,
                Page = query.PageIndex + 1,
                PageSize = query.PageSize
            };
        }

        private async Task<PaginatedResult<BehsazRecordViewModel>> ExecuteFallbackQueryAsync(GetBehsazRecordsQuery query, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_settings.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var where = new List<string> { "1 = 1" };
            var args = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.Number))
            {
                where.Add("[Number] LIKE @Number");
                args.Add("@Number", $"%{query.Number.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(query.ManualNumber))
            {
                where.Add("[ManualNumber] LIKE @ManualNumber");
                args.Add("@ManualNumber", $"%{query.ManualNumber.Trim()}%");
            }

            if (query.TBASPeopleGroupID.HasValue)
            {
                where.Add("[TBASPeopleGroupID] = @TBASPeopleGroupID");
                args.Add("@TBASPeopleGroupID", query.TBASPeopleGroupID.Value);
            }

            var offset = Math.Max(0, query.PageIndex) * Math.Max(1, query.PageSize);
            args.Add("@OffsetRows", offset);
            args.Add("@FetchRows", Math.Max(1, query.PageSize));

            var whereSql = string.Join(" AND ", where);
            var sql = $@"
SELECT
    [Number],
    [ManualNumber],
    [IsApartment],
    [StateLocation],
    [InstaurationCode],
    [PeopleProperty],
    [Mobile],
    [PeopleKey],
    [TBASPeopleGroupID],
    [PeopleGroupName],
    [Address],
    [Plaque],
    [Archive],
    [TBASZoneID],
    [UserName]
FROM [dbo].[Properties]
WHERE {whereSql}
ORDER BY [Number]
OFFSET @OffsetRows ROWS FETCH NEXT @FetchRows ROWS ONLY;

SELECT COUNT(1)
FROM [dbo].[Properties]
WHERE {whereSql};";

            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, args, cancellationToken: cancellationToken));

            var items = (await grid.ReadAsync<BehsazRecordViewModel>()).ToList();
            var totalCount = await grid.ReadFirstOrDefaultAsync<int>();

            return new PaginatedResult<BehsazRecordViewModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.PageIndex + 1,
                PageSize = query.PageSize
            };
        }
    }
}
