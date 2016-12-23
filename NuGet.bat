@echo off

set SolutionDir=%~dp0
set AssemblyName=TaskParallel
set SourceCodePath=%SolutionDir%

echo Copying source files...

CALL %SolutionDir%tools\nuget_source.bat %SolutionDir% %AssemblyName% %SourceCodePath% || EXIT /B 1

echo Preparing files for packaging...

CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net35-cf %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net35-client %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net40 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net45 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net451 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net452 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net46 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net461 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% net462 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% netstandard1.0 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% netstandard1.3 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% profile259 %AssemblyName% || EXIT /B 1
CALL %SolutionDir%tools\nuget_prepare.bat %SolutionDir% profile328 %AssemblyName% || EXIT /B 1

echo Copy complete. Starting NuGet packaging...

CALL %SolutionDir%tools\nuget_pack.bat %SolutionDir% %AssemblyName% || EXIT /B 1

echo Packaging complete

EXIT /B %ERRORLEVEL%
