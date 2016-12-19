internal static class Consts
{
    public const string Description = "Provides a backport of Task<T> for .NET 3.5 Compact Edition";
    public const string Company = "Fabrício Godoy";
    public const string Product = "TaskParallelCE";
    public const string Copyright = "Copyright © Fabrício Godoy 2016";
    public const bool ComVisible = false;
    public const string Version = "2.0.0";

#if WindowsCE
    public const string Title = "TaskParallel (.NET 3.5 CF)";
    public const string Guid = "fee8e6ad-84e3-4cb0-93ac-a63c25257b0e";
#elif PCL
    public const string Title = "TaskParallel (PCL)";
#elif NET46
    public const string Title = "TaskParallel (.NET 4.6)";
    public const string Guid = "af5dbcb5-3253-4476-97c1-8c78f19076d7";
#elif NET45
    public const string Title = "TaskParallel (.NET 4.5)";
    public const string Guid = "13688ed7-5e76-42d8-8218-ca959998c847";
#elif NET40
    public const string Title = "TaskParallel (.NET 4.0)";
    public const string Guid = "2f8828d1-c200-4793-96d2-2847b7b47743";
#elif NET35
    public const string Title = "TaskParallel (.NET 3.5)";
    public const string Guid = "d78f0942-7465-44e3-95ec-b60ac01938b8";
#endif

#if (!PCL && !NET45) || (PCL && !NET45)
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