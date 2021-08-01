using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Focus.Providers.Mutagen.Tests
{
    // Normally, we should be able to create a pass-through mock using e.g.
    //     var mock = new Mock<SkyrimMod>().As<ISkyrimMod>().Object
    //
    // However, due to some as-yet unidentified conflict between Moq, Castle Dynamic Proxy and Mutagen, possibly related
    // to the unusual combinations of generic constraints, this will throw a `BadImageFormatException`.
    //
    // The alternative is to use Castle DP directly, which won't give us a mock, but if all we need to do is track calls,
    // then this simplified interceptor may be good enough for many cases.
    class InvocationTracker<T> : StandardInterceptor
    {
        private readonly List<IInvocation> calls = new();

        public IEnumerable<IInvocation> All()
        {
            return calls;
        }

        public IEnumerable<IInvocation> Property<TReturn>(Expression<Func<T, TReturn>> expr)
        {
            if (expr.Body is not MemberExpression member || member.Member is not PropertyInfo property)
                throw new ArgumentException("Expression must be a property accessor.", nameof(expr));
            var getter = property.GetGetMethod();
            return calls.Where(x => x.Method == getter);
        }

        protected override void PostProceed(IInvocation invocation)
        {
            base.PostProceed(invocation);
            calls.Add(invocation);
        }
    }
}
