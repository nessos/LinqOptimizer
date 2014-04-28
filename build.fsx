// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
//#load "packages/SourceLink.Fake/tools/SourceLink.fsx"
open System
open Fake 
open Fake.Git
open Fake.ReleaseNotesHelper
open Fake.AssemblyInfoFile
//open SourceLink

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "LinqOptimizer"
let authors = ["Nessos Information Technologies, Nick Palladinos, Kostas Rontogiannis"]
let summary = "An automatic query optimizer for LINQ to Objects and PLINQ."

let description = """
    An automatic query optimizer for LINQ to Objects and PLINQ. 
    LinqOptimizer compiles declarative LINQ queries into fast loop-based imperative code. 
    The compiled code has fewer virtual calls, better data locality and speedups of up to 15x.
"""

let tags = "C# F# linq optimization"

let gitHome = "https://github.com/nessos"
let gitName = "LinqOptimizer"
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/nessos"


let testAssemblies = 
    [
        "tests/LinqOptimizer.Tests.CSharp/bin/Release/LinqOptimizer.Tests.CSharp.exe"
        "tests/LinqOptimizer.Tests.FSharp/bin/Release/LinqOptimizer.Tests.FSharp.exe"
    ]

//
//// --------------------------------------------------------------------------------------
//// The rest of the code is standard F# build script 
//// --------------------------------------------------------------------------------------

//// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")
let nugetVersion = release.NugetVersion

Target "BuildVersion" (fun _ ->
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
)

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let attributes =
        [ 
            Attribute.Title project
            Attribute.Product project
            Attribute.Company "Nessos Information Technologies"
            Attribute.Version release.AssemblyVersion
            Attribute.FileVersion release.AssemblyVersion
        ]

    CreateCSharpAssemblyInfo "src/LinqOptimizer.Base/Properties/AssemblyInfo.cs" attributes
    CreateFSharpAssemblyInfo "src/LinqOptimizer.Core/AssemblyInfo.fs" attributes
    CreateCSharpAssemblyInfo "src/LinqOptimizer.CSharp/Properties/AssemblyInfo.cs" attributes
    CreateFSharpAssemblyInfo "src/LinqOptimizer.FSharp/AssemblyInfo.fs" attributes
)


// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "Clean" (fun _ ->
    CleanDirs (!! "**/bin/Release/")
)

//
//// --------------------------------------------------------------------------------------
//// Build library & test project

let configuration = environVarOrDefault "Configuration" "Release"

Target "Build" (fun _ ->
    // Build the rest of the project
    { BaseDirectory = __SOURCE_DIRECTORY__
      Includes = [ project + ".sln" ]
      Excludes = [] } 
    |> MSBuild "" "Build" ["Configuration", configuration]
    |> Log "AppBuild-Output: "
)


// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->
    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/tools" nunitVersion
    ActivateFinalTarget "CloseTestRunner"

    testAssemblies
    |> NUnit (fun p ->
        { p with
            Framework = "v4.0.30319"
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)
//
//// --------------------------------------------------------------------------------------
//// Build a NuGet package

Target "NuGet" (fun _ ->

    let mkNuGetPackage project =
        // Format the description to fit on a single line (remove \r\n and double-spaces)
        let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
        let nugetPath = ".nuget/NuGet.exe"
        NuGet (fun p -> 
            { p with   
                Authors = authors
                Project = project
                Summary = summary
                Description = description
                Version = nugetVersion
                ReleaseNotes = String.concat " " release.Notes
                Tags = tags
                OutputPath = "nuget"
                ToolPath = nugetPath
                AccessKey = getBuildParamOrDefault "nugetkey" ""
                Publish = hasBuildParam "nugetkey" })
            ("nuget/" + project + ".nuspec")

    mkNuGetPackage "LinqOptimizer.CSharp"
    mkNuGetPackage "LinqOptimizer.FSharp"
)


Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "Prepare" DoNothing
Target "PrepareRelease" DoNothing
Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Prepare"
  ==> "Build"
  ==> "RunTests"
  ==> "All"

"All"
  ==> "PrepareRelease" 
  ==> "NuGet"
  ==> "Release"

//RunTargetOrDefault "Release"
RunTargetOrDefault "All"
