LinqOptimizer
=============
An automatic query optimizer for LINQ to Objects and PLINQ. 
LinqOptimizer compiles declarative LINQ queries into fast loop-based imperative code.
The compiled code has fewer virtual calls, better data locality and speedups of up to 15x.

Optimizations
-----------------------
* Lambda inlining
* Loop fusion
* Nested loop generation

The expression
```csharp
var query = (from num in nums.AsQueryExpr()
             where num % 2 == 0
             select num * num).Sum();

Console.WriteLine("Result: {0}",query.Run());
```
will compile to
```csharp
int sum = 0;
for (int index = 0; index < nums.Length; index++)
{
   int num = nums[index];
   if (num % 2 == 0)
      sum += num * num;
}
```
