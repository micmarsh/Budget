using BudgetClassifier;

namespace BudgetTests;

public record TestConsole(Seq<string> InitialInputs) : IConsole
{
    private readonly Atom<(string Head, Seq<string> Tail)> _inputs = Atom((string.Empty, inputs: InitialInputs));// Empty will get discarded on first read

    private static (string, Seq<string>) Pop(Seq<string> inputs) =>
        (inputs.Head.IfNone(() => throw new Exception("out of input!")), inputs.Tail);
    
    private readonly AtomSeq<string> _outputs = new([]);
    public Seq<string> Outputs => _outputs.ToSeq();

    public IO<string> ReadLine() => 
        _inputs.SwapIO(s => Pop(s.Tail)).Map(s => s.Head);

    public IO<Unit> WriteLine(string message) =>
        _outputs.SwapIO(s => s.Add(message)).Map(ignore);
}