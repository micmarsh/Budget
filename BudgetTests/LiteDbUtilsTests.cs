using Budget;
using Budget.Migration;
using Budget.Services.Storage.LiteDB;

namespace BudgetTests;

public class LiteDbUtilsTests
{
    private readonly Seq<History> BaseData =
    [
        new Added(new DateTime(2024, 1, 20)),
        new Classified(new Category("Food"), new DateTime(2024, 1, 22, 12, 36, 0)),
        new Classified(new Category("Other"), new DateTime(2024, 1, 22, 12, 36, 0))
    ];

    [Fact]
    public void EncodeDecodeHistoryIdentity()
    {
        var encoded = LiteDbUtils.EncodeHistory(BaseData);
        Assert.Equal(BaseData, LiteDbUtils.DecodeHistory(encoded));
    }
    
    [Fact]
    public void EncodeDecodeHistoryDropsSeconds()
    {
        var withSeconds = BaseData.Map(h => h with
        {
            DateTime = new DateTime(h.DateTime.Year, h.DateTime.Month, h.DateTime.Day, h.DateTime.Hour,
                h.DateTime.Minute, 46)
        });
        var encoded = LiteDbUtils.EncodeHistory(withSeconds);
        Assert.Equal(BaseData, LiteDbUtils.DecodeHistory(encoded));
    }
}