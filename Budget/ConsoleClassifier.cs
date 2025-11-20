using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;
using static Budget.Utilities;

namespace Budget;

public static class ConsoleClassifier
{
    public static Eff<Runtime, Unit> Create(CsvInfo input) =>
        from rt in askE<Runtime>()
        from csvLines in rt.FileReads.GetFileText(input.FilePath).Map(Csv.ParseText)
        let parsedCsv = parseCsvLines(input, csvLines)
        from lastSaved in rt.Storage.GetLatest()
        let lineItems = fastForward(lastSaved, parsedCsv.LineItems)
        from _1 in UserClassification.classifyAll(lastSaved.Categories, lineItems)
        select unit;

    private static (Seq<Error> Errors, Seq<LineItem> LineItems) parseCsvLines(CsvInfo info, CsvLines lines)
        => lines.Lines.Map(line =>   
                (line.Fields.Find(info.DescriptionField).ToValidation(Error.New($"Line {line.LineNumber} missing description field")), 
                    line.Fields.Find(info.AmountField).Bind(parseDecimal).ToValidation(Error.New($"Line {line.LineNumber} missing or invalid amount field")),
                    line.Fields.Find(info.DateField).Bind(parseDateTime).ToValidation(Error.New($"Line {line.LineNumber} has an invalid date field")))
                .Apply((desc, amount, date) => new LineItem(desc, amount, date)))
            .Map(v => v.As().ToEither())
            .Partition();
    
    private static Seq<LineItem> fastForward(ClassificationsState lastSaved, Seq<LineItem> lineItems)
    {
        var alreadyClassified = lastSaved.OnDate.Map(c => c.LineItem);
        return lineItems.Filter(l => l.Date >= lastSaved.Date)
            .Filter(l =>
            {
                if (l.Date > lastSaved.Date) return true;
                return ! alreadyClassified.Contains(l);
            });
    }
}

public record CsvInfo(string FilePath, string DescriptionField, string AmountField, string DateField);