using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

public class OldInstance : OldValue
{
   public List<OldID> Ids { get; set; }
   public OldID Id { get; set; }

   public OldInstance(OldID id, List<OldID> ids)
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
         result = a.Run(ref Manager, Ids, new Dictionary<OldID, OldValue>());
      }

      return result;
   }
}