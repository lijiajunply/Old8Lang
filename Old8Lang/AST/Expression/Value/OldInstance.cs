using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

public class OldInstance : OldValue
{
   public List<OldExpr> Ids { get; set; }
   public OldID Id { get; set; }

   public OldInstance(OldID id, List<OldExpr> ids)
   {
      Id = id;
      Ids = ids;
   }

   public override OldValue Run(ref VariateManager Manager)
   {
      var result = Id.Run(ref Manager);
      if (result is OldFunc)
      {
         var a = result as OldFunc;
         if (Manager.IsClass)
            result = a.Run(ref Manager,Ids);
         else
            result = a.Run(ref Manager,Ids);
      }
      if (result is OldAny)
      {
         var a = result as OldAny;
         if (a.Result.TryGetValue("init",out result))
            a.Dot(result,Ids);
         return a;
      }
      return result;
   }
   public override string ToString() => Id+ "("+APIs.ListToString(Ids)+")";
}