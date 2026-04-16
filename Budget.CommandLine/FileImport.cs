using CommandLine.Immutable;
using LanguageExt;

namespace Budget.CommandLine;

public static class FileImport
{
    private static System.CommandLine.Option<FileInfo> InputFile = new("--file", "-f")
    {
        Description = "The csv file to import",
        Required = true
    };

    private static System.CommandLine.Option<string> DescriptionField = new("--description-field", "-desc")
    {
        Description = "The column name in the provided CSV to use as the description/label for the transaction."
    };
    
    private static System.CommandLine.Option<string> AmountField = new("--amount-field", "-am")
    {
        Description = "The column name in the provided CSV to use as the dollar amount for the transaction (it's presumed positive/negative amounts reflect income/spending)",
    };
    
    private static System.CommandLine.Option<string> DateField = new("--date-field", "-date")
    {
        Description = $"The column name in the provided CSV to use as the date and time of the transaction, strings will be parsed with {nameof(Prelude.parseDateTime)} with no additional arguments or configuration."
    };
    
    private static System.CommandLine.Option<string> BackupDescription = new("--backup-description", "-bd")
    {
        Description = $"An alternative to {DescriptionField.Name} for the app to use if a particular row value is null or whitespace"
    };

    private static System.CommandLine.Option<bool> SetCsvConfig = new("--set-csv", "-sc")
    {
        Description = $"Use to save the csv columns names specified on other arguments to configuration to be automatically used " +
                      $"without manually specifying any.",
        Required = false
    };
    
    public static readonly ICmd Command = 
        Cmd.New("import", "Import a CSV file (typically exported from your bank) " + 
                          "into the database to be classified later. Will automatically run " + 
                          "(TODO: link actual 'clean cmd.Name') to deal with potential duplicates after")
            .AddOption(Shared.DbString)
            .AddOption(Shared.SetDb)
            .WithAction((dbString, setDb) => 
                IO.lift(() => System.Console.WriteLine($"TODO: actual import app! current args {dbString}, {setDb}")) 
                >> Shared.maybeSetDbPath(setDb, dbString));
}