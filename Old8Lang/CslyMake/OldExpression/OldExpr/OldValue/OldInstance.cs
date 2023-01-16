using System.Text;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

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
         if (Manager.isClass)
         {
            result = a.Run(ref Manager,Ids);
         }
         else
         {
            result = a.Run(ref Manager,Ids);
         }
      }

      return result;
   }
   public virtual string ListToString(List<OldExpr> a)
   {
      StringBuilder builder = new StringBuilder();
      foreach (var expr in a)
      {
         builder.Append(expr);
      }
      return builder.ToString();
   }
   public override string ToString() => Id + " "+ListToString(Ids);
}