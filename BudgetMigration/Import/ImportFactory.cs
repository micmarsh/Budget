using LanguageExt;
using static LanguageExt.Prelude;

namespace BudgetMigration.Import;

public static class ImportFactory
{
    public static readonly HashMap<string, Func<FileInfo, IBulkImport>> Importers = HashMap<string, Func<FileInfo, IBulkImport>>(
        (".db", file => new LiteDBImport(file.FullName)),
        ( ".csv", file => new CsvImport(file.FullName))
    );

    public static Option<IBulkImport> Create(FileInfo file) =>
        Importers.Find(file.Extension).Map(f => f(file));
}