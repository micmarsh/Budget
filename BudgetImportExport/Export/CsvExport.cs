using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace BudgetImportExport.Export;

public class CsvExport(string filePath) : IExport
{
    public Iterator<ClassificationDoc> ExportClassifications() =>
        Iterator.from(
            Csv.ParseFile(filePath)
                .Run()
                .Lines
                .GroupBy(line => line.Fields[nameof(FlatClassification.DbId)])
                .Select(g => g.Count() == 1 ? getCategorized(g.First()) : getSubclassifications(g.AsEnumerable()))
        );

    private ClassificationDoc getSubclassifications(IEnumerable<CsvLine> lines)
    {
        var line = lines.First();
        var dateTime = DateTime.Parse(line.Fields[nameof(FlatClassification.Date)]);
        return new ClassificationDoc(new ObjectId(line.Fields[nameof(FlatClassification.DbId)]),
            dateTime,
            new SubClassifications(
                Prelude.toSeq(lines.Select(l => new SubCategorized(
                    new Category(l.Fields[nameof(FlatClassification.Category)]),
                    decimal.Parse(l.Fields[nameof(FlatClassification.Amount)])
                    ))),
                new LineItem(line.Fields[nameof(FlatClassification.Description)],
                    decimal.Parse(line.Fields[nameof(FlatClassification.Amount)]),
                    dateTime)
            ));
    }

    private ClassificationDoc getCategorized(CsvLine line)
    {
        var dateTime = DateTime.Parse(line.Fields[nameof(FlatClassification.Date)]);
        return new ClassificationDoc(new ObjectId(line.Fields[nameof(FlatClassification.DbId)]),
            dateTime,
            new Categorized(
                new Category(line.Fields[nameof(FlatClassification.Category)]),
                new LineItem(line.Fields[nameof(FlatClassification.Description)],
                    decimal.Parse(line.Fields[nameof(FlatClassification.Amount)]),
                    dateTime)
            ));
    }


    public void Dispose()
    {
        // TODO release managed resources here
    }
}