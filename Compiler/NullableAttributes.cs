// Polyfill for the compiler-synthesized nullable metadata attributes.
//
// Some Il2Cpp interop assemblies publicly declare their own partial/broken copy of
// System.Runtime.CompilerServices.NullableAttribute. When the C# compiler needs to
// emit nullable metadata, it binds to that external definition instead of embedding
// its own, then can't find the constructor it expects, producing:
//     "Missing compiler required member 'NullableAttribute..ctor'".
//
// Declaring these attributes here, in your own assembly, gives the compiler a
// correct definition to use (source-declared types take priority), which satisfies
// the requirement regardless of what triggers the emission. Harmless to keep
// alongside <Nullable>disable</Nullable> / #nullable disable.

#nullable disable

using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field |
        AttributeTargets.GenericParameter | AttributeTargets.Parameter |
        AttributeTargets.Property | AttributeTargets.ReturnValue,
        AllowMultiple = false, Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;
        public NullableAttribute(byte flag) => NullableFlags = new[] { flag };
        public NullableAttribute(byte[] flags) => NullableFlags = flags;
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Delegate |
        AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct,
        AllowMultiple = false, Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;
        public NullableContextAttribute(byte flag) => Flag = flag;
    }

    [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
    internal sealed class NullablePublicOnlyAttribute : Attribute
    {
        public readonly bool IncludesInternals;
        public NullablePublicOnlyAttribute(bool includesInternals) => IncludesInternals = includesInternals;
    }
}