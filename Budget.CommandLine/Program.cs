// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.Parsing;
using BudgetMigration;
using BudgetMigration.Export;
using BudgetMigration.Import;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static CommandLine.Immutable.Parsing;

Argument<IExport> inputFile = new ("input file")
{
    CustomParser = factory(argResult =>
    {
        var file = new FileInfo(argResult.Tokens[0].Value);
        return ExportFactory.Create(file)
            .ToFin(Error.New($"Unable to create exporter for file {file.Name}"));
    })
};

Argument<IBulkImport> outputFile = new("output file")
{
    CustomParser = factory(argResult =>
    {
        var file = new FileInfo(argResult.Tokens[0].Value);
        return ImportFactory.Create(file)
            .ToFin(Error.New($"Unable to create importer for file {file.Name}"));
    })
};

var migrate = Cmd.New("migrate", "Migrate data from one format to another (csv, litedb, hopefully soon sqlite)")
    .AddArgument(inputFile)
    .AddArgument(outputFile)
    .WithAction((exporter, importer) =>
        exporter.ExportClassifications().Collect() >> importer.WriteAll);

Cmd.New("budget", "A suite of tools for managing a household budget")
    .AddSub(migrate)
    .ToRoot()
    .Parse(args)
    .Invoke();