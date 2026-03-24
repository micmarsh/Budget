using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace BudgetMigration.Export;

public class LiteDBExport(string DbFilePath) : IExport
{
    private LiteDatabase db = new (DbFilePath);
    
    static LiteDBExport() => RegisterSerializers.Register();
    
    public Iterator<FlatClassification> ExportClassifications()
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        return Iterator.from(coll.Find(_ => true)).Bind(LiteDbUtils.ConvertToRows);
    }

    public void Dispose() => db.Dispose();
}