using Budget;

namespace BudgetTests;

public class UnitTest1
{
    
    private readonly Seq<Category> Categories = Seq(new Category("Almsgiving"), new Category("Food"), new Category("Cart"));

    private readonly Seq<LineItem> LineItems =  Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
        new LineItem("Progressive Insurance", 800M, DateTime.Now),
        new LineItem("Stuff", 10, DateTime.Now));
    
    [Fact]
    public void classify_selectCategory_ShouldHandleBadInputs()
    {
        var console = new TestConsole([
            "0",
            "notaNumber",
            "-9",
            "1234",
            "2"
        ]);

        var result = UserClassification.classify(Categories, LineItems[0])
            .Map(c => (Categorized) c)
            .RunUnsafe(console);
        
        Assert.Equal("Food", result.Category.Value);
    }
}