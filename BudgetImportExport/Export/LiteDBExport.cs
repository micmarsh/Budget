using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace BudgetImportExport.Export;

public class LiteDBExport(string DbFilePath) : IExport
{
    private LiteDatabase db = new (DbFilePath);
    
    static LiteDBExport() => RegisterSerializers.Register();
    
    // todo maybe Seq can be some kind of Streaming construct?
    public Iterator<ClassificationDoc> ExportClassifications()
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        return Iterator.from(coll.Find(_ => true));
    }

    public void Dispose() => db.Dispose();
}