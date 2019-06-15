#!/bin/bash

if [ "X$OS" = "XWindows_NT" ] ; then
  # use .Net

  .nuget/NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion -Version 4.64.17
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
        exit $exit_code
  fi

  packages/FAKE/tools/FAKE.exe $@ --fsiargs build.fsx
else

  mono .nuget/NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion -Version 4.64.17
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
        exit $exit_code
  fi

  mono packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx
fi