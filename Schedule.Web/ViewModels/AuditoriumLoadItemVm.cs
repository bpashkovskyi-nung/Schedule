using System.Globalization;

namespace Schedule.Web.ViewModels;

public sealed class AuditoriumLoadItemVm
{
    public required string RoomName { get; init; }
    public required string BlockName { get; init; }
    public required double Percent { get; init; }
    public required int ActualPairs { get; init; }
    public required int PossiblePairs { get; init; }

    public string BlockDisplayName
    {
        get
        {
            string blockName = BlockName.Trim();
            if (!int.TryParse(blockName, NumberStyles.None, CultureInfo.InvariantCulture, out int blockNumber))
            {
                return BlockName;
            }

            string noun = blockNumber % 100 is >= 11 and <= 14
                ? "корпусів"
                : (blockNumber % 10) switch
                {
                    1 => "корпус",
                    2 or 3 or 4 => "корпуси",
                    _ => "корпусів",
                };

            return $"{blockName} {noun}";
        }
    }

    public string HeatColor => Percent switch
    {
        <= 25 => "#20c997",
        <= 50 => "#ffc107",
        <= 75 => "#fd7e14",
        _ => "#dc3545",
    };
}
