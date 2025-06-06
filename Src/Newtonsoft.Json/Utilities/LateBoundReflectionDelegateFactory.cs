#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

#if !HAVE_LINQ
using Newtonsoft.Json.Utilities.LinqBridge;
#endif

namespace Newtonsoft.Json.Utilities
{
    internal class LateBoundReflectionDelegateFactory : ReflectionDelegateFactory
    {
        private static readonly LateBoundReflectionDelegateFactory _instance = new LateBoundReflectionDelegateFactory();

        internal static ReflectionDelegateFactory Instance => _instance;

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override ObjectConstructor<object> CreateParameterizedConstructor(MethodBase method)
        {
            ValidationUtils.ArgumentNotNull(method, nameof(method));

            if (method is ConstructorInfo c)
            {
                // don't convert to method group to avoid medium trust issues
                // https://github.com/JamesNK/Newtonsoft.Json/issues/476
                return a => c.Invoke(a);
            }

            return a => method.Invoke(null, a)!;
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override MethodCall<T, object?> CreateMethodCall<T>(MethodBase method)
        {
            ValidationUtils.ArgumentNotNull(method, nameof(method));

            if (method is ConstructorInfo c)
            {
                return (o, a) => c.Invoke(a);
            }

            return (o, a) => method.Invoke(o, a);
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override Func<T> CreateDefaultConstructor<T>(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type)
        {
            ValidationUtils.ArgumentNotNull(type, nameof(type));

            if (type.IsValueType())
            {
                return () => (T)Activator.CreateInstance(type)!;
            }

            ConstructorInfo? constructorInfo = ReflectionUtils.GetDefaultConstructor(type, true);
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Unable to find default constructor for " + type.FullName);
            }

            return () => (T)constructorInfo.Invoke(null);
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override Func<T, object?> CreateGet<T>(PropertyInfo propertyInfo)
        {
            ValidationUtils.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

            return o => propertyInfo.GetValue(o, null);
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override Func<T, object?> CreateGet<T>(FieldInfo fieldInfo)
        {
            ValidationUtils.ArgumentNotNull(fieldInfo, nameof(fieldInfo));

            return o => fieldInfo.GetValue(o);
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override Action<T, object?> CreateSet<T>(FieldInfo fieldInfo)
        {
            ValidationUtils.ArgumentNotNull(fieldInfo, nameof(fieldInfo));

            return (o, v) => fieldInfo.SetValue(o, v);
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public override Action<T, object?> CreateSet<T>(PropertyInfo propertyInfo)
        {
            ValidationUtils.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

            return (o, v) => propertyInfo.SetValue(o, v, null);
        }
    }
}