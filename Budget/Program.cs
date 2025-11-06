using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;


IO<Unit> log(string message) => IO.lift(() => Console.WriteLine(message));

IO<string> readLine() => IO.lift(Console.ReadLine)
    .Bind(s => s == null ? 
        IO.fail<string>("Somehow read a null string from prompt") : 
        IO.pure(s));


IO<Classification> selectCategory(int result, Seq<Category> seq, LineItem lineItem1)
{
    throw new NotImplementedException();
}


IO<Classification> applySubClassifications(string s, Seq<Category> seq, LineItem lineItem1)
{
    throw new NotImplementedException();
}

IO<Classification> classifyFromInput(string input, Seq<Category> categories, LineItem lineItem)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return log("Please enter a valid (non-empty) value").Bind(_ => classify(categories, lineItem));
    }
    
    if (int.TryParse(input, out var index))
    {
        return selectCategory(index, categories, lineItem);
    }

    if (input.StartsWith('*'))
    {
        return applySubClassifications(input, categories, lineItem);
    }

    return IO<Classification>.Pure(new Categorized(new Category(input), lineItem));
}

string getMainPrompt(Seq<Category> categories, LineItem lineItem) =>
    string.Join(Environment.NewLine, $"Please classify {lineItem.Description}: {lineItem.Amount:N}"
        .Cons(categories.Map((c, i) => $"  {i + 1}) {c.Value}")));

IO<Classification> classify(Seq<Category> categories, LineItem lineItem) =>
    from _1 in log(getMainPrompt(categories, lineItem))
    from input in readLine()
    from result in classifyFromInput(input, categories, lineItem)
    select result;
//     
// //
// // var parsed = Csv.ParseFile("/home/michael/Downloads/Huntington_Delimited_Old_Account.csv");
// // var categories = Atom(new Seq<Category>());
//
// classify([new Category("Food"), new Category("Car")], new LineItem("THE STORE", 23.45M))
//     .Bind(c => log(c.ToString()))
//     .Run();

//Console.WriteLine("Foo");



// Might be nice own utility, "neglected" sort of "ecosystem library"?
public static class Csv
{
    public static IO<CsvFile> ParseFile(string filePath)
        => IO.lift(() => File.ReadAllText(filePath))
             .Map(ParseText)
             .Map(lines => new CsvFile(lines.Lines, lines.Header, filePath));

    public static CsvLines ParseText(string fileText)
        => fileText.Split(Environment.NewLine)
                   .Match(() => new CsvLines(Empty, Empty),
                          (head, tail) =>
                          {
                              var keys = toSeq(csvSplit(head));
                              return new CsvLines(tail.Map(CreateCsvLine(keys)), keys);
                          });
    
    private static Func<string, int, CsvLine> CreateCsvLine(Seq<string> keys) 
        => (lineString, linesIndex) =>
           {
               var values = csvSplit(lineString);
               var lineNumber = (uint) linesIndex + 2; // + 2 b/c line 1 should always be header, and index is of course 0-indexed
               var fields = keys.Zip(values).ToMap();
               if (keys.Length == values.Length)
               {
                   return new ValidCsvLine(fields, lineNumber);
               }

               if (values.Length > keys.Length)
               {
                   return new LongCsvLine(fields,  values.Skip(keys.Length), lineNumber);
               }
               // else value.Length < keys.Length
               return new ShortCsvLine(fields, keys.Skip(values.Length), lineNumber);
           };

    //todo needs actual perf testing! also perf testing vs. plain list!? This is why premature optimization is no good
    private static Seq<string> csvSplit(string line)
    {
        var result  = new LinkedList<string>();
        var quoted  = false;
        var builder = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                quoted = !quoted;
                builder.Append(line[i]);
                continue;
            }

            if (line[i] == ',' && !quoted)
            {
                result.AddLast(builder.ToString());
                builder = new StringBuilder();
                continue;
            }
            
            if (line[i] == ',')// && quoted)
            {
                builder.Append(line[i]);
                continue;
            }

