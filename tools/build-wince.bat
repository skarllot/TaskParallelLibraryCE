@echo off
setlocal enabledelayedexpansion

REM Capture parameters
set SolutionDir=%~1
set SolutionName=%~2
set Project=%~3
set Configuration=%~4

set net35path="C:\Windows\Microsoft.NET\Framework\v3.5"
set msbuild35="%net35path%\MSBuild.exe"
set targetscf="%net35path%\Microsoft.CompactFramework.CSharp.targets"

set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild"
if not exist %msbuild% (
    set msbuild="C:\Program Files (x86)\MSBuild\12.0\bin\msbuild"
)

echo Compiling %Project% for .NETFramework,Version=v3.5,Profile=CompactFramework
echo.

REM ============================================================================
REM Check tools availability
REM ============================================================================
if not exist %msbuild% (
    echo Error trying to find MSBuild executable
    EXIT /B 1
)

if not exist %targetscf% (
    echo Error trying to find Compact Framework targets
    echo.
    echo Install '.NET Compact Framework Redistributable'
    echo and 'Power Toys for .NET Compact Framework 3.5'
    exit /B 1
)

REM ============================================================================
REM Setup variables
REM ============================================================================

set SolutionFile=WindowsCE\%SolutionName%.sln
set TargetDir=net35-cf
set TargetFX=v3.5
set Profile=
set Constants="TRACE;WindowsCE"

if "%Configuration%" == "" (
    set Configuration=Release
)

if not exist %SolutionDir%%SolutionName%.sln (
    echo Missing solution for Compact Framework target
    exit /B 1
)

REM ============================================================================
REM ============================================================================

set Delim=%%3B
set OutputPath=%SolutionDir%Output\%TargetDir%
set ObjOutputPath=%SolutionDir%Output\obj
set Constants=%Constants:;=!Delim!%
rmdir /s/q "%OutputPath%" 2> nul
rmdir /s/q "%ObjOutputPath%" 2> nul

REM Call MSBuild to build library
%msbuild% %SolutionDir%%SolutionFile% /target:%Project% /verbosity:minimal /property:Configuration=%Configuration%;TargetFrameworkVersion=%TargetFX%;TargetFrameworkProfile=%Profile%;OutputPath=%OutputPath%\;DefineConstants=%Constants%;BaseIntermediateOutputPath=%ObjOutputPath%\;NuGetPlatform=%TargetDir% > output_rel.log
rmdir /s/q "%ObjOutputPath%"

if %ERRORLEVEL% == 0 (
    echo Compilation succeeded
) else (
    echo Compilation failed
    type output_rel.log
    EXIT /B 1
)

echo.
echo.
echo.
EXIT /B %ERRORLEVEL%

set Configuration=
set Delim=
set SolutionDir=
set SolutionFile=
set Project=
set TargetFX=
set Profile=
set Constants=
