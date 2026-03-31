using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Budget.Migration.Import;

public class CsvImport : IBulkImport
{
    private readonly string _filePath;

    public CsvImport(string filePath)
    {
        _filePath = filePath;
        stream = new Lazy<StreamWriter>(() =>
        {
            var value = new StreamWriter(filePath);
            value.WriteLine(FlatClassification.CsvHeader);
            return value;
        });
    }

    private readonly Lazy<StreamWriter> stream;


    public Unit Write(FlatClassification row)
    {

        stream.Value.WriteLine(row.ToString());
        

        return Unit.Default;
    }

    public void Dispose()
    {
        if (stream.IsValueCreated)
        {
            stream.Value.Flush();
            stream.Value.Close();
            stream.Value.Dispose();
        }
    }

    public IO<Unit> WriteAll(Seq<FlatClassification> items) => IO.lift(() =>
    {
        File.WriteAllText(_filePath, string.Join(Environment.NewLine,
            FlatClassification.CsvHeader.Cons(items
                .Map(row => row.ToString())
                .AsEnumerable()
            )));
        return Unit.Default;
    });
}