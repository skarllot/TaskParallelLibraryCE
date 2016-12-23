@echo off

set SolutionDir=%~dp0
set SolutionName=TaskParallelLibrary
set Project=TaskParallel

REM Cleanup output directory
rmdir /s/q "%SolutionDir%Output" 2> nul
mkdir "%SolutionDir%Output"

CALL %SolutionDir%tools\build-wince.bat %SolutionDir% %SolutionName% %Project% || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net35-client || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net40 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net45 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net451 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net452 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net46 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net461 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% net462 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% netstandard1.0 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% netstandard1.3 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% profile259 || EXIT /B 1
CALL %SolutionDir%tools\build-dotnet.bat %SolutionDir% %Project% profile328 || EXIT /B 1

echo build complete.
echo.
EXIT /B %ERRORLEVEL%
