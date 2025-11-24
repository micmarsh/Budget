namespace BudgetImportExport;

public record FlatClassification(string DbId, DateTime Date, string Category, string Description, decimal Amount)
{
    public readonly static string Header =
        $"{nameof(DbId)},{nameof(Date)},{nameof(Category)},{nameof(Description)},{nameof(Amount)}";
    
    public override string ToString() => $"{DbId},{Date},{Description},{Category},{Amount}";
}