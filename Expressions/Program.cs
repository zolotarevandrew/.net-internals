// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;


//(х) => Console.WriteLine(х + 5)

ParameterExpression parameter = Expression.Parameter(typeof(int));
ConstantExpression constant = Expression.Constant(5, typeof(int));

BinaryExpression add = Expression.Add(parameter, constant);

MethodInfo writeLine = typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(int) });

MethodCallExpression methodCall = Expression.Call(null, writeLine, add);

Expression<Action<int>> expressionlambda = Expression.Lambda<Action<int>>(methodCall, parameter);
Action<int> delegateLambda = expressionlambda.Compile();
delegateLambda(1);


//dynamic 
var parameter1 = Expression.Parameter(typeof(object), "name1");
var parameter2 = Expression.Parameter(typeof(object), "name2"); 
var dynamicParam1 = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
var dynamicParam2 = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null);
CallSiteBinder csb = Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Add, typeof(Program), new[] { dynamicParam1, dynamicParam2 });
var dyno = Expression.Dynamic(csb, typeof(object), parameter1, parameter2);
Expression<Func<dynamic, dynamic, dynamic>> expr = Expression.Lambda<Func<dynamic, dynamic, dynamic>>(dyno, new[] { parameter1, parameter2 });
var watch = Stopwatch.StartNew();
Func<dynamic, dynamic, dynamic> action = expr.Compile();
watch.Stop();
//compilation about 300 milliseconds, very slow, should cache it in ConcurrentDictionary and warming up
Console.WriteLine(watch.Elapsed);
var res = action("1", "2");
Console.WriteLine(res); //12
res = action(1, 2);
Console.WriteLine(res);