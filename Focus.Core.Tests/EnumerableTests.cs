using System;
using System.Collections.Generic;
using Xunit;

namespace Focus.Core.Tests
{
    public class EnumerableTests
    {
        public class NotNull
        {
            [Fact]
            public void FiltersNullElementsFromReferenceSequence()
            {
                var dummy = new object();
                var sequence = new[] { dummy, dummy, null, dummy, null, null, dummy };
                Assert.Equal(new[] { dummy, dummy, dummy, dummy }, sequence.NotNull());
            }

            [Fact]
            public void FiltersNullValuesFromNullableSequence()
            {
                var sequence = new int?[] { 2, 4, null, null, 6, null, 8, null, null, null, 10 };
                Assert.Equal(new[] { 2, 4, 6, 8, 10 }, sequence.NotNull());
            }
        }

        public class OrderBySafe
        {
            [Fact]
            public void ProjectsNullSequence()
            {
                IEnumerable<string> nullSequence = null;
                Assert.Null(nullSequence.OrderBySafe(x => x));
            }

            [Fact]
            public void OrdersNonNullSequence()
            {
                Assert.Equal(new[] { 1, 2, 3, 4 }, new[] { 2, 4, 1, 3 }.OrderBySafe(i => i));
            }
        }

        public class SequenceEqualSafe
        {
            [Fact]
            public void WhenBothSequencesNull_IsTrue()
            {
                IEnumerable<int> lhs = null;
                IEnumerable<int> rhs = null;
                Assert.True(lhs.SequenceEqualSafe(rhs));
            }

            [Fact]
            public void WhenBothSequencesSame_IsTrue()
            {
                Assert.True(new[] { 10, 20 }.SequenceEqualSafe(new[] { 10, 20 }));
            }

            [Fact]
            public void WhenLeftSequenceNull_IsFalse()
            {
                IEnumerable<int> lhs = null;
                Assert.False(lhs.SequenceEqualSafe(new[] { 4 }));
            }

            [Fact]
            public void WhenRightSequenceNull_IsFalse()
            {
                Assert.False(new[] { 42 }.SequenceEqualSafe(null));
            }

            [Fact]
            public void WhenSequencesDifferent_IsFalse()
            {
                Assert.False(new[] { 1, 2, 3 }.SequenceEqualSafe(new[] { 1, 2 }));
                Assert.False(new[] { 1, 2 }.SequenceEqualSafe(new[] { 2, 3 }));
                Assert.False(new[] { 3, 2 }.SequenceEqualSafe(new[] { 2, 3 }));
            }
        }
    }
}
