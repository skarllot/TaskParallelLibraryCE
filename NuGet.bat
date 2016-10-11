echo off

echo Copying assemblies into directories
set nuget_folder=NuGet\TaskParallelLibraryCE
rmdir /s/q "%nuget_folder%\lib"

echo Copying Full Framework assemblies...
set platform=lib\net35
mkdir "%nuget_folder%\%platform%"
copy "Output\Release\Full\TaskParallel\TaskParallel.*" "%nuget_folder%\%platform%\*.*"
mkdir "%nuget_folder%\%platform%\pt"
copy "Output\Release\Full\TaskParallel\pt\TaskParallel.*" "%nuget_folder%\%platform%\pt\*.*"

echo Copying Compact Framework assemblies...
set platform=lib\net35-cf
mkdir "%nuget_folder%\%platform%"
copy "Output\Release\Compact\TaskParallel\TaskParallel.*" "%nuget_folder%\%platform%\*.*"
mkdir "%nuget_folder%\%platform%\pt"
copy "Output\Release\Compact\TaskParallel\pt\TaskParallel.*" "%nuget_folder%\%platform%\pt\*.*"



echo Copy complete. Starting NuGet packaging...

cd NuGet
"nuget.exe" pack TaskParallelLibraryCE/TaskParallelLibraryCE.nuspec
cd..

echo Packaging complete