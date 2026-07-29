using System.IO;
using System.Linq;
using UniverseLib.Utility;
using Xunit;

namespace UnityExplorer.Tests
{
    public class IOUtilityTests
    {
        [Fact]
        public void EnsureValidFilename_StripsInvalidCharacters()
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            // Skip on the unusual platform where no characters are considered invalid.
            if (invalid.Length == 0)
                return;

            string dirty = "sa" + invalid[0] + "ve" + invalid[invalid.Length - 1] + "d.log";
            string cleaned = IOUtility.EnsureValidFilename(dirty);

            Assert.DoesNotContain(cleaned, c => invalid.Contains(c));
            Assert.Equal("saved.log", cleaned);
        }

        [Fact]
        public void EnsureValidFilename_LeavesCleanNameUnchanged()
        {
            Assert.Equal("report_2026.txt", IOUtility.EnsureValidFilename("report_2026.txt"));
        }
    }
}
