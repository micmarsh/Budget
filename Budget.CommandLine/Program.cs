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

Argument<FileInfo> inputFile = new ("input file")
{
    Validators = { validate(FileTypeValidation(ExportFactory.Exporters.Keys)) }
};

Argument<FileInfo> outputFile = new("output file")
{
    Validators = { validate(FileTypeValidation(ImportFactory.Importers.Keys)) }
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
            .Bind(importer.WriteAll);
    });

Cmd.New("budget", "A suite of tools for managing a household budget")
    .AddSub(migrate)
    .ToRoot()
    .Parse(args)
    .Invoke();

Func<ArgumentResult, Seq<Error>> FileTypeValidation(Iterable<string> allowExtensions) => argumentResult =>
{
    var extSet = Prelude.toSet(allowExtensions);
    var file = argumentResult.GetValueOrDefault<FileInfo>();
    return extSet.Contains(file.Extension) ? [] :
        [Error.New($"Invalid file type extension '{file.Extension}', only {string.Join(", ", allowExtensions)} files are supported")];
};