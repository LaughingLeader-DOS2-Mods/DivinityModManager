using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyTitle("DivinityModManager")]
[assembly: AssemblyDescription("A mod manager for Divinity: Original Sin 2 - Definitive Edition.")]
#if DEBUG
 [assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("LaughingLeader")]
[assembly: AssemblyProduct("Divinity Mod Manager")]
[assembly: AssemblyCopyright("Copyright © 2019")]
[assembly: AssemblyTrademark("")]
[assembly: NeutralResourcesLanguageAttribute("en")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,
	ResourceDictionaryLocation.SourceAssembly
)]

[assembly: AssemblyVersion("1.9.3.0")]