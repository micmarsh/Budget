using LanguageExt;
using LiteDB;
using static Budget.Services.Storage.LiteDB.CustomSerializers;
using static LanguageExt.Prelude;

namespace Budget.Services.Storage.LiteDB;

public class LiteDBStorage : IStorage
{
    private readonly string _connectionString;
    private readonly Func<ObjectId> _newObjectId;

    public LiteDBStorage(string connectionString, Func<ObjectId> newObjectId)
    {
        _connectionString = connectionString;
        _newObjectId = newObjectId;
        var mapper = BsonMapper.Global;
        mapper.RegisterType(serializeSeq<SubCategorized>(mapper), deserializeSeq<SubCategorized>(mapper));
        mapper.RegisterType(
            serialize: c => new BsonDocument {["_id"] = c.Value},
            deserialize: doc => new Category(doc["_id"].AsString)
        );
    }
    
    
    public IO<ClassificationsState> GetLatest() =>
        bracketIO(Acq: IO.lift(() => new LiteDatabase(_connectionString)),
            Use: conn => IO.lift(() =>
            {
                var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
                var catsColl = conn.GetCollection<Category>(nameof(Category));

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
        bracketIO(Acq: IO.lift(() => new LiteDatabase(_connectionString)),
            Use: conn => IO.lift(() =>
            {
                var coll = conn.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
                coll.Insert(new ClassificationDoc(_newObjectId(), classified.LineItem.Date, classified));
                
                var catsColl = conn.GetCollection<Category>(nameof(Category));
                var categories = classified switch
                {
                    Categorized categorized => [categorized.Category],
                    Income income => [income.Category],
                    SubClassifications subs => subs.Children.Map(c => c.Category),
                    _ => throw Utilities.patternMatchError(classified)
                };
                catsColl.Upsert(categories);
                return unit;
            }),
            Fin: conn => IO.lift(conn.Dispose)).As();
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
