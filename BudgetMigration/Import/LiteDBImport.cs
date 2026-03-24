using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;

namespace BudgetMigration.Import;

public class LiteDBImport(string DbFilePath) : IBulkImport
{
    private LiteDatabase db = new (DbFilePath);
    
    static LiteDBImport() => RegisterSerializers.Register();
    
    public void Dispose() => db.Dispose();
    
    public Unit WriteAll(Seq<FlatClassification> items)
    {
        var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
        var classificationDocs = items
            .GroupBy(line => line.DbId)
            .Select(g => g.Count() == 1 ? getSingle(g.First()) : getSubclassifications(g.AsEnumerable()))
            .AsIterable().ToSeq();
        
        coll.Upsert(classificationDocs);
                        
        var catsColl = db.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
        catsColl.Upsert(classificationDocs.Map(doc => doc.Record).Bind(CategorySelectOption.Create));
        
        return Unit.Default;
    }
    
    
    private static ClassificationDoc getSubclassifications(IEnumerable<FlatClassification> lines)
    {
        var line = lines.First();
        var dateTime = line.Date;
        return new ClassificationDoc(new ObjectId(line.DbId),
            dateTime,
            new SubClassifications(
                Prelude.toSeq(lines.Select(l => new SubCategorized(
                    new Category(line.Category.IfNone(() => throw new InvalidOperationException($"SubClassification of {l.Amount} for {line.Description} (DB ID: {line.DbId}) is missing a Category"))),
                    l.Amount)
                )),
                new LineItem(line.Description,
                    line.Amount,
                    dateTime)
            ));
    }

    private static ClassificationDoc getSingle(FlatClassification line)
    {
        var dateTime = line.Date;
        return new ClassificationDoc(new ObjectId(line.DbId),
            dateTime,
            line.Category.Match(category => new Categorized(
                new Category(category),
                new LineItem(line.Description,
                    line.Amount,
                    dateTime)
            ), () => (Classification)new UnCategorized(new LineItem(line.Description,
                line.Amount,
                dateTime))));
    }
}