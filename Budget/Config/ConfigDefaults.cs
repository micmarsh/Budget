using LanguageExt;
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

    public static readonly IO<ConfigData> config = IO
        .lift(() => File.ReadAllText(FilePath))
        .Bind(deserialize<ConfigData>);
    
    public static readonly IO<ConfigData> configWithWarning = +config
        .Catch(e => 
            //todo some kind of caching/memoization to prevent multiple prints?
            IO.lift(() =>
            {
                System.Console.WriteLine(
                    $"Error reading or parsing config file {FilePath}: '{e.Message}'{Environment.NewLine}" +
                    $"Using default config {ConfigData} instead, data may not be read or saved as expected");
                return ConfigData;
            }));
    
    public static IO<Unit> setConfig(string? DbLocation = null, Option<CsvConfigData> Csv = default) =>
        from config in config.Catch(_ => ConfigData)
        let withPath = config with
        {
            DbLocation = DbLocation ?? config.DbLocation,
            Csv = Csv.Match(csv => csv, config.Csv)
        }
        from text in serialize(withPath)
        // usage of ConfigDefaults.FilePath assumes that's where readDefaultConfig reads from!
        from _1 in IO.lift(() => File.WriteAllText(FilePath, text))
        select unit;

    // private static IO<CsvConfigData> createCsvConfig(string? descriptionField, string? amountField,
    //     string? dateField, string? backupDescriptionField) =>
    //     (Optional(descriptionField), Optional(amountField), Optional(dateField), Optional(backupDescriptionField))
    //     .Apply((d, a, dt, b) => new CsvConfigData(d, a, dt, b))
    //     .As()
    //     .Match(Some: IO.lift, None: () => IO.fail<CsvConfigData>(Error.New()));
}