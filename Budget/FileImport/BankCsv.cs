using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Budget.FileImport;

public static class BankCsv
{
    public static IO<ParseResults> parseBankCsv(FileInfo file, string descF, string amountF, string dateF, string backupF) =>
        Csv.StreamLines(file.FullName)
            .Map(parseCsvLine(new CsvInput(descF, amountF, dateF, backupF)))
            .ReduceIO(ParseResults.Empty, handleLineItemResult);
    
    public static Func<CsvLine, Fin<LineItem>> parseCsvLine(CsvInput input) => line =>
        (getDescription(input, line), getAmount(input, line), getDate(input, line))
        .Apply((desc, amount, date) => new LineItem(desc, amount, date))
        .As().ToFin();

    private static Validation<Error, DateTime> getDate(CsvInput input, CsvLine line) => 
        line.Fields.Find(input.DateField)
            .Bind(parseDateTime)
            .ToValidation(Error.New($"Line {line.LineNumber} has an invalid date field"));

    private static Validation<Error, decimal> getAmount(CsvInput input, CsvLine line) => 
        line.Fields.Find(input.AmountField)
            .Bind(parseDecimal)
            .ToValidation(Error.New($"Line {line.LineNumber} missing or invalid amount field"));
    
    private static Validation<Error, string> getDescription(CsvInput input, CsvLine line) =>
        line.Fields.Find(input.DescriptionField)
            .Filter(desc => ! string.IsNullOrWhiteSpace(desc))
            .Catch((Unit _) => line.Fields.Find(input.BackupDescription)).As()
            .Filter(desc => ! string.IsNullOrWhiteSpace(desc))
            .ToValidation(Error.New($"Line {line.LineNumber} missing description field"));
    
    //todo utilize some nice, re-usable method like instead of this internal thing (there's currently a couple in "User Classification")
    // also need an error or warning version of this, does/could that exist in CommandLine LanguageExt library?
    private static IO<Unit> log(object? obj) => IO.lift(() => System.Console.WriteLine(obj));
    
    private static IO<Reduced<ParseResults>> handleLineItemResult(ParseResults state, Fin<LineItem> input) =>
        input.Match(
            lineItem => Reduced.ContinueIO(state.Add(lineItem)),
            e => log(e.Message) >> Reduced.ContinueIO(state)
        );
}

public readonly record struct ParseResults(Seq<LineItem> LineItems, DateTime MinDate, DateTime MaxDate)
{
    public ParseResults Add(LineItem lineItem) => new(
        LineItems.Add(lineItem),
        lineItem.Date < MinDate ? lineItem.Date : MinDate,
        lineItem.Date > MaxDate ? lineItem.Date : MaxDate
    );

    public static readonly ParseResults Empty =
        new (LanguageExt.Seq<LineItem>.Empty, DateTime.MaxValue, DateTime.MinValue);
};

public readonly record struct CsvInput(string DescriptionField, string AmountField, string DateField, string BackupDescription);