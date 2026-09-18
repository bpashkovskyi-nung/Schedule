using Schedule.Web.ViewModels;
using Xunit;

namespace Schedule.Web.UnitTests;

public sealed class AuditoriumLoadItemVmTests
{
    [Theory]
    [InlineData("1", "1 корпус")]
    [InlineData("2", "2 корпус")]
    [InlineData("3", "3 корпус")]
    [InlineData("4", "4 корпус")]
    [InlineData("5", "5 корпус")]
    [InlineData("11", "11 корпус")]
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
