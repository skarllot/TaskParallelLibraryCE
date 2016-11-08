@echo off

set SolutionDir=%~dp0
set AssemblyName=TaskParallel
set NuGetCommand=%SolutionDir%.nuget\NuGet.exe
set VersionInfoCommand=%SolutionDir%.nuget\VersionInfo.vbs
set nuget_folder=%SolutionDir%.nuget\%AssemblyName%
set output=%SolutionDir%Output
rmdir /s/q "%nuget_folder%\lib"
for /f %%i in ('cscript //nologo %VersionInfoCommand% %output%\net45\%AssemblyName%.dll') do set AssemblyVersion=%%i

CALL :CopyLibrary net35 %AssemblyName%
CALL :CopyLibrary net40 %AssemblyName%
CALL :CopyLibrary net45 %AssemblyName%
CALL :CopyLibrary net451 %AssemblyName%
CALL :CopyLibrary net452 %AssemblyName%
CALL :CopyLibrary net46 %AssemblyName%
CALL :CopyLibrary net461 %AssemblyName%
CALL :CopyLibrary net462 %AssemblyName%
CALL :CopyLibrary portable-net40+sl5+win8+wp8+MonoAndroid+MonoTouch+XamariniOS %AssemblyName%
CALL :CopyLibrary portable-net45+win8+MonoAndroid+MonoTouch+XamariniOS %AssemblyName%
CALL :CopyLibrary portable-net45+win8+wpa81+MonoAndroid+MonoTouch+XamariniOS %AssemblyName%
CALL :CopyLibrary portable-net45+win8+wpa81+wp8+MonoAndroid+MonoTouch+XamariniOS %AssemblyName%
CALL :CopyLibrary portable-net451+win81+MonoAndroid+MonoTouch+XamariniOS %AssemblyName%
CALL :CopyLibrary net35-cf %AssemblyName%

echo Copy complete. Starting NuGet packaging...

cd %SolutionDir%.nuget
REM %NuGetCommand% pack %AssemblyName%/%AssemblyName%.nuspec -Version %AssemblyVersion% -symbols
%NuGetCommand% pack %AssemblyName%/%AssemblyName%.nuspec -Version %AssemblyVersion%
cd..

echo Packaging complete

EXIT /B %ERRORLEVEL%

:CopyLibrary

set platform=%~1
set assembly=%~2

if not exist "%output%\%platform%\%assembly%.dll" (
	echo Could not find assemblies for %platform% platform
	EXIT /B %ERRORLEVEL%
)

echo Copying assemblies for %platform%...
mkdir "%nuget_folder%\lib\%platform%"
mkdir "%nuget_folder%\lib\%platform%\pt"
copy "%output%\%platform%\%assembly%.*" "%nuget_folder%\lib\%platform%\*.*"
copy "%output%\%platform%\pt\%assembly%.*" "%nuget_folder%\lib\%platform%\pt\*.*"

set platform=
set assembly=
EXIT /B %ERRORLEVEL%
