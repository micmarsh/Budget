using LanguageExt.UnsafeValueAccess;

namespace Budget.Config;
using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.Json<LanguageExt.IO>;

//todo move these into own file?
public static class ConfigDefaults
{
    public const string DataFileName = "budget.json";
    public const string DatabaseFileName = "BudgetLiteDb.db";
    private static readonly string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static readonly string FilePath = Path.Join(ApplicationData, DataFileName);
    public static readonly string DatabasePath = Path.Join(ApplicationData, DatabaseFileName);

    public static readonly ConfigData ConfigData = new(DatabasePath);
}

public readonly record struct ConfigData(string DbLocation); //todo csv parsing info(?) maybe if you want to save it

public static class Database
{
    //todo move these to non-"Database" class
    private static readonly IO<ConfigData> readDefaultConfig = IO
        .lift(() => File.ReadAllText(ConfigDefaults.FilePath))
        .Bind(deserialize<ConfigData>);
    
    public static readonly IO<ConfigData> config = +readDefaultConfig
        .Catch(e => 
            //todo some kind of caching/memoization to prevent multiple prints
        IO.lift(() =>
        {
            System.Console.WriteLine(
                $"Error reading or parsing config file {ConfigDefaults.FilePath}: '{e.Message}'{Environment.NewLine}" +
                $"Using default config with databse location {ConfigDefaults.DatabasePath} instead, data may not be read or saved as expected");
            return ConfigDefaults.ConfigData;
        }));

    
    // begin database stuff
    
    public static readonly IO<string> readDbFilePath = config.Map(c => c.DbLocation);

    public static IO<Unit> setDbFilePath(string dbFilePath) =>
        from config in config
        let withPath = config with { DbLocation = dbFilePath }
        from text in serialize(withPath)
                    // usage of ConfigDefaults.FilePath assumes that's where readDefaultConfig reads from!
        from _1 in IO.lift(() => File.WriteAllText(ConfigDefaults.FilePath, text))
        select unit;
}