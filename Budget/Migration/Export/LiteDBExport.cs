using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace Budget.Migration.Export;

public class LiteDBExport(string DbFilePath) : IExport
{
    private LiteDatabase db = new (DbFilePath);
    
    static LiteDBExport() => RegisterSerializers.Register();
    
    public Source<FlatClassification> ExportClassifications()
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        return Source.lift(coll.Find(_ => true).AsIterable().Bind(LiteDbUtils.ConvertToRows));
    }

    public void Dispose() => db.Dispose();
}