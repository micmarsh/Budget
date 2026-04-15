
namespace Budget.Config;
using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.Json<LanguageExt.IO>;

public static class ConfigDefaults
{
    public const string DataFile = "budget_config.json";
    public const string DatabaseFile = "BudgetLiteDb.db";
    private static readonly string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static readonly string FilePath = Path.Join(ApplicationData, DataFile);
    public static readonly string DatabasePath = Path.Join(ApplicationData, DatabaseFile);

    public static readonly ConfigData ConfigData = new(DatabasePath);
}

public readonly record struct ConfigData(string DbLocation); //todo csv parsing info(?) maybe if you want to save it

public static class Database
{
    //todo move these to non-"Database" class

    public static readonly IO<ConfigData> config = +IO.lift(() => File.ReadAllText(ConfigDefaults.DatabaseFile))
        .Bind(deserialize<ConfigData>)
        .Catch(e => IO.lift(() =>
        {
            System.Console.WriteLine(
                $"Error reading or parsing config file {ConfigDefaults.FilePath}: '{e.Message}'{Environment.NewLine}" +
                $"Using default config with databse location {ConfigDefaults.DatabasePath} instead, data may not be read or saved as expected");
            return ConfigDefaults.ConfigData;
        }));

    // begin database stuff
    
    public static readonly IO<string> readDbFilePath = config.Map(c => c.DbLocation);

}