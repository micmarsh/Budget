// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using Budget.Migration.Export;
using Budget.Migration.Import;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static CommandLine.Immutable.Parsing;
using static LanguageExt.Prelude;

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
    .WithAction(RunMigration);

Cmd.New("budget", "A suite of tools for managing a household budget")
    .AddSub(migrate)
    .ToRoot()
    .Parse(args)
    .Invoke();

IO<Unit> RunMigration(IExport exporter, IBulkImport importer) =>
    exporter.ExportClassifications().Collect().Bind(importer.WriteAll)
        .Finally(IO.lift(() => 
        {
            exporter.Dispose();
            importer.Dispose(); 
        }));