namespace Budget.Config;
using LanguageExt;
using static ConfigDefaults;

//todo probably delete this, excessive abstraction at this point?
public static class Database
{
    public static readonly IO<string> readDbFilePath = configWithWarning.Map(c => c.DbLocation);

    public static IO<Unit> setDbFilePath(FileInfo dbFilePath) => setConfig(DbLocation: dbFilePath.FullName);
}