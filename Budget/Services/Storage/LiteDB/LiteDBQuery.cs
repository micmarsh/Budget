using LanguageExt;
using LiteDB;

namespace Budget.Services.Storage.LiteDB;

public class LiteDBQuery(LiteDatabase db) : IDisposable, IClassificationQuery
{
    public static LiteDBQuery From(string connection) => new (new LiteDatabase(connection));
    public static LiteDBQuery From(Stream stream) => new (new LiteDatabase(stream));
    
    public void Dispose() => db.Dispose();

    public Source<Classification> GetDateRange(DateTime start, DateTime end) =>
        Source.lift(db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc)).Query()
            .Where(c => c.Record.LineItem.Date >= start)
            .Where(c => c.Record.LineItem.Date <= end)
            .Select(c => c.Record)
            .ToEnumerable());
}