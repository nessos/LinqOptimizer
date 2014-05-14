# LinqOptimizer

An automatic query optimizer-compiler for Sequential and Parallel LINQ.

### Build Status

Head (branch `master`), Build & Unit tests

* Windows/.NET [![Build status](https://ci.appveyor.com/api/projects/status/w1avtn54cl6f4eo8/branch/master)](https://ci.appveyor.com/project/nessos/linqoptimizer)
* Mac OS X/Mono 3.2.x [![Build Status](https://travis-ci.org/nessos/LinqOptimizer.png?branch=master)](https://travis-ci.org/nessos/LinqOptimizer/branches)

### Introduction

LinqOptimizer compiles declarative LINQ queries into fast loop-based imperative code.
The compiled code has fewer virtual calls and heap allocations, better data locality and speedups of up to 15x (Check the [Performance] (https://github.com/nessos/LinqOptimizer/wiki/Performance) page).

The main idea is that we lift query sources into the world of Expression trees and
after various transformations-optimizations we compile them into IL for efficient execution.

```csharp
var query = (from num in nums.AsQueryExpr() // lift
             where num % 2 == 0
             select num * num).Sum();
             
Console.WriteLine("Result: {0}", query.Run()); // compile and execute
```

For F# we support functional pipelines and support for F# style LINQ queries is in development.
```fsharp
let query = nums
            |> Query.ofSeq
            |> Query.filter (fun num -> num % 2 = 0)
            |> Query.map (fun num -> num * num)
            |> Query.sum
             
printfn "Result: %d" <| Query.run query // compile and execute
```

### Install via NuGet

```
Install-Package LinqOptimizer.CSharp
Install-Package LinqOptimizer.FSharp
```

### Optimizations

* Lambda inlining
* Loop fusion
* Nested loop generation
* Anonymous Types-Tuples elimination
* Specialized strategies and algorithms

The expression
```csharp
var query = (from num in nums.AsQueryExpr()
             where num % 2 == 0
             select num * num).Sum();
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
and for the parallel case
```csharp
var query = (from num in nums.AsParallelQueryExpr()
             where num % 2 == 0
             select num * num).Sum();
```
will compile to a reduce-combine style straregy
```csharp
Parallel.ReduceCombine(nums, 0, 
                          (acc, num) => { 
                                       if (num % 2 == 0)  
                                         return acc + num * num; 
                                       else
                                         return acc; 
                          }, (left, right) => left + right);
```

### Future work

* Many missing operators
* New specialized operators 
* Even more optimizations


### References

LinqOptimizer draws heavy inspiration from 
* [Steno](http://research.microsoft.com/pubs/173946/paper-pldi.pdf)
* [Clojure - reducers](http://clojure.org/reducers)
