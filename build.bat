echo off
set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild"

echo building all projects in the solution (Release)

echo =========================================================  > output_rel.log
echo ==================== PCL ================================ >> output_rel.log

echo building any cpu (pcl)
%msbuild% TaskParallelLibrary.sln /property:Configuration=Release;Platform="Any CPU" /verbosity:minimal > output_rel.log

echo build complete.
