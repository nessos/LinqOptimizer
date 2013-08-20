LinqOptimizer
=============

An automatic query optimizer for LINQ to Objects and PLINQ.


```csharp

var query = (from num in nums.AsQueryExpr()
             where num % 2 == 0
             select num * num).Sum();

Console.WriteLine("Result: {0}", query.Run());

```
