using Budget;

namespace BudgetTests;

public class UnitTest1
{

    private readonly Seq<Category> Categories = Seq(
        new Category("Almsgiving"),
        new Category("Food"),
        new Category("Car"),
        new Category("Work"));

    private readonly Seq<LineItem> LineItems =  Seq(new LineItem("Frank's POS Charge", 23.34M, DateTime.Now),
        new LineItem("Progressive Insurance", 800M, DateTime.Now),
        new LineItem("Stuff", 10, DateTime.Now));
    
    
    [Fact]
    public void classifyAll_basicTest()
    {
        var expectedOutput = Seq(
@"Frank's POS Charge: $23.34
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
// input: two blank spaces
"Please enter a valid (non-empty) value",
// select 2/"Food"
@"Progressive Insurance: $800.00
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
// enter "* House 400"
// enter "3 200"
// enter "* Motorcycle 200" (excersing optional bullet points)"
// todo will need a lot of error handling in subclasses
@"Stuff: $10.00
  1) Almsgiving
  2) Food
  3) Car
  4) Work
  5) House
  6) Motorcycle"
// enter "income Interest Payment"
);

        var console = new TestConsole([
            "  ",
            "2",
            "* House 400",
            "3 200",
            "* Motorcycle 200",
            "income Interest Payment"
        ]);
        
        var _ = UserClassification.classifyAll(_ => unitIO, Categories, LineItems)
            .RunUnsafe(console);
        
        Assert.Equal(expectedOutput, console.Outputs);
    }
    
    [Fact]
    public void classifyAll_isStackSafe()
    {
        const int itemCount = 100000;
        var lineItems = toSeq(Enumerable.Range(0, itemCount))
            .Map(i => new LineItem($"Item {i}", 100, DateTime.Now));
        
        var inputs = toSeq(Enumerable.Repeat("1", itemCount));

        var _ = UserClassification.classifyAll(_ => unitIO, Categories, lineItems)
            .RunUnsafe(new TestConsole(inputs));
    }

    [Fact]
    public void classify_applySubClassifications_ShouldEnforceTotals()
    {
        var expectedOutput = Seq(
            @"Frank's POS Charge: $23.32
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // enter "* 2 11.22"
            // enter "* Outdoors 10"
            // enter "Other 20"
"Last entry exceeded total by $17.90 (only $2.10 left), please try again",
            // enter "* Other 10"
"Last entry exceeded total by $7.90 (only $2.10 left), please try again"
            // enter "Other 2.1"
            );

        var console = new TestConsole([
            "* 2 11.22",
            "* Outdoors 10",
            "Other 20",
            "* Other 10",
            "Other 2.1"
        ]);

        var result = UserClassification.classify(Categories, LineItems[0])
            .RunUnsafe(console);
        
        Assert.Equal(expectedOutput, console.Outputs);
    }
    
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

        var expectedOutput = 
            toSeq(Enumerable.Repeat($"Please select a number between 1 and {Categories.Count}", console.InitialInputs.Count - 1));
        
        Assert.Equal("Food", result.Category.Value);
        Assert.Equal(expectedOutput, console.Outputs.Tail); // skip initial prompt
    }
    
    [Fact]
    public void classify_income_ShouldAllowSelectingCategories()
    {
        var console = new TestConsole(["income 4", "income Rebate"]);

        var lineItems = Seq(new LineItem("PAYCHECK", 1000M, DateTime.Now),
            new LineItem("REBATE", 300M, DateTime.Now));

        var results = lineItems.TraverseM(lineItem => UserClassification.classify(Categories, lineItem))
            .Map(cs => cs.Map(c => (Income)c))
            .RunUnsafe(console);
        
        Assert.Equal("Work", results[0].Category.Value);
        Assert.Equal("Rebate", results[1].Category.Value);
    }
}