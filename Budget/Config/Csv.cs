using LanguageExt;
using static Budget.Config.ConfigDefaults;
using static LanguageExt.Prelude;

namespace Budget.Config;

//todo probably delete this, excessive abstraction at this point?
public static class Csv
{
    public static IO<Option<CsvConfigData>> getConfig = configWithWarning.Map(c => c.Csv);
}