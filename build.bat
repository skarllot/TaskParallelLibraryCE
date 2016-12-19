@echo off

set SolutionDir=%~dp0
set SolutionName=TaskParallelLibrary
set ProjectDefault=TaskParallel_Full
set ProjectPortable=TaskParallel_PCL
set ProjectWinCE=TaskParallel_Compact

echo building all projects in the solution (Release)

REM Cleanup output directory
rmdir /s/q "%SolutionDir%Output" 2> nul
mkdir "%SolutionDir%Output"

CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectWinCE% net35-cf || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net35 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net40 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net45 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net451 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net452 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net46 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net461 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectDefault% net462 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectPortable% profile136 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectPortable% profile7 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectPortable% profile111 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectPortable% profile259 || EXIT /B 1
CALL %SolutionDir%tools\build.bat %SolutionDir% %SolutionName% %ProjectPortable% profile44 || EXIT /B 1

echo build complete.
EXIT /B %ERRORLEVEL%
