using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class Instance : ValueType
{
   public List<OldExpr> Ids { get; set; }
   public OldID Id { get; set; }

   public Instance(OldID id, List<OldExpr> ids)
   {
      Id = id;
      Ids = ids;
   }

   public override ValueType Run(ref VariateManager Manager)
   {
      var result = Id.Run(ref Manager);
      if (result is FuncValue)
      {
         var a = result as FuncValue;
         if (Manager.IsClass)
            result = a.Run(ref Manager,Ids);
         else
            result = a.Run(ref Manager,Ids);
      }
      if (result is AnyValue)
      {
         var a = result as AnyValue;
         if (a.Result.TryGetValue("init",out result))
            a.Dot(result,Ids);
         return a;
      }
      return result;
   }
   public override string ToString() => Id+ "("+APIs.ListToString(Ids)+")";
}