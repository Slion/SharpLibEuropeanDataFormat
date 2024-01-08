using System.IO;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7e8b16eb-eb43-4a0f-b4aa-d00bfe41ebe2")]

namespace WindowsDesktop.Properties;

internal static class AssemblyInfo
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static string? _title;
    private static string? _description;
    private static string? _company;
    private static string? _product;
    private static string? _copyright;
    private static string? _trademark;
    private static string? _versionString;

    public static string Title
        => _title ??= Prop<AssemblyTitleAttribute>(x => x.Title);

    public static string Description
        => _description ??= Prop<AssemblyDescriptionAttribute>(x => x.Description);

    public static string Company
        => _company ??= Prop<AssemblyCompanyAttribute>(x => x.Company);

    public static string Product
        => _product ??= Prop<AssemblyProductAttribute>(x => x.Product);

    public static string Copyright
        => _copyright ??= Prop<AssemblyCopyrightAttribute>(x => x.Copyright);

    public static string Trademark
        => _trademark ??= Prop<AssemblyTrademarkAttribute>(x => x.Trademark);

    public static Version Version
        => _assembly.GetName().Version ?? new Version();

    public static string VersionString
        => _versionString ??= Version.ToString(3);

    private static string Prop<T>(Func<T, string> propSelector)
        where T : Attribute
    {
        var attribute = _assembly.GetCustomAttribute<T>();
        return attribute != null ? propSelector(attribute) : "";
    }
}

internal static class LocationInfo
{
    private static DirectoryInfo? _localAppData;

    internal static DirectoryInfo LocalAppData
        => _localAppData ??= new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AssemblyInfo.Company, AssemblyInfo.Product));
}

