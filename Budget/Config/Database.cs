namespace Budget.Config;
using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.Json<LanguageExt.IO>;
using static ConfigDefaults;

public static class Database
{
    public static readonly IO<string> readDbFilePath = config.Map(c => c.DbLocation);

    public static IO<Unit> setDbFilePath(string dbFilePath) =>
        from config in readDefaultConfig.Catch(_ => ConfigDefaults.ConfigData)
        let withPath = config with { DbLocation = dbFilePath }
        from text in serialize(withPath)
                    // usage of ConfigDefaults.FilePath assumes that's where readDefaultConfig reads from!
        from _1 in IO.lift(() => File.WriteAllText(FilePath, text))
        select unit;
}