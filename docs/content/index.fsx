(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.


(**

# LinqOptimizer

An automatic query optimizer-compiler for Sequential and Parallel LINQ. 
LinqOptimizer compiles declarative LINQ queries into fast loop-based imperative code.
The compiled code has fewer virtual calls and heap allocations, better data locality and speedups of up to 15x (Check the [Performance](https://github.com/nessos/LinqOptimizer/wiki/Performance) page).

The main idea is that we lift query sources into the world of Expression trees and
after various transformations-optimizations we compile them into IL for efficient execution.

    [lang=csharp]
    var query = (from num in nums.AsQueryExpr() // lift
                 where num % 2 == 0
                 select num * num).Sum();
                 
    Console.WriteLine("Result: {0}", query.Run()); // compile and execute


For F# we support functional pipelines and support for F# style LINQ queries is in development.

    [lang=fsharp]
    let query = nums
                |> Query.ofSeq
                |> Query.filter (fun num -> num % 2 = 0)
                |> Query.map (fun num -> num * num)
                |> Query.sum
             
    printfn "Result: %d" <| Query.run query // compile and execute

## Get LinqOptimizer via NuGet

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      LinqOptimizer can be installed from NuGet:
      <pre>PM> Install-Package LinqOptimizer.CSharp
PM> Install-Package LinqOptimizer.FSharp</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>


## Optimizations

* Lambda inlining
* Loop fusion
* Nested loop generation
* Anonymous Types-Tuples elimination
* Specialized strategies and algorithms


The expression

    [lang=csharp]
    var query = (from num in nums.AsQueryExpr()
                 where num % 2 == 0
                 select num * num).Sum();

will compile to

    [lang=csharp]
    int sum = 0;
    for (int index = 0; index < nums.Length; index++)
    {
       int num = nums[index];
       if (num % 2 == 0)
          sum += num * num;
    }

and for the parallel case

    [lang=csharp]
    var query = (from num in nums.AsParallelQueryExpr()
                 where num % 2 == 0
                 select num * num).Sum();

will compile to a reduce-combine style strategy

    [lang=csharp]
    Parallel.ReduceCombine(nums, 0, 
                              (acc, num) => { 
                                           if (num % 2 == 0)  
                                             return acc + num * num; 
                                           else
                                             return acc; 
                              }, (left, right) => left + right);

## Future work
* Many missing operators
* New specialized operators 
* Even more optimizations

## References

LinqOptimizer draws heavy inspiration from 

* [Steno](http://research.microsoft.com/pubs/173946/paper-pldi.pdf)
* [Clojure reducers](http://clojure.org/reducers)


## Contributing and copyright

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests.

The library is available under the Apache License. 
For more information see the [License file][license] in the GitHub repository. 

  [gh]: https://github.com/nessos/LinqOptimizer
  [issues]: https://github.com/nessos/LinqOptimizer/issues
  [license]: https://github.com/nessos/LinqOptimizer/blob/master/License.md
*)
