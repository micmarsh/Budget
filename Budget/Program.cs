using System.Security.Cryptography;
using System.Text;
using Budget;
using Budget.Services.Storage.LiteDB;
using LanguageExt;
using LiteDB;
using static LanguageExt.Prelude;
using static Budget.Services.Storage.LiteDB.CustomSerializers;
using Console = Budget.Console;

// https://stackoverflow.com/a/5721294
string createHash(string phrase)
{
    var hashTool = new SHA512Managed();
    var phraseAsByte = Encoding.UTF8.GetBytes(string.Concat(phrase));
    var encryptedBytes = hashTool.ComputeHash(phraseAsByte);
    hashTool.Clear();
    return Convert.ToHexString(encryptedBytes).Substring(0, 15).ToLower();
}

var fileReads = new FileReads();

const string csvPath = "/home/michael/Downloads/Huntington_Delimited.csv";

var fileHash = fileReads.GetFileText(csvPath).Map(createHash).Run();

var liteDbString = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"/BudgetDb.{fileHash}.db";

var liteDbStorage = new LiteDBStorage(liteDbString, ObjectId.NewObjectId);
ConsoleClassifier.Create(new CsvInfo(
    FilePath: csvPath,
    DescriptionField: "Payee Name",
    AmountField: "Amount",
    DateField: "Date",
    BackupDescription: "Memo"
))
.RunUnsafe(new Runtime(fileReads, liteDbStorage, new Console(), liteDbStorage));

System.Console.WriteLine("DONE");