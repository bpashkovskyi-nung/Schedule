namespace Schedule.Web.Database;

internal sealed class ScheduleRoomRecord
{
    public long Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int BuildingId { get; set; }

    public int SortOrder { get; set; }

    public ScheduleBuildingRecord? Building { get; set; }
}
