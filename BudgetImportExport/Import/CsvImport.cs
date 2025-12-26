using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;

namespace BudgetImportExport.Import;

public class CsvImport : IImport<ClassificationDoc>, IBulkImport<ClassificationDoc>
{
    private readonly string _filePath;

    public CsvImport(string filePath)
    {
        _filePath = filePath;
        stream = new Lazy<StreamWriter>(() =>
        {
            var value = new StreamWriter(filePath);
            value.WriteLine(FlatClassification.Header);
            return value;
        });
    }

    private readonly Lazy<StreamWriter> stream;


    public Unit Write(ClassificationDoc doc)
    {
        var rows = ConvertToRows(doc);

        foreach (var row in rows)
        {
            stream.Value.WriteLine(row.ToString());
        }

        return Unit.Default;
    }

    private static Iterator<FlatClassification> ConvertToRows(ClassificationDoc doc) =>
        doc.Record switch
        {
            Categorized({ Value: var category }, var (desc, amount, date)) =>
                Iterator.singleton(new FlatClassification(doc.Id.ToString(), date, category, desc, amount)),
            SubClassifications(var children, var (desc, _, date)) =>
                Iterator.from(children.Map(c =>
                    new FlatClassification(doc.Id.ToString(), date, c.Category.Value, desc, c.Amount))),
            _ => throw Utilities.patternMatchError(doc.Record)
        };

    public void Dispose()
    {
        if (stream.IsValueCreated)
        {
            stream.Value.Flush();
            stream.Value.Close();
            stream.Value.Dispose();
        }
    }

    public Unit WriteAll(Seq<ClassificationDoc> items)
    {
        File.WriteAllText(_filePath, string.Join(Environment.NewLine, 
            FlatClassification.Header.Cons(items
                .Bind(item => ConvertToRows(item).ToSeq())
                .Map(row => row.ToString())
                .AsEnumerable()
            )));
        return Unit.Default;
    }
}