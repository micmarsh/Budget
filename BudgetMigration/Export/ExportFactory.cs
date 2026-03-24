namespace BudgetMigration.Export;

public static class ExportFactory
{
    public static IExport Create(FileInfo file) => file.Extension switch
    {
        ".db" => new LiteDBExport(file.FullName),
        ".csv" => new CsvExport(file.FullName),
        _ => throw new InvalidOperationException($"Unsupported file type to export '{file.Extension}'")
    };
}