namespace Schedule.Web.Database;

internal sealed class ScheduleBuildingRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ScheduleRoomRecord> Rooms { get; set; } = [];
}
