using System.CommandLine.Parsing;
using Budget.Config;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static CommandLine.Immutable.Parsing;

namespace Budget.CommandLine;

public static class FileImport
{
    private static System.CommandLine.Option<FileInfo> InputFile = new("--file", "-f")
    {
        Description = "The csv file to import",
        Required = true
    };

    private static Func<ArgumentResult, T> GetDefaultValueFactory<T>(string argName, Func<CsvConfigData, T> getString) =>
        factory(arg => arg.Tokens.Match(
            () => Config.Csv.getConfig
                .RunSafe()
                //todo see how this works when attempting to print default values? ugly?
                .Bind(c => c.Map(getString).ToFin(Error.New($"{argName} is required but not provided, " +
                                                            $"and there was an error reading default from config"))),
            (token, _) => arg.GetValueOrDefault<T>()
        ));
    
    private static System.CommandLine.Option<string> DescriptionField = new("--description-field", "-desc")
    {
        Description = "The column name in the provided CSV to use as the description/label for the transaction.",
        DefaultValueFactory = GetDefaultValueFactory("--description-field", c => c.DescriptionField)
    };
    
    private static System.CommandLine.Option<string> AmountField = new("--amount-field", "-am")
    {
        Description = "The column name in the provided CSV to use as the dollar amount for the transaction (it's presumed positive/negative amounts reflect income/spending)",
        DefaultValueFactory = GetDefaultValueFactory("--amount-field", c => c.AmountField)
    };
    
    private static System.CommandLine.Option<string> DateField = new("--date-field", "-date")
    {
        Description = $"The column name in the provided CSV to use as the date and time of the transaction, strings will be parsed with {nameof(Prelude.parseDateTime)} with no additional arguments or configuration.",
        DefaultValueFactory = GetDefaultValueFactory("--date-field", c => c.DateField)
    };
    
    private static System.CommandLine.Option<string> BackupDescription = new("--backup-description", "-bd")
    {
        Description = $"An alternative to {DescriptionField.Name} for the app to use if a particular row value is null or whitespace",
        DefaultValueFactory = GetDefaultValueFactory("--backup-description", c => c.BackupDescriptionField)
    };

    private static System.CommandLine.Option<bool> SetCsvConfig = new("--set-csv")
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
            .AddOption(DescriptionField)
            .AddOption(AmountField)
            .AddOption(DateField)
            .AddOption(BackupDescription)
            .AddOption(Shared.SetDb)
            .AddOption(SetCsvConfig)
            .WithAction((dbString, descF, amountF, dateF, backupF, setDb, setCsv) => 
                IO.lift(() => System.Console.WriteLine($"TODO: actual import app! current args {dbString}, {setDb}")) 
                >> Shared.maybeSetDbPath(setDb, dbString));
}