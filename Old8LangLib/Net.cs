namespace Old8LangLib;

public class Net
{
    public string A { get; set; }

    public void Print() => Console.WriteLine(A);
    public Net(string s) => A = s;
}