// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.Parsing;
using BudgetMigration;
using BudgetMigration.Export;
using BudgetMigration.Import;
using CommandLine.Immutable;
using LanguageExt;

Argument<FileInfo> inputFile = new ("input file")
{
    Validators = { FileTypeValidation }
};

Argument<FileInfo> outputFile = new("output file")
{
    Validators = { FileTypeValidation }
};

var migrate = Cmd.New("migrate", "Migrate data from one format to another (csv, litedb, hopefully soon sqlite)")
    .AddArgument(inputFile)
    .AddArgument(outputFile)
    .WithAction((input, output) =>
    {
        var exporter = ExportFactory.Create(input);
        var importer = ImportFactory.Create(output);
        return exporter.ExportClassifications()
            .Collect()
            .Bind(importer.WriteAll)
            .Map(_ => 0)
            .Run();
    });

Cmd.New("budget", "A suite of tools for managing a household budget")
    .AddSub(migrate)
    .ToRoot()
    .Parse(args)
    .Invoke();

void FileTypeValidation(ArgumentResult argumentResult)
{
    var file = argumentResult.GetValueOrDefault<FileInfo>();
    switch (file.Extension)
    {
        case ".db":
        case ".csv":
            return;
        default:
            argumentResult.AddError($"Invalid file type extension '{file.Extension}', only .db or .csv files are supported");
            break;
    }
}