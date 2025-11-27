using Budget;
using FluentAssertions;

namespace BudgetTests;

public class UserClassificationTests
{

    private readonly Seq<CategorySelectOption> Categories = Seq(
        new CategorySelectOption(new Category("Almsgiving"), false),
        new CategorySelectOption(new Category("Food"), false),
        new CategorySelectOption(new Category("Car"), false),
        new CategorySelectOption(new Category("Work"), true));

    private int SpendingCategoryCount => Categories.Count(c => ! c.IsIncome);


    private static readonly DateTime TestDate = new (2025, 11, 20);

    private readonly Seq<LineItem> LineItems =  Seq(new LineItem("Frank's POS Charge", -23.32M, TestDate),
        new LineItem("Progressive Insurance", -800M, TestDate),
        new LineItem("Stuff", -10, TestDate));


    private Eff<IConsole, Classification> testClassify(Seq<CategorySelectOption> categories, LineItem lineItem)
    {
        var output = Atom(default(Classification));
        return UserClassification.classifyAll(categories, [lineItem])
            .CoMap((IConsole c) => new Runtime(new NoopFile(), new AtomStorage(output), c, new NoopStorage()))
            .Map(_ => output.Value ?? throw new InvalidOperationException());
    }
    
    [Fact]
    public void classifyAll_basicTest()
    {
        var expectedOutput = Seq(
@"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
// input: two blank spaces
"Please enter a valid (non-empty) value",
@"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
// select 2/"Food"
@"Progressive Insurance: -$800.00 on Thursday, November 20, 2025
(Spending)
  1) Food
  2) Almsgiving
  3) Car",
// enter "* House 400"
"$400.00 remaining to classify",
// enter "3 200"
"$200.00 remaining to classify",
// enter "* Motorcycle 200" (exercising optional bullet points)"
@"Stuff: -$10.00 on Thursday, November 20, 2025
(Spending)
  1) House
  2) Car
  3) Motorcycle
  4) Food
  5) Almsgiving"
);

        var console = new TestConsole([
            "  ",
            "2",
            "* House 400",
            "3 200",
            "* Motorcycle 200",
            "Other"
        ]);
        
        var _ = UserClassification.classifyAll(Categories, LineItems)
            .RunUnsafe(ConsoleOnly(console));

        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
    }
    
    //[Fact]
    //Too slow, comment back in occasionally
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
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
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
            toSeq(Enumerable.Repeat($"Please select a number between 1 and {SpendingCategoryCount}", console.InitialInputs.Count - 1));
        
        Assert.Equal("Food", result.Category.Value);
        Assert.Equal(expectedOutput, console.Outputs.Tail); // skip initial prompt
    }
    
    [Fact]
    public void classify_selectCategory_ShouldHandleCancellation()
    {
        var expectedOutput = Seq(
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
                $"Please select a number between 1 and {SpendingCategoryCount}",
                $"Please select a number between 1 and {SpendingCategoryCount}",
                $"Please select a number between 1 and {SpendingCategoryCount}",
                // "cancel"
                "Previous in-progress classification cancelled",
                @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
                $"Please select a number between 1 and {SpendingCategoryCount}",
            // "cancel with extra text"
                "Previous in-progress classification cancelled",
                @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car"
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
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
            // -9
            $"Please select a number between 1 and {SpendingCategoryCount}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
            // enter "* 2 11.22"
            "$12.10 remaining to classify",
            // enter "* Outdoors 10"
            "$2.10 remaining to classify",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
            // enter "* 2 11.22"
            "$12.10 remaining to classify",
            // "* 54433 12.10"
            $"Please select a number between 1 and {SpendingCategoryCount}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
            // "6"
            $"Please select a number between 1 and {SpendingCategoryCount}",
            // "cancel"
            "Previous in-progress classification cancelled",
            @"Frank's POS Charge: -$23.32 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car"
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
            "6",
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
    public void classify_income_ShouldFilterCategories()
    {
        var expectedOutput = Seq(
            @"PAYCHECK: $1,000.00 on Thursday, November 20, 2025
(Income)
  1) Work",
            // enter "1",
            @"MENARDS REBATE: $300.00 on Thursday, November 20, 2025
(Income)
  1) Work",
            // enter "Rebate"
            @"Food Store: -$123.00 on Thursday, November 20, 2025
(Spending)
  1) Almsgiving
  2) Food
  3) Car",
            // "Groceries"
            @"Cash: $1.00 on Thursday, November 20, 2025
(Income)
  1) Rebate
  2) Work"
            // enter "Found on Sidewalk"
        );
        
        var console = new TestConsole(["1", "Rebate", "Groceries", "Found on Sidewalk"]);

        var lineItems = Seq(new LineItem("PAYCHECK", 1000M, TestDate),
            new LineItem("MENARDS REBATE", 300M, TestDate),
            new LineItem("Food Store", -123, TestDate), 
            new LineItem("Cash", 1, TestDate));

        var _ = UserClassification.classifyAll(Categories, lineItems)
            .RunUnsafe(ConsoleOnly(console));

        console.Outputs.Should<Seq<string>>().BeEquivalentTo(expectedOutput);
    }

    private static Runtime ConsoleOnly(IConsole c) => new (new NoopFile(), new NoopStorage(), c, new NoopStorage());

    private class NoopStorage : IStorage, IAutoClassifierStorage
    {
        public IO<ClassificationsState> GetLatest() => IO.empty<ClassificationsState>();
        public IO<Unit> Save(Classification classified) => unitIO;
        public IO<Unit> Save(string description, Category category) => unitIO;
        public IO<Option<Category>> Lookup(string description) => IO.pure(Option<Category>.None);
    }
    
    private class AtomStorage(Atom<Classification?> Saved) : IStorage
    {
        public IO<ClassificationsState> GetLatest() => IO.empty<ClassificationsState>();
        public IO<Unit> Save(Classification classified) => Saved.SwapIO(_ => classified).IgnoreF().As();
    }
    
    private class NoopFile : IFileReads
    {
        public IO<string> GetFileText(string filePath) => IO.pure(string.Empty);
    }
}