using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace Budget.Migration.Export;

public class CsvExport(string filePath) : IExport
{
    public Source<FlatClassification> ExportClassifications() =>
        Source.lift(
            Csv.ParseFile(filePath)
                .Run()
                .Lines
                .Filter(line => line is ValidCsvLine)
                .Map(line => new FlatClassification(
                    line.Fields[nameof(FlatClassification.DbId)],
                    DateTime.Parse(line.Fields[nameof(FlatClassification.Date)]),
                    line.Fields[nameof(FlatClassification.Category)],
                    line.Fields[nameof(FlatClassification.Description)],
                    decimal.Parse(line.Fields[nameof(FlatClassification.Amount)])
        )));

    public void Dispose()
    {
        // TODO release managed resources here
    }
}