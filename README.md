LinqOptimizer
=============
An automatic query optimizer for LINQ to Objects and PLINQ. 
LinqOptimizer compiles declarative LINQ queries into fast loop-based imperative code, offering speedups of up to 15x.

Optimizations
-----------------------
* Lambda inlining
* Loop fusion
* Nested loop generation

```csharp
var query = (from num in nums.AsQueryExpr()
             where num % 2 == 0
             select num * num).Sum();

Console.WriteLine("Result: {0}",query.Run());
// Compiled query
int ___sum___ = 0;
for (int ___index___ = 0; ___index___ < nums.Length; ___index___++)
{
   int num = nums[___index___];
   if (num % 2 == 0)
      ___sum___ += num * num;
}
```
