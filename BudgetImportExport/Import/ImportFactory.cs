using Budget.Services.Storage.LiteDB;

namespace BudgetImportExport.Import;

public static class ImportFactory
{
    public static IBulkImport Create(FileInfo file) => file.Extension switch
    {
        ".db" => new LiteDBImport(file.FullName),
        ".csv" => new CsvImport(file.FullName),
        _ => throw new InvalidOperationException($"Unsupported file type to import '{file.Extension}'")
    };
}