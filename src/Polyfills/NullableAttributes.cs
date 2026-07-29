// Polyfill for the nullable-analysis attributes that ship with the BCL only on
// .NET 6+. The Mono (net35) and IL2CPP (net472) build targets used by
// UnityExplorer do not provide them, so we define the minimal subset the code
// base relies on. On net6+ the real framework types are used instead.
#if !NET6_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Specifies that when a method returns <see cref="ReturnValue"/>, the
    /// associated parameter will not be <see langword="null"/> even if the
    /// corresponding type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        public bool ReturnValue { get; }
    }
}

#endif
