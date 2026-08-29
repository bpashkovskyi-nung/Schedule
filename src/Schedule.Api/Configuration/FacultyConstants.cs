namespace Schedule.Api.Configuration;

internal static class FacultyConstants
{
    private static readonly Dictionary<string, int> FacultyIdMap = new(StringComparer.Ordinal)
    {
        ["Інститут гуманітарної підготовки та державного управління"] = 1001,
        ["Інститут нафтогазової інженерії"] = 1002,
        ["Інститут інженерної механіки та робототехніки"] = 1003,
        ["Факультет природничих наук"] = 1006,
        ["Інститут економіки та менеджменту"] = 1007,
        ["Інститут післядипломної освіти"] = 1011,
        ["Відділ аспірантури і докторантури"] = 1012,
        ["Центр міжнародної освіти"] = 1013,
        ["Факультет інформаційних технологій"] = 1015,
        ["Факультет автоматизації та енергетики"] = 1016,
        ["Інститут архітектури та будівництва 'ІФНТУНГ-ДонНАБА'"] = 1017,
    };

    public static int GetFacultyId(string? facultyName)
    {
        if (string.IsNullOrWhiteSpace(facultyName))
        {
            return 0;
        }

        string normalized = NormalizeFacultyName(facultyName);
        KeyValuePair<string, int> entry = FacultyIdMap.FirstOrDefault(pair =>
            NormalizeFacultyName(pair.Key) == normalized);

        return entry.Value;
    }

    public static string NormalizeFacultyName(string name) =>
        name.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('\u2032', '\'').Replace('`', '\'').Trim();
}
