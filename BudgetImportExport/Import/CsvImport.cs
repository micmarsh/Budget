using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Import;

public class CsvImport : IImport
{

    public CsvImport(string filePath)
    {
        stream = new StreamWriter(filePath);
        stream.WriteLine(FlatClassification.Header);
    }

    private readonly StreamWriter stream;


    public Unit Write(ClassificationDoc doc)
    {
        var rows = doc.Record switch
        {
            Categorized({ Value: var category }, var (desc, amount, date)) =>
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, category, desc, amount)),
            SubClassifications(var children, var (desc, _, date)) =>
                Iterator.from(children.Map(c =>
                    new FlatClassification(doc.Id.ToString(), date, c.Category.Value, desc, c.Amount))),
            _ => throw Utilities.patternMatchError(doc.Record)
        };

        foreach (var row in rows)
        {
            stream.WriteLine(row.ToString());
        }

        return Unit.Default;
    }

    public void Dispose()
    {
        stream.Flush();
        stream.Close();
        stream.Dispose();
    }
}