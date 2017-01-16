internal static class Consts
{
    public const string Product = "Task Parallel Library for Compact Framework 3.5";
    public const string Description = "Provides a Task class for Compact Framework 3.5 with similar functionality as provided by Framework 4. For another framework it simply forwards the type to corresponding assembly";
    public const string AssemblyVersion = "3.0.0";
    public const string FileVersion = "3.0.3";
    public const string ProductVersion = "3.0.3";
    public const string Copyright = "© Fabrício Godoy. All rights reserved.";
    public const string Company = "Fabrício Godoy";

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

#if DEBUG
    public const string Configuration = "Debug";
#else
    public const string Configuration = "Retail";
#endif

    public const string PublicKey =
        @"00240000048000001402000006020000002400005253413100100000010001002d0bf3a319ee3a" +
"c3bf1e2ab86e68a579d2664e25f90dde08805439e8f2d059d59dc18b27edac451faa883c9fbbaf" +
"9fadae327d39a5a6c5e2935c9ebe937de28ba4be8c67a56195deff13194835558e5d47a7354158" +
"37cbaed2b68cc67f1842a30802f29f315de5e202123998b172e71970929e38bab7bac164fec07e" +
"7a3c55edbc8046d7a34785647af0b191a39c324f7d7cc6867e27235ce3817cf4ad24bfd79db2eb" +
"7e4536be4f73560261b274cc4fd50e7dcd19c6b82e8b0108ae72335e47242953935015bd883735" +
"a94dbf730027a35e095442703e32db7f233f47933ac3c4674b7d8193059f9ee4044b1d91d0a015" +
"8cd992e71598bd19b04cedc0adf78f2c46b6b3c3eaf5c7b23cad2785b3fa7fa36a39a6408a9af9" +
"e834bb308f56c448ef57ee50d58da001ce9914c74201dc42331ae1c4257aeaf6ed91efca0578f9" +
"e62a9566d3f96341c2fc6bd13628ecc28354f7b35182dfb7b3305e69984122c676e8febeb80ae2" +
"9a0728b4cb959f4540f46fb1b9fca6bb1dae93d2d800f14ea357a953deb16df3f058d335163c53" +
"28667a291640d4361df02a405eb94f8eee45513660a4063f7a9b3dd3899753f10cdb10c2a33799" +
"9a9ac82d634437958694391ef159ee19588846c5dafdd9669d337f8394ee1aa0042e9d6baae88d" +
"279ecb2e9007d4e078c2b8763cea6ec8a47030eb2cf4db12462071d8542d1c51e2256543e4";
}