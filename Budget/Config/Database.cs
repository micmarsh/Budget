using System.Text.Json;

namespace Budget.Config;
using LanguageExt;
using static LanguageExt.Prelude;

public static class Database
{
    //todo move these to non-"Database" class
    public const string ConfigDirectory = "~/.budget";
    public const string DataFile = "config";

    public record ConfigData(string DbLocation); //todo csv parsing info(?) maybe if you want to save it

    public static readonly IO<ConfigData> config = IO.lift(() =>
    {
        Directory.CreateDirectory(ConfigDirectory);
        var file = File.Open(Path.Join(ConfigDirectory, DataFile), FileMode.OpenOrCreate);
        return JsonSerializer.Deserialize<ConfigData>(file) ??
               throw new Exception("Configuration data deserialized to null");
    });

    // begin database stuff
    
    public const string DefaultDatabaseFile = "BudgetLiteDb.db";

    public static readonly IO<string> readDbFilePath = config.Map(c => c.DbLocation);

}