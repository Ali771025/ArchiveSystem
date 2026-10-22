namespace ArchiveSystem.Application.Features.Behsaz.Models
{
    public class BehsazRecordDto
    {
        public string Number { get; init; } = string.Empty;
        public string ManualNumber { get; init; } = string.Empty;
        public bool IsApartment { get; init; }
        public string StateLocation { get; init; } = string.Empty;
        public string InstaurationCode { get; init; } = string.Empty;
        public string PeopleProperty { get; init; } = string.Empty;
        public string Mobile { get; init; } = string.Empty;
        public string PeopleKey { get; init; } = string.Empty;
        public int? TBASPeopleGroupID { get; init; }
        public string PeopleGroupName { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Plaque { get; init; } = string.Empty;
        public string Archive { get; init; } = string.Empty;
        public int? TBASZoneID { get; init; }
        public string UserName { get; init; } = string.Empty;
    }
}
