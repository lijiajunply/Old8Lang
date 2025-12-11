namespace Old8Lang;

[Serializable]
public class LangInfo
{
    public string ImportPath { get; set; } = "";

    public List<LibInfo> LibInfos { get; set; } = [];

    public string Ver { get; set; } = "";

    public string Url { get; set; } = "";
}

[Serializable]
public class LibInfo
{
    public string LibName { get; set; } = "";
    public double Var { get; set; }
    public bool IsDir { get; set; }
}