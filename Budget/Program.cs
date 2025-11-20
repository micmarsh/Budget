using System.Security.Cryptography;
using System.Text;
using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;
using static Budget.Services.Storage.LiteDB.CustomSerializers;
using Console = Budget.Console;

//
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

// https://stackoverflow.com/a/5721294
string CreateSHAHash(string phrase)
{
    var hashTool = new SHA512Managed();
    var phraseAsByte = Encoding.UTF8.GetBytes(string.Concat(phrase));
    var encryptedBytes = hashTool.ComputeHash(phraseAsByte);
    hashTool.Clear();
    return Convert.ToBase64String(encryptedBytes);
}

var fileReads = new FileReads();

const string csvPath = "/home/michael/Downloads/Huntington_Delimited.csv";

var hasher = SHA1.Create();
var fileHash = fileReads.GetFileText(csvPath).Map(CreateSHAHash).Run();

var liteDbString = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"BudgetDb.{fileHash}.db";

ConsoleClassifier.Create(new CsvInfo(
    FilePath: csvPath,
    DescriptionField: "Payee Name",
    AmountField: "Amount",
    DateField: "Date"
))
.RunUnsafe(new Runtime(fileReads, new LiteDBStorage(liteDbString, ObjectId.NewObjectId), new Console()));

System.Console.WriteLine("DONE");