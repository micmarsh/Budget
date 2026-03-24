using LanguageExt;

namespace BudgetMigration;

public readonly record struct FlatClassification(string DbId, DateTime Date, Option<string> Category, string Description, decimal Amount)
{
    public readonly static string CsvHeader =
        $"{nameof(DbId)},{nameof(Date)},{nameof(Description)},{nameof(Category)},{nameof(Amount)}";
    
    public override string ToString() => $"{DbId},\"{Date}\",\"{Description}\",\"{Category.IfNone(string.Empty)}\",{Amount}";
}