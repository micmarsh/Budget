using LanguageExt;
using static LanguageExt.Prelude;

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
        .lift(() => File.ReadAllText(ConfigDefaults.FilePath))
        .Bind(Json<IO>.deserialize<ConfigData>);
    
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
}