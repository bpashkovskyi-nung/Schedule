namespace Schedule.Web.Configuration;

public sealed class LegacyFeatureOptions
{
    public const string SectionName = "Features";

    public bool GenerateIcalFromJson { get; set; }
}
