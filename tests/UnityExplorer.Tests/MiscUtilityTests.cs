using System;
using System.Text;
using UniverseLib.Utility;
using Xunit;

namespace UnityExplorer.Tests
{
    public class MiscUtilityTests
    {
        [Theory]
        [InlineData("Hello World", "hello", true)]
        [InlineData("Hello World", "WORLD", true)]
        [InlineData("Hello World", "Hello World", true)]
        [InlineData("Hello World", "", true)]
        [InlineData("Hello World", "xyz", false)]
        [InlineData("abc", "abcd", false)]
        public void ContainsIgnoreCase_MatchesCaseInsensitively(string haystack, string needle, bool expected)
        {
            Assert.Equal(expected, haystack.ContainsIgnoreCase(needle));
        }

        [Flags]
        private enum SampleFlags
        {
            None = 0,
            A = 1,
            B = 2,
            C = 4,
        }

        // Called via the static class explicitly: Enum defines a built-in
        // HasFlag(Enum) instance method, so instance-style calls would bind to
        // the BCL rather than to MiscUtility's extension.
        [Fact]
        public void HasFlag_ReturnsTrue_WhenFlagPresent()
        {
            SampleFlags value = SampleFlags.A | SampleFlags.C;
            Assert.True(MiscUtility.HasFlag(value, SampleFlags.A));
            Assert.True(MiscUtility.HasFlag(value, SampleFlags.C));
            Assert.True(MiscUtility.HasFlag(value, SampleFlags.A | SampleFlags.C));
        }

        [Fact]
        public void HasFlag_ReturnsFalse_WhenFlagAbsent()
        {
            SampleFlags value = SampleFlags.A | SampleFlags.C;
            Assert.False(MiscUtility.HasFlag(value, SampleFlags.B));
            Assert.False(MiscUtility.HasFlag(value, SampleFlags.A | SampleFlags.B));
        }

        [Theory]
        [InlineData("Hello World", "World", true)]
        [InlineData("Hello World", "Hello", false)]
        [InlineData("Hello", "Hello", true)]
        [InlineData("Hi", "Hello", false)]
        [InlineData("", "x", false)]
        public void StringBuilderEndsWith_DetectsSuffix(string content, string suffix, bool expected)
        {
            StringBuilder sb = new StringBuilder(content);
            Assert.Equal(expected, sb.EndsWith(suffix));
        }
    }
}
