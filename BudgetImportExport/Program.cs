// See https://aka.ms/new-console-template for more information

using BudgetImportExport.Export;
using BudgetImportExport.Import;

// using var export = new LiteDBExport("/home/michael/Documents/BudgetDb.457069f285a9eba.db");
using var export = new CsvExport("/home/michael/Documents/BudgetDb.457069f285a9eba.csv");

using var import = new CsvImport("/home/michael/Documents/test.csv");

foreach (var doc in  export.ExportClassifications())
{
    import.Write(doc);
}

Console.WriteLine("Hello, World!");