using Budget;

namespace BudgetTests;

public class TestConsole(Seq<string> inputs) : IConsole
{
    
    private Atom<(string Head, Seq<string> Tail)> _inputs = Atom((string.Empty, inputs));// Empty will get discarded on first read

    private static (string, Seq<string>) Pop(Seq<string> inputs) =>
        (inputs.Head.IfNone(() => throw new Exception("out of input!")), inputs.Tail);
    
    private AtomSeq<string> _outputs = new([]);
    public Seq<string> Outputs => _outputs.ToSeq();

    public IO<string> ReadLine() => 
        _inputs.SwapIO(s => Pop(s.Tail)).Map(s => s.Head);

    public IO<Unit> WriteLine(string message) =>
        _outputs.SwapIO(s => s.Add(message)).Map(ignore);
}