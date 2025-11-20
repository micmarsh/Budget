using Budget;
using FluentAssertions;

namespace BudgetTests;

public class UserClassificationTests
{

    private readonly Seq<Category> Categories = Seq(
        new Category("Almsgiving"),
        new Category("Food"),
        new Category("Car"),
        new Category("Work"));

    private static readonly DateTime TestDate = new (2025, 11, 20);

    private readonly Seq<LineItem> LineItems =  Seq(new LineItem("Frank's POS Charge", 23.32M, TestDate),
        new LineItem("Progressive Insurance", 800M, TestDate),
        new LineItem("Stuff", 10, TestDate));


    private Eff<IConsole, Classification> testClassify(Seq<Category> categories, LineItem lineItem) =>
         UserClassification.classify.CoMap((IConsole c) =>
            new UserClassification.ClassifyRT(c, categories, lineItem));
    
    [Fact]
    public void classifyAll_basicTest()
    {
        var expectedOutput = Seq(
@"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
// input: two blank spaces
"Please enter a valid (non-empty) value",
@"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
// select 2/"Food"
@"Progressive Insurance: $800.00 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
// enter "* House 400"
"$400.00 remaining to classify",
// enter "3 200"
"$200.00 remaining to classify",
// enter "* Motorcycle 200" (exercising optional bullet points)"
@"Stuff: $10.00 on Thursday, November 20, 2025
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
        
        var _ = UserClassification.classifyAll(Categories, LineItems)
            .RunUnsafe(ConsoleOnly(console));

        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
    }
    
    [Fact]
    public void classifyAll_isStackSafe()
    {
        const int itemCount = 100000;
        var lineItems = toSeq(Enumerable.Range(0, itemCount))
            .Map(i => new LineItem($"Item {i}", 100, TestDate));
        
        var inputs = toSeq(Enumerable.Repeat("1", itemCount));

        var _ = UserClassification.classifyAll(Categories, lineItems)
            .RunUnsafe(ConsoleOnly(new TestConsole(inputs)));

    }

    [Fact]
    public void classify_applySubClassifications_ShouldEnforceTotals()
    {
        var expectedOutput = Seq(
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // enter "* 2 11.22"
            "$12.10 remaining to classify",
            // enter "* Outdoors 10"
            "$2.10 remaining to classify",
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

        var result = testClassify(Categories, LineItems[0])
            .Map(c => (SubClassifications)c)
            .RunUnsafe(console);
        
        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
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

        var result = testClassify(Categories, LineItems[0])
            .Map(c => (Categorized) c)
            .RunUnsafe(console);

        var expectedOutput = 
            toSeq(Enumerable.Repeat($"Please select a number between 1 and {Categories.Count}", console.InitialInputs.Count - 1));
        
        Assert.Equal("Food", result.Category.Value);
        Assert.Equal(expectedOutput, console.Outputs.Tail); // skip initial prompt
    }
    
    [Fact]
    public void classify_selectCategory_ShouldHandleCancellation()
    {
        var expectedOutput = Seq(
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
                $"Please select a number between 1 and {Categories.Count}",
                $"Please select a number between 1 and {Categories.Count}",
                $"Please select a number between 1 and {Categories.Count}",
                // "cancel"
                "Previous in-progress classification cancelled",
                @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
                $"Please select a number between 1 and {Categories.Count}",
            // "cancel with extra text"
                "Previous in-progress classification cancelled",
                @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work"
        );
        
        var console = new TestConsole([
            "0",
            "notaNumber",
            "-9",
            "cancel",
            "1234",
            "cancel with extra text",
            "Other"
        ]);

        var result = testClassify(Categories, LineItems[0])
            .Map(c => (Categorized) c)
            .RunUnsafe(console);

        result.Category.Value.Should().Be("Other");
        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
    }
    
    
    
    [Fact]
    public void classify_cancellation_ShouldCoverEverythingNeeded()
    {
        var expectedOutput = Seq(
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // -9
            $"Please select a number between 1 and {Categories.Count}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // enter "* 2 11.22"
            "$12.10 remaining to classify",
            // enter "* Outdoors 10"
            "$2.10 remaining to classify",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // enter "* 2 11.22"
            "$12.10 remaining to classify",
            // "* 54433 12.10"
            $"Please select a number between 1 and {Categories.Count}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work",
            // "income 6"
            $"Please select a number between 1 and {Categories.Count}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: $23.32 on Thursday, November 20, 2025
  1) Almsgiving
  2) Food
  3) Car
  4) Work"
            //"Other"
        );
        
        var console = new TestConsole([
            "-9",
            "cancel",
            "* 2 11.22",
            "* Outdoors 10",
            "cancel",
            "* 2 11.22",
            "* 54433 12.10",
            "cancel",
            "income 6",
            "cancel",
            "Other"
        ]);

        var result = testClassify(Categories, LineItems[0])
            .Map(c => (Categorized) c)
            .RunUnsafe(console);
        
        Assert.Equal("Other", result.Category.Value);
        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
    }
    
    [Fact]
    public void classify_income_ShouldAllowSelectingCategories()
    {
        var console = new TestConsole(["income 4", "income Rebate"]);

        var lineItems = Seq(new LineItem("PAYCHECK", 1000M, DateTime.Now),
            new LineItem("REBATE", 300M, DateTime.Now));

        var results = lineItems.TraverseM(lineItem => testClassify(Categories, lineItem))
            .Map(cs => cs.Map(c => (Income)c))
            .RunUnsafe(console);
        
        Assert.Equal("Work", results[0].Category.Value);
        Assert.Equal("Rebate", results[1].Category.Value);
    }

    private static Runtime ConsoleOnly(IConsole c) => new (new NoopFile(), new NoopStorage(), c);

    private class NoopStorage : IStorage
    {
        public IO<ClassificationsState> GetLatest() => IO.empty<ClassificationsState>();
        public IO<Unit> Save(Classification classified) => unitIO;
    }
    
    private class NoopFile : IFileReads
    {
        public IO<string> GetFileText(string filePath) => IO.pure(string.Empty);
    }
}