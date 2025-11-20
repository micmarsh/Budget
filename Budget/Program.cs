using System.Text;
using Budget;
using Budget.Services.Storage.LiteDB;
using LiteDB;
using static LanguageExt.Prelude;
using static Budget.Services.Storage.LiteDB.CustomSerializers;


var cats = Seq(new Category("Almsgiving"), new Category("Food"), new Category("Car"));

var lineItems = Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
    new LineItem("Progressive Insurance", 800M, DateTime.Now),
    new LineItem("Stuff", 10, DateTime.Now));

const string database = "dsfajspdflkjq239r8u9ndsaf.db";

// var storage = new LiteDBStorage(database, ObjectId.NewObjectId);
//
// UserClassification.classifyAll(cats, lineItems)
//     .RunUnsafe(new Runtime(default!, storage, new Console()));

var mapper = BsonMapper.Global;
mapper.RegisterType(serializeSeq<SubCategorized>(mapper), deserializeSeq<SubCategorized>(mapper));

using var db = new LiteDatabase(database);
var coll = db.GetCollection<ClassificationDoc>(nameof(ClassificationDoc));
var catsColl = db.GetCollection<Category>(nameof(Category));

var ds = coll.Find(_ => true).ToList();
var dbCats = catsColl.Find(_ => true).ToList();

System.Console.WriteLine("hello");

