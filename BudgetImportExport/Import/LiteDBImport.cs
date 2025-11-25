using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace BudgetImportExport.Import;

public class LiteDBImport(string DbFilePath) : IImport<ClassificationDoc>, IBulkImport<ClassificationDoc>
{
    private LiteDatabase db = new (DbFilePath);

    public void Dispose() => db.Dispose();
    public Unit Write(ClassificationDoc doc)
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        coll.Upsert(doc);
                        
        var catsColl = db.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
        catsColl.Upsert(CategorySelectOption.Create(doc.Record));
        
        return Unit.Default;
    }

    public Unit WriteAll(Seq<ClassificationDoc> items)
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        coll.Upsert(items.AsEnumerable());
                        
        var catsColl = db.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
        catsColl.Upsert(items.AsEnumerable().SelectMany(doc => CategorySelectOption.Create(doc.Record)));
        
        return Unit.Default;
    }
}