using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public class LiteDb : IStorage<ObjectId>, IAutoClassifier, IClassificationQuery<ObjectId>
{
    private const string AutoClassificationsCollectionName = "AutoClassifications";
    private readonly string _connectionString;
    private readonly Stream _stream;
    private readonly Func<ObjectId> _newObjectId;

    public LiteDb(string connectionString, Func<ObjectId> newObjectId)
    {
        _connectionString = connectionString;
        _newObjectId = newObjectId;
        Initialize();
    }
    
    public LiteDb(Stream stream, Func<ObjectId> newObjectId)
    {
        _stream = stream;
        _newObjectId = newObjectId;
        Initialize();
    }

    private void Initialize()
    {
        RegisterSerializers.Register();
        
        using var db = GetDb();
        var coll = db.GetCollection(AutoClassificationsCollectionName);
        var saved = coll.Find(_ => true)
            .Select(doc => (doc["_id"].AsString, new Category(doc["category"].AsString)))
            .ToHashMap();
        AutoClassifyCache.Swap(_ => saved);
    }

    private LiteDatabase GetDb() => string.IsNullOrEmpty(_connectionString) ? new (_stream) : new(_connectionString);

    public IO<Unit> Save(Classification classified) =>
        IO.lift(() =>
        {
            var now = DateTime.Now; // todo inject into constructor?
            using var conn = GetDb();
            var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
            coll.Insert(ClassificationDoc.NewAdd(_newObjectId(), now, classified));

            var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
            catsColl.Upsert(CategorySelectOption.Create(classified));
            return unit;
        });

    public IO<Unit> Delete(Seq<ObjectId> deleteIds) =>
        IO.lift(() =>
        {
            using var conn = GetDb();
            var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
            var idSet = deleteIds.ToHashSet();
            coll.DeleteMany(c => idSet.Contains(c.Id));
        });

    private readonly Atom<HashMap<string, Category>> AutoClassifyCache = Atom(LanguageExt.HashMap<string, Category>.Empty);

    public IO<Unit> Save(string description, Category category) =>
        IO.lift(() =>
        {
            using var db = GetDb();
            var coll = db.GetCollection(AutoClassificationsCollectionName);
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

    //todo just return id + classification? Union type UniqueDbId that only wraps LiteDb for now?
    public Source<QueryResult<ObjectId>> GetDateRange(DateTime start, DateTime end)
    {
        var db = GetDb();
        var cursor = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc)).Query()
            .Where(c => c.Record.LineItem.Date >= start)
            .Where(c => c.Record.LineItem.Date <= end)
            .ToEnumerable()
            .Select(c => new QueryResult<ObjectId>(c.Id, c.Record));
        return Source.lift(cursor.DisposeAfter(db));
    }

    public IO<Seq<CategorySelectOption>> GetAllCategories() =>
        IO.lift(() =>
        {
            using var conn = GetDb();
            var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
            return toSeq(catsColl.Find(_ => true).ToList());
        });
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
