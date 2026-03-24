// See https://aka.ms/new-console-template for more information

using Budget;
using Budget.Services.Storage.LiteDB;
using BudgetImportExport.Export;
using BudgetImportExport.Import;
using LanguageExt;
using LiteDB;

const string testCsv = "/home/michael/Documents/test.csv";

const string realDb = "/home/michael/Documents/BudgetDb.457069f285a9eba.db";
//
// using var export = new LiteDBExport(realDb);
// // using var export = new CsvExport(testCsv);
// //
// // using var import = new LiteDBImport(realDb);
// // using var export = new CsvExport(testCsv);
// // using var import = new LiteDBImport("/home/michael/Documents/test.db");
// using var import = new CsvImport(testCsv);
//
// foreach (var item in export.ExportClassifications())
// {
//  import.Write(item);
// }
//
// // import.WriteAll(export.ExportClassifications().ToSeq());
//
// Console.WriteLine("Hello, World!");

using var liteDbDb = new LiteDatabase(realDb);
// using var export = new LiteDBExport(realDb);
//
// liteDbDb.GetCollection<CategorySelectOption>(nameof(CategorySelectOption)).DeleteAll();
// liteDbDb.GetCollection<CategorySelectOption>(nameof(CategorySelectOption)).Upsert(export.ExportClassifications()
//     .AsEnumerable()
//     .SelectMany(c => CategorySelectOption.Create(c.Record)));

var deleted = liteDbDb.GetCollection("AutoClassifications").DeleteMany(doc => doc["_id"].AsString == "SUBSTITUTE CHECK");
System.Console.WriteLine(deleted);