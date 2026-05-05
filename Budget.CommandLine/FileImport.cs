using System.CommandLine.Parsing;
using Budget.Config;
using Budget.FileImport;
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

    private static Func<ArgumentResult, Fin<T>> GetDefaultValueFactory<T>(string argName, Func<CsvConfigData, T> getString) =>
        arg => arg.Tokens switch {
            [] => Config.Csv.getConfig
                .RunSafe()
                //todo see how this works when attempting to print default values? ugly?
                .Bind(c => c.Map(getString).ToFin(Error.New($"{argName} is required but not provided, " +
                                                            $"and there was an error reading default from config"))),
             _ => arg.GetValueOrDefault<T>()
        };
    
    private static System.CommandLine.Option<string> DescriptionField = new("--description-field", "-desc")
    {
        Description = "The column name in the provided CSV to use as the description/label for the transaction.",
        DefaultValueFactory = factory(GetDefaultValueFactory("--description-field", c => c.DescriptionField))
    };
    
    private static System.CommandLine.Option<string> AmountField = new("--amount-field", "-am")
    {
        Description = "The column name in the provided CSV to use as the dollar amount for the transaction (it's presumed positive/negative amounts reflect income/spending)",
        DefaultValueFactory = factory(GetDefaultValueFactory("--amount-field", c => c.AmountField))
    };
    
    private static System.CommandLine.Option<string> DateField = new("--date-field", "-date")
    {
        Description = $"The column name in the provided CSV to use as the date and time of the transaction, strings will be parsed with {nameof(Prelude.parseDateTime)} with no additional arguments or configuration.",
        DefaultValueFactory = factory(GetDefaultValueFactory("--date-field", c => c.DateField))
    };
    
    private static System.CommandLine.Option<Option<string>> BackupDescription = OptionalInput.Opt<string>("--backup-description", "-bd")
        .With(Description: $"An alternative to {DescriptionField.Name} for the app to use if a particular row value is null or whitespace");


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
            .AddOption(InputFile)
            .AddOption(Shared.DbString)
            .AddOption(DescriptionField)
            .AddOption(AmountField)
            .AddOption(DateField)
            .AddOption(BackupDescription)
            .AddOption(Shared.SetDb)
            .AddOption(SetCsvConfig)
            .WithAction((file, dbString, descF, amountF, dateF, backupF, setDb, setCsv) => 
                RunImport(file, dbString, descF, amountF, dateF, backupF) >>
                Shared.maybeSetDbPath(setDb, dbString) >>
                (setCsv ? 
                    ConfigDefaults.setConfig(Csv: new CsvConfigData(descF, amountF, dateF, backupF)) : 
                    Prelude.unitIO));

    //todo utilize some nice, re-usable method like instead of this internal thing (there's currently a couple in "User Classification")
    // also need an error or warning version of this, does/could that exist in CommandLine LanguageExt library?
    private static IO<Unit> log(object? obj) => IO.lift(() => System.Console.WriteLine(obj));
    
    private static IO<Unit> RunImport(FileInfo file, FileInfo dbString, string descF, string amountF, string dateF, 
        Option<string> backupF)
        =>
            BankCsv.parseBankCsv(file, descF, amountF, dateF, backupF)
            .Bind(results => log("TADA: ") >> log(results))
            .Map(_ => Prelude.unit);
}