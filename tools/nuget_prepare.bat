@echo off

REM Capture parameters
set SolutionDir=%~1
set platform=%~2
set assembly=%~3

set nuget_folder=%SolutionDir%.nuget\%assembly%
set output=%SolutionDir%Output

REM ============================================================================
REM Translate platform name
REM ============================================================================
if "%platform%" == "profile2" (
	set platform=portable-net40+win8+sl4+wp7
	GOTO SetupEnd
)

if "%platform%" == "profile7" (
	set platform=portable-net45+win8+MonoAndroid+MonoTouch+XamariniOS
	GOTO SetupEnd
)

if "%platform%" == "profile111" (
	set platform=portable-net45+win8+wpa81+MonoAndroid+MonoTouch+XamariniOS
	GOTO SetupEnd
)

if "%platform%" == "profile136" (
	set platform=portable-net40+sl5+win8+wp8+MonoAndroid+MonoTouch+XamariniOS
	GOTO SetupEnd
)

if "%platform%" == "profile44" (
	set platform=portable-net451+win81+MonoAndroid+MonoTouch+XamariniOS
	GOTO SetupEnd
)

if "%platform%" == "profile259" (
	set platform=portable-net45+win8+wpa81+wp8+MonoAndroid+MonoTouch+XamariniOS
	GOTO SetupEnd
)

:SetupEnd
REM ============================================================================
REM ============================================================================

if not exist "%output%\%platform%\%assembly%.dll" (
	echo Could not find assemblies for %platform% platform
	EXIT /B %ERRORLEVEL%
)

echo Copying assemblies for %platform%...
rmdir /s/q "%nuget_folder%\lib\%platform%" > nul 2>&1
mkdir "%nuget_folder%\lib\%platform%" || EXIT /B 1

set nodoc=0
copy "%output%\%platform%\%assembly%.dll" "%nuget_folder%\lib\%platform%\*.*" > nul || EXIT /B 1
copy "%output%\%platform%\%assembly%.pdb" "%nuget_folder%\lib\%platform%\*.*" > nul || EXIT /B 1
copy "%output%\%platform%\%assembly%.xml" "%nuget_folder%\lib\%platform%\*.*" > nul || set nodoc=1
if %nodoc% == 1 (
    echo [WARNING] No documentation was found for %assembly%
    set ERRORLEVEL=0
)
if exist "%output%\%platform%\%assembly%.mdb" (
	copy "%output%\%platform%\%assembly%.mdb" "%nuget_folder%\lib\%platform%\*.*" > nul || EXIT /B 1
)


set platform=
set assembly=
EXIT /B %ERRORLEVEL%