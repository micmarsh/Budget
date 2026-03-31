using System.CommandLine;
using Budget.Migration.Export;
using Budget.Migration.Import;
using CommandLine.Immutable;
using LanguageExt;
using LanguageExt.Common;
using static CommandLine.Immutable.Parsing;

namespace Budget.CommandLine;

public static class Migration
{
    
    private static readonly Argument<IExport> InputFileArg = new ("input file")
    {
        CustomParser = factory(argResult =>
        {
            var file = new FileInfo(argResult.Tokens[0].Value);
            return ExportFactory.Create(file)
                .ToFin(Error.New($"Unable to create exporter for file {file.Name}"));
        })
    };

    private static readonly Argument<IBulkImport> OutputFileArg = new("output file")
    {
        CustomParser = factory(argResult =>
        {
            var file = new FileInfo(argResult.Tokens[0].Value);
            return ImportFactory.Create(file)
                .ToFin(Error.New($"Unable to create importer for file {file.Name}"));
        })
    };

    public static readonly ICmd Command = Cmd.New("migrate", "Migrate data from one format to another (csv, litedb, hopefully soon sqlite)")
        .AddArgument(InputFileArg)
        .AddArgument(OutputFileArg)
        .WithAction(RunMigration);
    
    private static IO<Unit> RunMigration(IExport exporter, IBulkImport importer) =>
        exporter.ExportClassifications().Collect().Bind(importer.WriteAll)
            .Finally(IO.lift(() => 
            {
                exporter.Dispose();
                importer.Dispose(); 
            }));
}