using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle(Consts.Title)]
[assembly: AssemblyDescription(Consts.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Consts.Company)]
[assembly: AssemblyProduct(Consts.Product)]
[assembly: AssemblyCopyright(Consts.Copyright)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if !Profile259 && !Profile328
// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion(Consts.Version)]
#if !WindowsCE
[assembly: AssemblyFileVersion(Consts.Version)]
#endif

[assembly: InternalsVisibleTo("TaskParallel.Tests")]