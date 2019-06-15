@echo off

if not exist packages\FAKE\tools\Fake.exe ( 
  .nuget\NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion -Version 4.64.17
)

packages\FAKE\tools\FAKE.exe build.fsx %*