using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle(Consts.Title)]
[assembly: AssemblyDescription(Consts.Description)]
[assembly: AssemblyConfiguration(Consts.Configuration)]
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

[assembly: AssemblyVersion(Consts.AssemblyVersion)]
[assembly: AssemblyInformationalVersion(Consts.ProductVersion)]
#if !WindowsCE
[assembly: AssemblyFileVersion(Consts.FileVersion)]
#endif

[assembly: InternalsVisibleTo("TaskParallel.Tests, PublicKey=" + Consts.PublicKey)]

#if CLASSIC
[assembly: AssemblyKeyFile(@"..\..\..\tools\keypair.snk")]
[assembly: AssemblyDelaySign(true)]
#endif