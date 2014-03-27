
namespace Nessos.LinqOptimizer.Core

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
 
module Version =
    let [<Literal>]Number = "0.5.2.*"

[<assembly: AssemblyVersion(Version.Number)>]
do ()