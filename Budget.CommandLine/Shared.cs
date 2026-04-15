using Budget.Config;
using LanguageExt;

namespace Budget.CommandLine;

public static class Shared
{
    internal static readonly System.CommandLine.Option<bool> SetDb = new("--set-db")
    {
        Required = false
    };

    public static IO<Unit> maybeSetDbPath(bool shouldSetDb, string dbString) =>
        shouldSetDb ? Database.setDbFilePath(dbString) : Prelude.unitIO;
}