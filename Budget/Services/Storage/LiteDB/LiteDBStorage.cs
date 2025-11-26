using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public class LiteDBStorage : IStorage, IAutoClassifierStorage
{
    private readonly string _connectionString;
    private readonly Func<ObjectId> _newObjectId;

    public LiteDBStorage(string connectionString, Func<ObjectId> newObjectId)
    {
        _connectionString = connectionString;
        _newObjectId = newObjectId;
        RegisterSerializers.Register();
    }


    public IO<ClassificationsState> GetLatest() =>
        bracketIO(Acq: IO.lift(() => new LiteDatabase(_connectionString)),
            Use: conn => IO.lift(() =>
            {
                var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
                var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));

                //todo need separate categories collection?
                var lastDay = coll.Query()
                    .OrderByDescending(c => c.DateTime)
                    .Select(c => c.DateTime)
                    .FirstOrDefault();
                var lastClassifications = coll
                    .Find(c => c.DateTime.Date == lastDay.Date)
                    .Select(doc => doc.Record)
                    .ToList();
                return new ClassificationsState(
                    lastDay,
                    toSeq(catsColl.Find(_ => true).ToList()),
                    toSet(lastClassifications)
                    );
            }),
            Fin: conn => IO.lift(conn.Dispose)).As();

    public IO<Unit> Save(Classification classified) =>
        IO.lift(() =>
        {
            using var conn = new LiteDatabase(_connectionString);
            var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
            coll.Insert(new ClassificationDoc(_newObjectId(), classified.LineItem.Date, classified));

            var catsColl = conn.GetCollection<CategorySelectOption>(nameof(CategorySelectOption));
            catsColl.Upsert(CategorySelectOption.Create(classified));
            return unit;
        });

    public IO<Unit> Save(string description, Category category) =>
        IO.lift(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var coll = db.GetCollection("AutoClassifications");
            coll.Upsert(new BsonDocument
            {
                ["_id"] = description,
                ["category"] = category.Value
            });
            return unit;
        });

    public IO<Seq<(string Description, Category Category)>> GetAll() =>
        IO.lift(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var coll = db.GetCollection("AutoClassifications");
            return toSeq(coll.Find(_ => true))
                .Map(doc => (doc["_id"].AsString, new Category(doc["category"].AsString)))
                .Strict();
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
