using System;
using Xunit;

namespace Focus.Core.Tests
{
    public class RecordKeyTests
    {
        public class EqualityComparisons
        {
            [Fact]
            public void WhenMatchedCaseSensitive_IsEqual()
            {
                var key = new RecordKey("Dawnguard.esm", "0129cf");
                var sameTypeKey = new RecordKey("Dawnguard.esm", "0129cf");
                var otherTypeKey = new ExternalKey("Dawnguard.esm", "0129cf");

                Assert.True(key.Equals(sameTypeKey));
                Assert.True(key == sameTypeKey);
                Assert.True(RecordKey.Equals(key, sameTypeKey));
                Assert.True(key.Equals(otherTypeKey));
                Assert.True(key == otherTypeKey);
                Assert.True(RecordKey.Equals(key, otherTypeKey));
            }

            [Fact]
            public void WhenMatchedCaseInsensitive_IsEqual()
            {
                var key = new RecordKey("Dawnguard.esm", "0129cf");
                var sameTypeKey = new RecordKey("dawnguard.ESM", "0129CF");
                var otherTypeKey = new ExternalKey("DaWnGuArD.esm", "0129cf");

                Assert.True(key.Equals(sameTypeKey));
                Assert.True(key == sameTypeKey);
                Assert.True(RecordKey.Equals(key, sameTypeKey));
                Assert.True(key.Equals(otherTypeKey));
                Assert.True(key == otherTypeKey);
                Assert.True(RecordKey.Equals(key, otherTypeKey));
            }

            [Fact]
            public void WhenPluginNotMatched_IsNotEqual()
            {
                var key = new RecordKey("Dawnguard.esm", "0129cf");
                var sameTypeKey = new RecordKey("Skyrim.esm", "0129cf");
                var otherTypeKey = new ExternalKey("Dragonborn.esm", "0129cf");

                Assert.False(key.Equals(sameTypeKey));
                Assert.False(key == sameTypeKey);
                Assert.False(RecordKey.Equals(key, sameTypeKey));
                Assert.False(key.Equals(otherTypeKey));
                Assert.False(key == otherTypeKey);
                Assert.False(RecordKey.Equals(key, otherTypeKey));
            }

            [Fact]
            public void WhenFormIdNotMatched_IsNotEqual()
            {
                var key = new RecordKey("Dawnguard.esm", "0129cf");
                var sameTypeKey = new RecordKey("Dawnguard.esm", "0139cf");
                var otherTypeKey = new ExternalKey("Dawnguard.esm", "abc123");

                Assert.False(key.Equals(sameTypeKey));
                Assert.False(key == sameTypeKey);
                Assert.False(RecordKey.Equals(key, sameTypeKey));
                Assert.False(key.Equals(otherTypeKey));
                Assert.False(key == otherTypeKey);
                Assert.False(RecordKey.Equals(key, otherTypeKey));
            }
        }

        public class Parse
        {
            [Fact]
            public void WhenValueIsNull_Throws()
            {
                Assert.Throws<ArgumentNullException>(() => RecordKey.Parse(null));
            }

            [Fact]
            public void WhenValueHasNoSeparator_Throws()
            {
                Assert.Throws<ArgumentException>(() => RecordKey.Parse("invalid"));
            }

            [Fact]
            public void WhenValueHasTooManySeparators_Throws()
            {
                Assert.Throws<ArgumentException>(() => RecordKey.Parse("foo:bar:baz"));
            }

            [Fact]
            public void WhenValueHasSingleSeparator_IncludesEachSide()
            {
                Assert.Equal(new RecordKey("bar", "foo"), RecordKey.Parse("foo:bar"));
            }
        }

        [Fact]
        public void GetHashCode_IsCaseInsensitive()
        {
            Assert.Equal(
                new RecordKey("foo.esm", "1A2B3C").GetHashCode(),
                new RecordKey("Foo.ESM", "1a2b3C").GetHashCode());
        }

        [Fact]
        public void PluginNameEquals_IsCaseInsensitive()
        {
            var key = new RecordKey("Foo.esm", "dummy");
            Assert.True(key.PluginEquals("Foo.esm"));
            Assert.True(key.PluginEquals("foo.ESM"));
            Assert.False(key.PluginEquals("Bar.esm"));
        }

        [Fact]
        public void ToString_IsColonDelimitedPair()
        {
            Assert.Equal("0123456:Foo.esp", new RecordKey("Foo.esp", "0123456").ToString());
        }

        [Fact]
        public void WhenConstructedFromExternalKey_HasSameValues()
        {
            var key = new RecordKey(new ExternalKey("Skyrim.esm", "123456"));
            Assert.Equal("Skyrim.esm", key.BasePluginName);
            Assert.Equal("123456", key.LocalFormIdHex);
        }
    }

    record ExternalKey(string BasePluginName, string LocalFormIdHex) : IRecordKey { }
}
