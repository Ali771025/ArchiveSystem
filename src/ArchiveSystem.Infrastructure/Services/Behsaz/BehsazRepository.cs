using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Common;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Integrations;
using ArchiveSystem.Domain.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Reflection;

namespace ArchiveSystem.Infrastructure.Services.Behsaz
{
    public sealed class BehsazRepository : IBehsazRepository
    {
        private const string StoredProcedureName = "[dbo].[sp_TINFBuilding_Search]";
        private const string PeoplePropertiesTvpTypeName = "TPeopleProperties";
        private readonly BehsazSettings _settings;
        private readonly ILogger<BehsazRepository> _logger;

        static BehsazRepository()
        {
            SqlMapper.SetTypeMap(
                typeof(BehsazSearchResultDto),
                new CustomPropertyTypeMap(typeof(BehsazSearchResultDto), ResolveResultProperty));
        }

        public BehsazRepository(
            IOptions<BehsazSettings> settings,
            ILogger<BehsazRepository> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<PagedResult<BehsazSearchResultDto>> SearchAsync(
            BehsazSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.IsEnabled || string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                return Empty(criteria);
            }

            try
            {
                await using var connection = new SqlConnection(_settings.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                using var grid = await connection.QueryMultipleAsync(
                    new CommandDefinition(
                        StoredProcedureName,
                        BuildParameters(criteria),
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken));
                var rows = (await grid.ReadAsync<BehsazSearchResultDto>()).ToList();
                foreach (var row in rows)
                {
                    row.TaxpayerDetails = BuildTaxpayerDetails(row);
                }

                var totalCount = rows.Count;

                if (!grid.IsConsumed)
                {
                    var total = await grid.ReadFirstOrDefaultAsync<int?>();
                    if (total.HasValue && total.Value >= 0)
                    {
                        totalCount = total.Value;
                    }
                }

                return new PagedResult<BehsazSearchResultDto>
                {
                    Items = rows,
                    TotalCount = totalCount,
                    Page = criteria.Page,
                    PageSize = criteria.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Behsaz integration search failed.");
                throw;
            }
        }

        private DynamicParameters BuildParameters(BehsazSearchCriteria criteria)
        {
            var parameters = new DynamicParameters();
            var searchValuesParameters = BuildPeoplePropertiesTable(criteria);
            if (searchValuesParameters.Rows.Count > 0)
            {
                parameters.Add(
                "@SearchValues", searchValuesParameters.AsTableValuedParameter(PeoplePropertiesTvpTypeName)
                );
            }

            parameters.Add("@TINFBuildingID", dbType: DbType.Int32, value: null);
            parameters.Add(
                "@TBASPeopleGroupID",
                criteria.TaxpayerGroup.HasValue ? (int)criteria.TaxpayerGroup.Value : null,
                DbType.Int32);
            parameters.Add("@InstaurationFilterType", criteria.InstaurationFilterType, DbType.Int32);
            parameters.Add("@InstaurationArea", criteria.Area, DbType.Int32);
            parameters.Add("@InstaurationZone", criteria.Zone, DbType.Int32);
            parameters.Add("@InstaurationBlock", criteria.Block, DbType.Int32);
            parameters.Add("@InstaurationLand", criteria.Land, DbType.Int32);
            parameters.Add("@InstaurationBuilding", criteria.Building, DbType.Int32);
            parameters.Add("@InstaurationApartment", criteria.Apartment, DbType.Int32);
            parameters.Add("@InstaurationBusiness", criteria.Business, DbType.Int32);
            parameters.Add("@ManualNumber", criteria.FileNumber, DbType.String);
            parameters.Add("@Number", criteria.SystemNumber, DbType.String);
            parameters.Add("@MainCode", criteria.RegistrationPlateMain, DbType.Decimal);
            parameters.Add("@SubCode", criteria.RegistrationPlateSub, DbType.Decimal);
            parameters.Add("@GhateCode", criteria.GhateCode, DbType.Decimal);
            parameters.Add("@Address", criteria.Address, DbType.String);
            parameters.Add("@GisIndicated", criteria.GisIndicated, DbType.Boolean);
            parameters.Add("@IsObsoleted", false, DbType.Boolean);
            parameters.Add("@InstaurationCodeStatus", dbType: DbType.Boolean, value: null);
            parameters.Add("@IsInZoningState", false, DbType.Boolean);
            parameters.Add("@StateMode", criteria.PropertyStatus ?? 1, DbType.Int32);
            var userCode = criteria.UserCode.HasValue && criteria.UserCode.Value > 0
                ? criteria.UserCode.Value
                : Math.Max(1, _settings.UserCode);
            parameters.Add("@UserCode", userCode, DbType.Int32);
            parameters.Add("@OrderBy", "ManualNumber ASC, UserName ASC", DbType.String);
            parameters.Add("@PageSize", criteria.PageSize, DbType.Int32);
            parameters.Add("@PageIndex", Math.Max(0, criteria.Page - 1), DbType.Int32);
            return parameters;
        }

        private static DataTable BuildPeoplePropertiesTable(BehsazSearchCriteria criteria)
        {
            var table = new DataTable(PeoplePropertiesTvpTypeName);
            table.Columns.Add("TBASPeopleGroupID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Family", typeof(string));
            table.Columns.Add("NationalCode", typeof(string));
            table.Columns.Add("OrganizationName", typeof(string));
            table.Columns.Add("NationalID", typeof(string));
            table.Columns.Add("RegisterNumber", typeof(string));

            if (criteria.TaxpayerGroup is null or TaxpayerGroup.Real)
            {
                AddRealTaxpayerRow(table, criteria);
            }

            if (criteria.TaxpayerGroup is null or TaxpayerGroup.Legal)
            {
                AddLegalTaxpayerRow(table, criteria);
            }

            return table;
        }

        private static void AddRealTaxpayerRow(DataTable table, BehsazSearchCriteria criteria)
        {
            if (string.IsNullOrWhiteSpace(criteria.FirstName) &&
                string.IsNullOrWhiteSpace(criteria.LastName) &&
                string.IsNullOrWhiteSpace(criteria.NationalCode))
            {
                return;
            }

            var row = table.NewRow();
            row["TBASPeopleGroupID"] = (int)TaxpayerGroup.Real;
            row["Name"] = DbValue(criteria.FirstName);
            row["Family"] = DbValue(criteria.LastName);
            row["NationalCode"] = DbValue(criteria.NationalCode);
            row["OrganizationName"] = DBNull.Value;
            row["NationalID"] = DBNull.Value;
            row["RegisterNumber"] = DBNull.Value;
            table.Rows.Add(row);
        }

        private static void AddLegalTaxpayerRow(DataTable table, BehsazSearchCriteria criteria)
        {
            if (string.IsNullOrWhiteSpace(criteria.CompanyName) &&
                string.IsNullOrWhiteSpace(criteria.RegisterNumber) &&
                string.IsNullOrWhiteSpace(criteria.NationalCompanyId))
            {
                return;
            }

            var row = table.NewRow();
            row["TBASPeopleGroupID"] = (int)TaxpayerGroup.Legal;
            row["Name"] = DBNull.Value;
            row["Family"] = DBNull.Value;
            row["NationalCode"] = DBNull.Value;
            row["OrganizationName"] = DbValue(criteria.CompanyName);
            row["NationalID"] = DbValue(criteria.NationalCompanyId);
            row["RegisterNumber"] = DbValue(criteria.RegisterNumber);
            table.Rows.Add(row);
        }

        private static object DbValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private static string BuildTaxpayerDetails(BehsazSearchResultDto row)
        {
            var parts = new List<string>();
            AddDistinctPart(parts, row.TaxpayerDetails);

            var isLegal = !string.IsNullOrWhiteSpace(row.CompanyName) ||
                !string.IsNullOrWhiteSpace(row.NationalCompanyId) ||
                row.TaxpayerGroup.Contains("حقوقی", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(row.TaxpayerGroup.Trim(), "2", StringComparison.Ordinal);

            if (isLegal)
            {
                AddDistinctPart(parts, row.CompanyName);
                AddLabeledPart(parts, "شناسه ملی / کد اقتصادی", row.NationalCompanyId);
                AddLabeledPart(parts, "شماره ثبت", row.RegisterNumber);
            }
            else
            {
                AddDistinctPart(parts, string.Join(
                    " ",
                    new[] { row.FirstName, row.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value))));
                AddLabeledPart(parts, "کد ملی", row.NationalCode);
            }

            return string.Join(" | ", parts);
        }

        private static void AddLabeledPart(List<string> parts, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                parts.Any(part => part.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            parts.Add($"{label}: {value.Trim()}");
        }

        private static void AddDistinctPart(List<string> parts, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var trimmed = value.Trim();
            if (!parts.Any(part =>
                part.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains(part, StringComparison.OrdinalIgnoreCase)))
            {
                parts.Add(trimmed);
            }
        }

        private static PagedResult<BehsazSearchResultDto> Empty(BehsazSearchCriteria criteria)
        {
            return new PagedResult<BehsazSearchResultDto>
            {
                Items = Array.Empty<BehsazSearchResultDto>(),
                TotalCount = 0,
                Page = criteria.Page,
                PageSize = criteria.PageSize
            };
        }

        private static PropertyInfo? ResolveResultProperty(Type type, string columnName)
        {
            var cleanName = NormalizeColumnName(columnName);

            return type.GetProperties()
                .FirstOrDefault(property =>
                    string.Equals(property.Name, columnName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeColumnName(property.Name), cleanName, StringComparison.OrdinalIgnoreCase) ||
                    LegacyColumnMap.TryGetValue(cleanName, out var targetPropertyName) &&
                    string.Equals(property.Name, targetPropertyName, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeColumnName(string columnName) =>
            columnName.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();

        private static readonly IReadOnlyDictionary<string, string> LegacyColumnMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["INSTAURATIONCODE"] = nameof(BehsazSearchResultDto.ZoningCode),
                ["NUMBER"] = nameof(BehsazSearchResultDto.SystemNumber),
                ["MANUALNUMBER"] = nameof(BehsazSearchResultDto.FileNumber),
                ["ARCHIVE"] = nameof(BehsazSearchResultDto.ArchiveType),
                ["STATELOCATION"] = nameof(BehsazSearchResultDto.Status),
                ["PEOPLEGROUPNAME"] = nameof(BehsazSearchResultDto.TaxpayerGroup),
                ["PEOPLEPROPERTY"] = nameof(BehsazSearchResultDto.TaxpayerDetails),
                ["PEOPLEKEY"] = nameof(BehsazSearchResultDto.TaxpayerKey),
                ["NAME"] = nameof(BehsazSearchResultDto.FirstName),
                ["FAMILY"] = nameof(BehsazSearchResultDto.LastName),
                ["NATIONALCODE"] = nameof(BehsazSearchResultDto.NationalCode),
                ["ORGANIZATIONNAME"] = nameof(BehsazSearchResultDto.CompanyName),
                ["NATIONALID"] = nameof(BehsazSearchResultDto.NationalCompanyId),
                ["REGISTERNUMBER"] = nameof(BehsazSearchResultDto.RegisterNumber),
                ["POSTCODE"] = nameof(BehsazSearchResultDto.PostalCode),
                ["ZIPCODE"] = nameof(BehsazSearchResultDto.PostalCode),
                ["USERNAME"] = nameof(BehsazSearchResultDto.User),
                ["PLAQUE"] = nameof(BehsazSearchResultDto.RegistrationPlate),
                ["MOBILE"] = nameof(BehsazSearchResultDto.Phone)
            };
    }
}