            // else is just regular character
            builder.Append(line[i]);
        }
        //todo remove newline at end?
        result.AddLast(builder.ToString());
        return toSeq(result);
    }
}



public abstract record CsvLine(Map<string, string> Fields, uint LineNumber);

public sealed record ValidCsvLine(Map<string, string> Fields, uint LineNumber) : CsvLine(Fields, LineNumber);
public sealed record ShortCsvLine(Map<string, string> Fields, Seq<string> MissingFields, uint LineNumber)
    : CsvLine(Fields, LineNumber);
public sealed record LongCsvLine(Map<string, string> Fields, Seq<string> ExtraValues, uint LineNumber)
    : CsvLine(Fields, LineNumber);

public record CsvLines(Seq<CsvLine> Lines, Seq<string> Header);
public record CsvFile(Seq<CsvLine> Lines, Seq<string> Header, string FileName) : CsvLines(Lines, Header);

// Similarly nothing to do with budget at all, but generally useful for C#? Doesn't even need LanguageExt dep!
    // public static ArgumentException patternMatchError<Supertype>(object unmatchable, string? paramName = null) =>
    //     new ($"Unknown case type {unmatchable.GetType().Name} in" +
    //          $" pattern-match for {typeof(Supertype).Name}" +
    //          fileNameAndLine(), paramName);
    //
    // private static string fileNameAndLine()
    // {
    //     var stackTrace = new System.Diagnostics.StackTrace();
    //     // 0 is fileNameAndLine frame, 1 is pattermMatchError, 2 is where this is used?
    //     var matchFrame = stackTrace.GetFrame(2);
    //     if (matchFrame == null || matchFrame.GetFileName() == null)
    //     {
    //         return string.Empty;
    //     }
    //
    //     return $" at {matchFrame.GetFileName()}:{matchFrame.GetFileLineNumber()}";
    // }

// this class is more of a sketch, use function to think about what inputs are needed?
// maybe just need (LineItem -> IO<Classification>), user prompt, categoryStore, and even 
// whether or not user input is needed at all is then somehow encapsulated away? Maybe now
// need to do real design and figuring out problem to solve
public static class BusinessLogic
{
    public static IO<Classification> classify(CategoryStore categories, LineItem input) 
        => throw new NotImplementedException();
}

public sealed record Category(string Value); // NonEmpty string, use those "domain NewType substitutes", for this?

public interface CategoryStore
{
    public IO<Seq<Category>> Query(CategoryQuery query);
    public IO<Unit> Save(Category category);
}
//todo maybe "module" methods that follow the same pattern as http: requires MonadIO and Readable<CategoryStore>?

public abstract record CategoryQuery;
public sealed record Search(string term) : CategoryQuery;


public abstract record Classification(LineItem LineItem);

public sealed record Categorized(Category Category, LineItem LineItem) : Classification(LineItem);

public sealed record SubClassifications : Classification
{
    // prevent recursive craziness, "Single" and "Multiple" subtypes are now clearly distinct
    public Seq<Categorized> Children { get; }

    private SubClassifications(Seq<Categorized> children, LineItem lineItem) : base(lineItem)
    {
        Children = children;
    }

    public static Option<SubClassifications> New(Seq<Categorized> children, LineItem lineItem)
        => lineItem.Amount == children.Map(x => x.LineItem.Amount).Sum(x => x) ?
               new SubClassifications(children, lineItem) :
               Option<SubClassifications>.None;
}

public sealed record LineItem(string Description, decimal Amount);

// Overall idea: localhost/0.0.0.0 running server, "local first" app that tries to sync with "basic REST" (maybe 
// some kind of tcp/udp check in a background service to avoiding messy polling), but phone is kind of main point of "input". Interesting!
// Do it in MAUI???
// Links to help with project overall
// * Some kind of basis for accessing text messages (at least for Androind) https://stackoverflow.com/questions/72656609/read-sms-for-opt-programically-in-maui-android
// * Background jobs for MAUI: https://github.com/shinyorg/shiny
// * Less terrile MAUI UI? https://github.com/adospace/reactorui-maui