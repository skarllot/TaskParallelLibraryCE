@echo off
setlocal enabledelayedexpansion

REM Capture parameters
set SolutionDir=%~1
set SolutionName=%~2
set Project=%~3
set TargetDir=%~4
set Configuration=%~5

REM ============================================================================
REM .NET Compact Framework
REM ============================================================================
set net35path="C:\Windows\Microsoft.NET\Framework\v3.5"
set msbuild35="%net35path%\MSBuild.exe"
set targetscf="%net35path%\Microsoft.CompactFramework.CSharp.targets"

REM ============================================================================
REM .NET Framework (Full)
REM ============================================================================
set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild"
if not exist %msbuild% (
	set msbuild="C:\Program Files (x86)\MSBuild\12.0\bin\msbuild"
)

REM ============================================================================
REM Check tools availability
REM ============================================================================
if not exist %msbuild% (
	echo Error trying to find MSBuild executable
	EXIT /B 1
)

if "%TargetDir%" == "net35-cf" (
	if not exist %targetscf% (
		echo Error trying to find Compact Framework targets
		echo.
		echo Install '.NET Compact Framework Redistributable'
		echo and 'Power Toys for .NET Compact Framework 3.5'
		exit /B 1
	)
)

REM ============================================================================
REM Setup variables according to target
REM ============================================================================
if "%TargetDir%" == "net35-cf" (
	set SolutionName=%SolutionName%.vs2008
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
	GOTO SetupEnd
)

if "%TargetDir%" == "net35" (
	set TargetFX=v3.5
	set Profile=Client
	set Constants="TRACE;NET35"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net40" (
	set TargetFX=v4.0
	set Profile=
	set Constants="TRACE;NET35;NET40"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net45" (
	set TargetFX=v4.5
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net451" (
	set TargetFX=v4.5.1
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net452" (
	set TargetFX=v4.5.2
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net46" (
	set TargetFX=v4.6
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45;NET46"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net461" (
	set TargetFX=v4.6.1
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45;NET46"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "net462" (
	set TargetFX=v4.6.2
	set Profile=
	set Constants="TRACE;NET35;NET40;NET45;NET46"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile2" (
	set TargetDir=portable-net40+win8+sl4+wp7
	set TargetFX=v4.0
	set Profile=Profile2
	set Constants="TRACE;NET35;NET40;PCL"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile7" (
	set TargetDir=portable-net45+win8+MonoAndroid+MonoTouch+XamariniOS
	set TargetFX=v4.5
	set Profile=Profile7
	set Constants="TRACE;NET35;NET40;NET45;PCL"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile111" (
	set TargetDir=portable-net45+win8+wpa81+MonoAndroid+MonoTouch+XamariniOS
	set TargetFX=v4.5
	set Profile=Profile111
	set Constants="TRACE;NET35;NET40;NET45;PCL"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile136" (
	set TargetDir=portable-net40+sl5+win8+wp8+MonoAndroid+MonoTouch+XamariniOS
	set TargetFX=v4.0
	set Profile=Profile136
	set Constants="TRACE;NET35;NET40;PCL"

	if "%Configuration%" == "" (
		set Configuration=DIST40
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile44" (
	set TargetDir=portable-net451+win81+MonoAndroid+MonoTouch+XamariniOS
	set TargetFX=v4.6
	set Profile=Profile44
	set Constants="TRACE;NET35;NET40;NET45;PCL"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

if "%TargetDir%" == "profile259" (
	set TargetDir=portable-net45+win8+wpa81+wp8+MonoAndroid+MonoTouch+XamariniOS
	set TargetFX=v4.5
	set Profile=Profile259
	set Constants="TRACE;NET35;NET40;NET45;PCL"

	if "%Configuration%" == "" (
		set Configuration=Release
	)
	GOTO SetupEnd
)

echo Unknown target: %TargetDir%
exit /B 1

:SetupEnd
REM ============================================================================
REM ============================================================================

set Delim=%%3B
set SolutionFile=%SolutionName%.sln
set OutputPath=%SolutionDir%Output\%TargetDir%
set ObjOutputPath=%SolutionDir%Output\obj
set Constants=%Constants:;=!Delim!%
<nul set /p ="building %TargetFX% %Profile% (%TargetDir%) "
rmdir /s/q "%OutputPath%" 2> nul

REM Call MSBuild to build library
%msbuild% %SolutionDir%%SolutionFile% /target:%Project% /verbosity:minimal /property:Configuration=%Configuration%;TargetFrameworkVersion=%TargetFX%;TargetFrameworkProfile=%Profile%;OutputPath=%OutputPath%\;DefineConstants=%Constants%;BaseIntermediateOutputPath=%ObjOutputPath%\;NuGetPlatform=%TargetDir% > output_rel.log
rmdir /s/q "%ObjOutputPath%"

if %ERRORLEVEL% == 0 (
	echo [  OK  ]
) else (
	echo [ FAIL ]
	type output_rel.log
	EXIT /B 1
)

EXIT /B %ERRORLEVEL%
