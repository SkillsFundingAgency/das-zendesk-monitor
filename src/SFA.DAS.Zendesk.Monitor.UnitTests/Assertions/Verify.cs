﻿using System.Linq;

namespace NSubstitute
{
    using FluentAssertions.Execution;
    using NSubstitute.Core;
    using NSubstitute.Core.Arguments;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Verify
    {
        private readonly static ArgumentFormatter DefaultArgumentFormatter = new ArgumentFormatter();

        /// <summary>
        /// Verify the NSubstitute call using FluentAssertions.
        /// </summary>
        /// <example>
        /// <code>
        /// var sub = Substitute.For&lt;ISomeInterface&gt;();
        /// sub.InterestingMethod("Hello hello");
        ///
        /// sub.Received().InterestingMethod(Verify.That&lt;string&gt;(s => s.Should().StartWith("hello").And.EndWith("goodbye")));
        /// </code>
        /// Results in the failure message:
        /// <code>
        /// Expected to receive a call matching:
        ///     SomeMethod("
        /// Expected string to start with
        /// "hello", but
        /// "Hello hello" differs near "Hel" (index 0).
        /// Expected string
        /// "Hello hello" to end with
        /// "goodbye".")
        /// Actually received no matching calls.
        /// Received 1 non-matching call(non-matching arguments indicated with '*' characters):
        ///     SomeMethod(*"Hello hello"*)
        /// </code>
        /// </example>
        /// <typeparam name="T">Type of argument to verify.</typeparam>
        /// <param name="action">Action in which to perform FluentAssertions verifications.</param>
        /// <returns></returns>
        public static T That<T>(Action<T> action)
            => ArgumentMatcher.Enqueue(new AssertionMatcher<T>(action));

        //=> ArgumentMatcher.Enqueue(new GenericToNonGenericMatcherProxyWithDescribe<T>(new AssertionMatcher<T>(action)));

        private class AssertionMatcher<T> : IArgumentMatcher<T>, IDescribeNonMatches
        {
            private readonly Action<T> assertion;
            private string allFailures = "";

            public AssertionMatcher(Action<T> assertion)
                => this.assertion = assertion;

            public bool IsSatisfiedBy(T argument)
            {
                using (var scope = new AssertionScope())
                {
                    try
                    {
                        assertion((T)argument);
                    }
                    catch (Exception exception)
                    {
                        var f = scope.Discard();
                        allFailures = f.Any() ? AggregateFailures(f) : exception.Message;
                        return false;
                    }

                    var failures = scope.Discard().ToList();

                    if (failures.Count == 0) return true;

                    allFailures = String.Join("\n", failures);
                    //allFailures = AggregateFailures(failures);

                    return false;
                }
            }

            private string AggregateFailures(IEnumerable<string> discard)
                => discard.Aggregate(allFailures, (a, b) => a + "\n" + b);

            public override string ToString()
                => DefaultArgumentFormatter.Format(allFailures, false);

            public string DescribeFor(object argument) => ToString();
        }
    }
}