namespace Schedule.Contracts.Common;

public sealed record DateRangeDto(
    DateOnly From,
    DateOnly To);
