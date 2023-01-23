using System.Reflection;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldFunc : OldValue
{
    public OldID ID { get; set; }
    public BlockStatement BlockStatement { get; set; }
    public List<OldID> IDs { get; set; }
    public bool isNative { get; set; }
    public MethodInfo Method { get; set; }

    public OldFunc(OldID id,List<OldID>ids,BlockStatement blockStatement)
    {
        ID = id;
        IDs = ids;
        BlockStatement = blockStatement;
    }

    public OldFunc(string IDName, MethodInfo methodInfo)
    {
        isNative = true;
        Method = methodInfo;
    }

    public override OldValue Run(ref VariateManager Manager) => this;


    public OldValue Run(ref VariateManager Manager,List<OldExpr>? ids)
    {
        if (Manager.isClass)
        {
            Manager.AddChildren();
            if (ids is not null && IDs is not null)
                for (int i = 0; i < ids.Count; i++)
                    Manager.Set(IDs[i],ids[i].Run(ref Manager));
            BlockStatement.Run(ref Manager);
            Manager.RemoveChildren();
            return Manager.Result;
        }
        else
        {
            VariateManager manager = new VariateManager();
            manager.AnyInfo = Manager.AnyInfo;
            if (ids is not null && IDs is not null)
                for (int i = 0; i < ids.Count; i++)
                    manager.Set(IDs[i],ids[i].Run(ref Manager));
            BlockStatement.Run(ref manager);
            return manager.Result;
        }
        
    }

    public override string ToString()
    {
        if (isNative)
            return $"{Method}";
        else
            return $"{ID}({OldLangTree.ListToString(IDs)}) => {BlockStatement}";
    }
}