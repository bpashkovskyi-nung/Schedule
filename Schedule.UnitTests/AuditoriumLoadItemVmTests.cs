using Schedule.Web.ViewModels;
using Xunit;

namespace Schedule.UnitTests;

public sealed class AuditoriumLoadItemVmTests
{
    [Theory]
    [InlineData("1", "1 корпус")]
    [InlineData("2", "2 корпуси")]
    [InlineData("5", "5 корпусів")]
    [InlineData("11", "11 корпусів")]
    [InlineData("21", "21 корпус")]
    [InlineData("1(гараж)", "1(гараж)")]
    public void BlockDisplayName_FormatsNumericBuildings(string blockName, string expected)
    {
        AuditoriumLoadItemVm item = new()
        {
            RoomName = "1.412.ауд.",
            BlockName = blockName,
            Percent = 0,
            ActualPairs = 0,
            PossiblePairs = 0,
        };

        Assert.Equal(expected, item.BlockDisplayName);
    }
}
