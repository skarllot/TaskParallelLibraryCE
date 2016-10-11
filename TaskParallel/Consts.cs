internal static class Consts
{
    public const string Description = "Provides a backport of Task<T> for .NET 3.5 Compact Edition";
    public const string Company = "Fabrício Godoy";
    public const string Product = "TaskParallelCE";
    public const string Copyright = "Copyright © Fabrício Godoy 2016";
    public const bool ComVisible = false;
    public const string Version = "1.1.0.0";

#if WindowsCE
    public const string Title = "TaskParallel (Compact Edition)";
    public const string Guid = "fee8e6ad-84e3-4cb0-93ac-a63c25257b0e";
#else
    public const string Title = "TaskParallel (Full Profile)";
    public const string Guid = "d78f0942-7465-44e3-95ec-b60ac01938b8";
#endif
}