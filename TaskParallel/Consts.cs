internal static class Consts
{
    public const string Description = "Provides a Task class for Compact Framework 3.5 with similar functionality as provided by Framework 4. For another framework it simply forwards the type to corresponding assembly";
    public const string Company = "Fabrício Godoy";
    public const string Product = "TaskParallelCE";
    public const string Copyright = "Copyright © Fabrício Godoy 2016";
    public const string Version = "3.0.2";

#if WindowsCE
    public const string Title = "TaskParallel (Net Compact 3.5)";
#elif NETSTANDARD1_3
    public const string Title = "TaskParallel (NetStandard 1.3)";
#elif NETSTANDARD1_0
    public const string Title = "TaskParallel (NetStandard 1.0)";
#elif Profile259
    public const string Title = "TaskParallel (PCL Profile259)";
#elif Profile328
    public const string Title = "TaskParallel (PCL Profile328)";
#elif NET46
    public const string Title = "TaskParallel (Net 4.6)";
#elif NET45
    public const string Title = "TaskParallel (Net 4.5)";
#elif NET40
    public const string Title = "TaskParallel (Net 4.0)";
#elif NET35
    public const string Title = "TaskParallel (Net 3.5)";
#endif

#if NET35 || NET40 || WindowsCE || Profile259 || Profile328
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