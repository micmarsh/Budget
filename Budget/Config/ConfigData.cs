using LanguageExt;

namespace Budget.Config;

public readonly record struct ConfigData(string DbLocation, Option<CsvConfigData> Csv);

public readonly record struct CsvConfigData(string DescriptionField, string AmountField, string DateField, string BackupDescriptionField);