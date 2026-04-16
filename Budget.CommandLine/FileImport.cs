using CommandLine.Immutable;
using LanguageExt;

namespace Budget.CommandLine;

public static class FileImport
{
    public static readonly ICmd Command = 
        Cmd.New("import", "Import a CSV file (typically exported from your bank) " + 
                          "into the database to be classified later. Will automatically run " + 
                          "(TODO: link actual 'clean cmd.Name') to deal with potential duplicates after")
            .AddOption(Shared.DbString)
            .AddOption(Shared.SetDb)
            .WithAction((dbString, setDb) => IO.lift(
                () => System.Console.WriteLine($"TODO: actual import app! current args {dbString}, {setDb}")));
    
    
}