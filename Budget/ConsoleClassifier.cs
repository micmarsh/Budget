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
        from fileText in rt.FileReads.GetFileText(input.FilePath)
        let csvLines = Csv.ParseText(fileText)
        from lineItems in parseCsvLines<Eff<CsvInfo>>(csvLines).As().CoMap((Runtime _) => input)
        
        select unit;

    public static K<M, (Seq<Error> Errors, Seq<LineItem> lineItems)> parseCsvLines<M>(CsvLines lines)
        where M : Monad<M>, Readable<M, CsvInfo>
        => lines.Lines.Map(line => line switch
        {
            ValidCsvLine validCsvLine => throw new NotImplementedException(),
            LongCsvLine longCsvLine => throw new NotImplementedException(),
            ShortCsvLine shortCsvLine => throw new NotImplementedException(),
            _ => throw patternMatchError(line)
        });
}

public record CsvInfo(string FilePath, string DescriptionField, string AmountField, string DateField);