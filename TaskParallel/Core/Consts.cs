internal static class Consts
{
    public const string Description = "Provides a backport of Task<T> for .NET 3.5 Compact Edition";
    public const string Company = "Fabrício Godoy";
    public const string Product = "TaskParallelCE";
    public const string Copyright = "Copyright © Fabrício Godoy 2016";
    public const bool ComVisible = false;
    public const string Version = "3.0.0";

#if WindowsCE
    public const string Title = "TaskParallel (.NET 3.5 CF)";
#elif NETSTANDARD1_0
    public const string Title = "TaskParallel (Standard 1.0)";
#elif NETSTANDARD1_3
    public const string Title = "TaskParallel (Standard 1.3)";
#elif NET46
    public const string Title = "TaskParallel (.NET 4.6)";
#elif NET45
    public const string Title = "TaskParallel (.NET 4.5)";
#elif NET40
    public const string Title = "TaskParallel (.NET 4.0)";
#elif NET35
    public const string Title = "TaskParallel (.NET 3.5)";
#endif

#if NET35 || NET40 || WindowsCE
    /// <summary>
    /// Gets the type itself (for Resource compatibility).
    /// </summary>
    /// <param name="type">A type.</param>
    /// <returns>The type itself.</returns>
    public static System.Type GetTypeInfo(this System.Type type)
    {
        return type;
    }
#endif
}