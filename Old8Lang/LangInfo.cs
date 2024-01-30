using Old8Lang.Lib;

namespace Old8Lang;

[Serializable]
public class LangInfo
{
    public string ImportPath { get; set; } = "";

    public List<LibInfo> LibInfos { get; init; } = [];

    public string Ver { get; init; } = "";

    public string Url { get; init; } = "";
}