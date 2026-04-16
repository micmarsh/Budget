namespace Budget.Config;
using LanguageExt;
using static ConfigDefaults;

public static class Database
{
    public static readonly IO<string> readDbFilePath = configWithWarning.Map(c => c.DbLocation);

    public static IO<Unit> setDbFilePath(string dbFilePath) => setConfig(DbLocation: dbFilePath);
}