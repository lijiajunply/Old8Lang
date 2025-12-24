using System;
using System.Reflection;
using Old8Lang.AST.Statement;

var type = typeof(TryStatement);
Console.WriteLine("Fields:");
foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
{
    Console.WriteLine($"  {field.Name} - {field.FieldType.Name}");
}
Console.WriteLine("\nProperties:");
foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
{
    Console.WriteLine($"  {prop.Name} - {prop.PropertyType.Name}");
}
