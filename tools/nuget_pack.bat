@echo off

REM Capture parameters
set ScriptDir=%~dp0
set SolutionDir=%~1
set AssemblyName=%~2

set NuGetCommand=%ScriptDir%NuGet.exe
set VersionInfoCommand=%ScriptDir%VersionInfo.vbs
set output=%SolutionDir%Output
set nuget_nuspec=%SolutionDir%%AssemblyName%.nuspec
set nuget_folder=%SolutionDir%.nuget\%AssemblyName%

if not exist %nuget_nuspec% (
    echo The nuspec file could not be found at: %nuget_nuspec%
    EXIT /B 1
)

for /f %%i in ('cscript //nologo %VersionInfoCommand% %output%\net46\%AssemblyName%.dll') do set AssemblyVersion=%%i

mkdir "%nuget_folder%" > nul 2>&1
del "%nuget_folder%\%AssemblyName%.nuspec" > nul 2>&1
copy "%nuget_nuspec%" "%nuget_folder%" > nul

REM %NuGetCommand% pack %nuget_folder%\%AssemblyName%.nuspec -Version %AssemblyVersion% -Symbols -OutputDirectory %output%
%NuGetCommand% pack %nuget_folder%\%AssemblyName%.nuspec -Version %AssemblyVersion% -OutputDirectory %output%

rmdir /s/q "%SolutionDir%.nuget" > nul 2>&1
