using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class CartesianProduct
    {
        /// <summary>
        /// <para>
        /// Calculate the set of all ordered pairs (a,b)
        /// where a is in A and b is in B.
        /// </para>
        /// <para>
        /// The Cartesian Product of the sets of playing card ranks
        /// {A, K, Q, J, 10, 9, 8, 7, 6, 5, 4, 3, 2}
        /// and playing card suits {♠, ♥, ♦, ♣}
        /// forms a complete deck of cards {(A,♠), (A,♥), ..., (2, ♦), (2, ♣)}
        /// </para>
        /// </summary>
        public static CartesianProduct<TA, TB> OfEnums<TA, TB>()
            where TA : struct, Enum
            where TB : struct, Enum
            => new CartesianProduct<TA, TB>(GetValues<TA>(), GetValues<TB>());

        public static T[] GetValues<T>() where T : struct, Enum
            => (T[])Enum.GetValues(typeof(T));

        public static CartesianProduct<TA, TB> Of<TA, TB>(
            IEnumerable<TA> sequence1,
            IEnumerable<TB> sequence2)
            =>
            new CartesianProduct<TA, TB>(sequence1, sequence2);
    }

    public class CartesianProduct<TA, TB>
    {
        private readonly IEnumerable<TA> A;
        private readonly IEnumerable<TB> B;

        public CartesianProduct(IEnumerable<TA> sequence1, IEnumerable<TB> sequence2)
        {
            A = sequence1;
            B = sequence2;
        }

        public IEnumerable<TResult> Using<TResult>(Func<TA, TB, TResult> combine)
            => from a in A
               from b in B
               select combine(a, b);
    }
}