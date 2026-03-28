using LanguageExt;
using static LanguageExt.Prelude;

namespace BudgetMigration.Export;

public static class ExportFactory
{
    public static readonly HashMap<string, Func<FileInfo, IExport>> Exporters = HashMap<string, Func<FileInfo, IExport>>(
        (".db", file => new LiteDBExport(file.FullName)),
        ( ".csv", file => new CsvExport(file.FullName))
    );

    public static Option<IExport> Create(FileInfo file) =>
        Exporters.Find(file.Extension).Map(f => f(file));
}