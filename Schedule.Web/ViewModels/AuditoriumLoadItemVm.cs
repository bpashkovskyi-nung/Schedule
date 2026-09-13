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
            if (!int.TryParse(blockName, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return BlockName;
            }

            return $"{blockName} корпус";
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
