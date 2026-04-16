using LanguageExt;
using static Budget.Config.ConfigDefaults;
using static LanguageExt.Prelude;

namespace Budget.Config;

public static class Csv
{
    private static readonly Atom<Option<CsvConfigData>> cachedConfigData = Atom<Option<CsvConfigData>>(None);

    public static IO<Option<CsvConfigData>> getConfig = IO
        .lift(() => cachedConfigData.Swap(opt => 
            opt.Match(v => v, () => configWithWarning.Run().Csv)));
}