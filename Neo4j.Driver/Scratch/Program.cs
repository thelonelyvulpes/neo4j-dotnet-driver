// See https://aka.ms/new-console-template for more information

using System.Linq.Expressions;

Expression<Func<int, bool>> expr = i => i < 5;


Console.WriteLine(expr.Name);
Console.WriteLine(expr.Type);
Console.WriteLine(expr.Parameters);
Console.WriteLine(expr.NodeType);
Console.WriteLine(expr.TailCall);
Console.WriteLine(expr.Body);
Console.WriteLine();
Console.WriteLine(expr.Body.NodeType);
Console.WriteLine(expr.Body.Type);
