using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using static LanguageExt.Prelude;
using static LanguageExt.Json<LanguageExt.IO>;

namespace Budget.Config;

public static class ConfigDefaults
{
    public const string DataFileName = "budget.json";
    public const string DatabaseFileName = "BudgetLiteDb.db";
    private static readonly string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static readonly string FilePath = Path.Join(ApplicationData, DataFileName);
    public static readonly string DatabasePath = Path.Join(ApplicationData, DatabaseFileName);

    public static readonly ConfigData ConfigData = new(DatabasePath, None);

    private static readonly IO<ConfigData> config = IO
        .lift(() => File.ReadAllText(FilePath))
        .Bind(deserialize<ConfigData>);
    
    private static readonly IO<ConfigData> ConfigWithWarningInternal = +config
        .Catch(e => 
            IO.lift(() =>
            {
                System.Console.WriteLine(
                    $"Error reading or parsing config file {FilePath}: '{e.Message}'{Environment.NewLine}" +
                    $"Using default config {ConfigData} instead, data may not be read or saved as expected");
                return ConfigData;
            }));

    private static readonly Atom<Option<ConfigData>> _cachedConfigData = Atom<Option<ConfigData>>(None);
    
    public static readonly IO<ConfigData> configWithWarning = IO.lift(() =>
        _cachedConfigData.Swap(opt =>
            opt.Match(v => v, () => ConfigWithWarningInternal.Run()))
            .ValueUnsafe());
    
    public static IO<Unit> setConfig(string? DbLocation = null, Option<CsvConfigData> Csv = default) =>
        from config in configWithWarning
        let withPath = config with
        {
            DbLocation = DbLocation ?? config.DbLocation,
            Csv = Csv.Match(csv => csv, config.Csv)
        }
        from text in serialize(withPath)
        // usage of ConfigDefaults.FilePath assumes that's where readDefaultConfig reads from!
        from _1 in IO.lift(() => File.WriteAllText(FilePath, text))
        select unit;
}