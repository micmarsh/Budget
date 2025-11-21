namespace Budget;

// not in "Domain" b/c is likely getting moved to console-only project/namespace, not shared
public record CategorySelectOption(Category Category, bool IsIncome);