using Budget.Config;
using LanguageExt;
using static CommandLine.Immutable.Parsing;

namespace Budget.CommandLine;

public static class Shared
{
    public static readonly System.CommandLine.Option<FileInfo> DbString = new("-db")
    {
        Description = "Database file to use (currently LiteDb only, possible for forseeable future)",
        DefaultValueFactory = factory(_ => Database.readDbFilePath
            .Map(str => new FileInfo(str))
            .RunSafe()),
        Required = false
    };
    
    internal static readonly System.CommandLine.Option<bool> SetDb = new("--set-db")
    {
        Description = $"Use to save the value specified in `{DbString.Name}` to configuration to be automatically used " +
                      $"without specifying, for this or any other command that utilizes `{DbString.Name}`",
        Required = false
    };

    public static IO<Unit> maybeSetDbPath(bool shouldSetDb, FileInfo dbString) =>
        shouldSetDb ? Database.setDbFilePath(dbString) : Prelude.unitIO;
}