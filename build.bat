@echo off
setlocal enabledelayedexpansion
set SolutionDir=%~dp0
set SolutionFileFull=TaskParallelLibrary.sln
set SolutionFileCF=TaskParallelLibrary.vs2008.sln
set GLOBALERROR=0
echo building all projects in the solution (Release)

rmdir /s/q "%SolutionDir%Output" 2> nul
mkdir "%SolutionDir%Output"

REM ============================================================================
REM Compact .NET Framework
REM ============================================================================
set net35path="C:\Windows\Microsoft.NET\Framework\v3.5"
set msbuild="%net35path%\MSBuild.exe"
set targetscf="%net35path%\Microsoft.CompactFramework.CSharp.targets"
if not exist %msbuild% (
	echo Error trying to find MSBuild 3.5 executable
	exit /B 1
)
if not exist %targetscf% (
	echo Error trying to find Compact Framework targets
    echo.
	echo Install '.NET Compact Framework Redistributable'
	echo and 'Power Toys for .NET Compact Framework 3.5'
	exit /B 1
)
set SolutionFile=%SolutionFileCF%

CALL :Build TaskParallel_Compact Release v3.5 "" net35-cf "TRACE;WindowsCE"

REM ============================================================================
REM Full .NET Framework
REM ============================================================================
set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild"
if not exist %msbuild% (
	set msbuild="C:\Program Files (x86)\MSBuild\12.0\bin\msbuild"
)
if not exist %msbuild% (
	echo Error trying to find MSBuild executable
	exit /B 1
)
set SolutionFile=%SolutionFileFull%

CALL :Build TaskParallel_Full WINCE v3.5 Client net35-cf-test "TRACE;WindowsCE"
CALL :Build TaskParallel_Full Release v3.5 Client net35 "TRACE;NET35"
CALL :Build TaskParallel_Full Release v4.0 "" net40 "TRACE;NET35;NET40"
CALL :Build TaskParallel_Full Release v4.5 "" net45 "TRACE;NET35;NET40;NET45"
CALL :Build TaskParallel_Full Release v4.5.1 "" net451 "TRACE;NET35;NET40;NET45"
CALL :Build TaskParallel_Full Release v4.5.2 "" net452 "TRACE;NET35;NET40;NET45"
CALL :Build TaskParallel_Full Release v4.6 "" net46 "TRACE;NET35;NET40;NET45;NET46"
CALL :Build TaskParallel_Full Release v4.6.1 "" net461 "TRACE;NET35;NET40;NET45;NET46"
CALL :Build TaskParallel_Full Release v4.6.2 "" net462 "TRACE;NET35;NET40;NET45;NET46" optional
CALL :Build TaskParallel_PCL DIST40 v4.0 Profile136 portable-net40+sl5+win8+wp8+MonoAndroid+MonoTouch+XamariniOS "TRACE;NET35;NET40;PCL;Profile136"
CALL :Build TaskParallel_PCL Release v4.5 Profile7 portable-net45+win8+MonoAndroid+MonoTouch+XamariniOS "TRACE;NET35;NET40;NET45;PCL;Profile7" optional
CALL :Build TaskParallel_PCL Release v4.5 Profile111 portable-net45+win8+wpa81+MonoAndroid+MonoTouch+XamariniOS "TRACE;NET35;NET40;NET45;PCL;Profile111" optional
CALL :Build TaskParallel_PCL Release v4.5 Profile259 portable-net45+win8+wpa81+wp8+MonoAndroid+MonoTouch+XamariniOS "TRACE;NET35;NET40;NET45;PCL;Profile259"
CALL :Build TaskParallel_PCL Release v4.6 Profile44 portable-net451+win81+MonoAndroid+MonoTouch+XamariniOS "TRACE;NET35;NET40;NET45;PCL;Profile44"

echo build complete.

EXIT /B %GLOBALERROR%

:Build
set Project=%~1
set Configuration=%~2
set TargetFX=%~3
set Profile=%~4
set TargetDir=%~5
set Constants=%~6
set Optional=%~7
set Delim=%%3B

if %GLOBALERROR% == 1 (
	EXIT /B %ERRORLEVEL%
)

set OutputPath=%SolutionDir%Output\%TargetDir%
set ObjOutputPath=%SolutionDir%Output\obj
set Constants=%Constants:;=!Delim!%
<nul set /p ="building %TargetFX% %Profile% (%TargetDir%) "
rmdir /s/q "%OutputPath%" 2> nul
%msbuild% %SolutionDir%%SolutionFile% /target:%Project% /verbosity:minimal /property:Configuration=%Configuration%;TargetFrameworkVersion=%TargetFX%;TargetFrameworkProfile=%Profile%;OutputPath=%OutputPath%\;DefineConstants=%Constants%;BaseIntermediateOutputPath=%ObjOutputPath%\ > output_rel.log
rmdir /s/q "%ObjOutputPath%"

if %ERRORLEVEL% == 0 (
	echo [  OK  ]
) else (
    if "%Optional%" == "optional" (
        echo [ SKIP ]
    ) else (
        echo [ FAIL ]
        type output_rel.log
        set GLOBALERROR=1
    )
)

REM Clean variables
set Project=
set Configuration=
set TargetFX=
set Profile=
set TargetDir=
set Constants=
set Optional=
set OutputPath=
set ObjOutputPath=
EXIT /B %ERRORLEVEL%
