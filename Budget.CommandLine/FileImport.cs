using System.CommandLine.Parsing;
using Budget.Config;
using Budget.FileImport;
using Budget.Migration;
using Budget.Migration.Import;
using Budget.Services.Storage.LiteDB;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using LiteDB;
using static CommandLine.Immutable.Parsing;
using static LanguageExt.Prelude;

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
    
    private static System.CommandLine.Option<string> BackupDescription = new System.CommandLine.Option<string>("--backup-description", "-bd")
        .With(Description: $"An alternative to {DescriptionField.Name} for the app to use if a particular row value is null or whitespace",
            DefaultValueFactory: GetDefaultValueFactory("--backup-description", c => c.BackupDescriptionField)
                >> (fin => +fin.Catch("")));

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
                maybeSetConfig(setDb, setCsv, dbString, descF, amountF, dateF, backupF));

    private static IO<Unit> RunImport(FileInfo file, FileInfo dbString, string descF, string amountF, string dateF, string backupF)
        => from csvResults in BankCsv.parseBankCsv(file, descF, amountF, dateF, backupF) 
            let importer = new LiteDBImport(dbString.Name)
           from _ in importer.WriteAll(LineItemsToImportable(csvResults.LineItems))
           select unit;

    private static Seq<FlatClassification> LineItemsToImportable(Seq<LineItem> lineItems) =>
        lineItems
            .Map(lineItem => new UnCategorized(lineItem))
            .Map(c => ClassificationDoc.NewAdd(ObjectId.NewObjectId(), DateTime.Now, c))
            .Bind(c => LiteDbUtils.ConvertToRows(c).ToSeq());
    
    private static IO<Unit> maybeSetConfig(bool setDb, bool setCsv, FileInfo dbString, string descF, string amountF, string dateF, string backupF) =>
        from configData in Shared.maybeSetDbPath(setDb, dbString)
        from _1 in ConfigDefaults.setConfig(configData with
        {
            Csv = setCsv ? new CsvConfigData(descF, amountF, dateF, backupF) : configData.Csv
        })
        select unit;
}