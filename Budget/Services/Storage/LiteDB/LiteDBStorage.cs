using ConsoleApps;
using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public class LiteDb : IStorage<ObjectId>, IAutoClassifier, IClassificationQuery<ObjectId>, IDisposable
{
    private const string AutoClassificationsCollectionName = "AutoClassifications";
    private readonly LiteDatabase conn;

    public LiteDb(FileInfo connectionString, Func<ObjectId> newObjectId)
    {
        conn = new LiteDatabase(connectionString.FullName);
        Initialize();
    }

    public LiteDb(Stream stream, Func<ObjectId> newObjectId)
    {
        conn = new LiteDatabase(stream);
        Initialize();
    }

    private void Initialize()
    {
        RegisterSerializers.Register();

        var coll = conn.GetCollection(AutoClassificationsCollectionName);
        var saved = coll.Find(_ => true)
            .Select(doc => (doc["_id"].AsString, new Category(doc["category"].AsString)))
            .ToHashMap();
        AutoClassifyCache.Swap(_ => saved);
    }

    public IO<Unit> Save(ObjectId objectId, Classification classified) =>
        IO.lift(() =>
        {
            var now = DateTime.Now; // todo inject into constructor?

            var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
            var categoryOptions = CategorySelectOption.Create(classified);
            catsColl.Upsert(categoryOptions);

            var classifiedHistoryEntries = categoryOptions.Map(c => (History)new Classified(c.Category, now));

            var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
            var existing = DocLookupCache.Value.Find(objectId);
            // should just be Update but Upsert makes existing tests (5/12/2026) not break
            coll.Upsert(new ClassificationDoc(
                objectId, 
                classified,
                existing.Map(d => d.History).IfNone(Empty)
                    .Concat(classifiedHistoryEntries)
                ));

            return unit;
        });

    public IO<int> Delete(Seq<ObjectId> deleteIds) =>
        IO.lift(() =>
        {
            var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
            var idSet = deleteIds.ToHashSet();
            return coll.DeleteMany(c => idSet.Contains(c.Id));
        });

    private readonly Atom<HashMap<string, Category>> AutoClassifyCache =
        Atom(LanguageExt.HashMap<string, Category>.Empty);

    IO<Unit> IAutoClassifier.Save(string description, Category category) =>
        IO.lift(() =>
        {
            var coll = conn.GetCollection(AutoClassificationsCollectionName);
            coll.Upsert(new BsonDocument
            {
                ["_id"] = description,
                ["category"] = category.Value
            });
            AutoClassifyCache.Swap(cache => cache.Add(description, category));
            return unit;
        });

    public IO<Option<Category>> Lookup(string description) =>
        AutoClassifyCache.ValueIO.Map(cache => cache.Find(description));

    private readonly Atom<HashMap<ObjectId, ClassificationDoc>> DocLookupCache = Atom(HashMap<ObjectId, ClassificationDoc>());
    
    public Source<QueryResult<ObjectId>> GetDateRange(DateTime start, DateTime end)
    {
        var cursor = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc)).Query()
            .Where(c => c.Record.LineItem.Date >= start)
            .Where(c => c.Record.LineItem.Date <= end)
            .ToEnumerable();
        System.Console.WriteLine($"State of db before looking up {start} to {end} { conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc)).Query()
            .Where(c => c.Record.LineItem.Date >= start)
            .Where(c => c.Record.LineItem.Date <= end).Count()} all docs? {conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc)).Find(_ => true).Count()} docs");
        return Source.lift(cursor)
         //   .Transform(new ActionTransducer<ClassificationDoc>(doc => Prompt.logIO($"Found a doc: {doc}")))
            .Transform(new ActionTransducer<ClassificationDoc>(doc => 
                DocLookupCache.SwapIO(map => map.AddOrUpdate(doc.Id, doc)) * ignore))
            .Map(doc => new QueryResult<ObjectId>(doc.Id, doc.Record));
    }

    public Source<CategorySelectOption> GetAllCategories()
    {
        var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
        return Source.lift(catsColl.Find(_ => true));
    }

    public void Dispose() => conn.Dispose();
}

// Basically some repl code
//
// var cats = Seq(new Category("Almsgiving"), new Category("Food"), new Category("Car"));
//
// var lineItems = Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
//     new LineItem("Progressive Insurance", 800M, DateTime.Now),
//     new LineItem("Stuff", 10, DateTime.Now));
//
// const string database = "dsfajspdflkjq239r8u9ndsaf.db";
//
// // var storage = new LiteDBStorage(database, ObjectId.NewObjectId);
// //
// // UserClassification.classifyAll(cats, lineItems)
// //     .RunUnsafe(new Runtime(default!, storage, new Console()));
//
// var mapper = BsonMapper.Global;
// mapper.RegisterType(serializeSeq<SubCategorized>(mapper), deserializeSeq<SubCategorized>(mapper));
//
// using var db = new LiteDatabase(database);
// var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
// var catsColl = db.GetCollection<Category>(nameof(Category));
//
// var ds = coll.Find(_ => true).ToList();
// var dbCats = catsColl.Find(_ => true).ToList();
//
// System.Console.WriteLine("hello");
