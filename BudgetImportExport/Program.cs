// See https://aka.ms/new-console-template for more information

using BudgetImportExport.Export;
using BudgetImportExport.Import;
using LanguageExt;

const string testCsv = "/home/michael/Documents/test.csv";

// using var export = new LiteDBExport("/home/michael/Documents/BudgetDb.457069f285a9eba.db");
// using var export = new CsvExport("/home/michael/Documents/BudgetDb.457069f285a9eba.csv");
//
// using var import = new CsvImport(testCsv);
using var export = new CsvExport(testCsv);
using var import = new LiteDBImport("/home/michael/Documents/test.db");

import.WriteAll(export.ExportClassifications().ToSeq());

Console.WriteLine("Hello, World!");