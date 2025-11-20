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
                var lastClassifications = coll.Query()
                    .Where(c => c.DateTime.Date == lastDay.Date)
                    .ToEnumerable();
                return new ClassificationsState(
                    lastDay,
                    toSeq(catsColl.Find(_ => true)),
                    toSet(lastClassifications.Select(doc => doc.Record))
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
                catsColl.EnsureIndex(c => c.Value, unique: true);
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