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
      switch (Id.IdName)
      {
         case "Position":
         {
            var a = Ids[0].Position.ToString();
            return new StringValue(a);
         }
         case "type":
            return new TypeValue(Ids[0]).Run(ref Manager);
      }
      var result = Id.Run(ref Manager);
      if (result is FuncValue funcValue)
      {
         result = funcValue.Run(ref Manager,Ids);
      }
      if (result is AnyValue anyValue)
      {
         if (anyValue.Result.TryGetValue("init",out result))
            anyValue.Dot(result,Ids);
         result = anyValue;
      }
      if (result is NativeAnyValue nativeAnyValue)
      {
         List<ValueType> a = new List<ValueType>();
         foreach (var id in Ids)
            a.Add(id.Run(ref Manager));
         nativeAnyValue.New(APIs.ListToObjects(a).ToArray());
         result = nativeAnyValue;
      }
      return result;
   }
   public override string ToString() => Id+ "("+APIs.ListToString(Ids)+")";
}