using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class TupleHelpers
    {
        public static Type DeconstructTupleType(this Type type)
        {
            if (type.IsConstructedGenericType && type.FullName.StartsWith(typeof(System.Tuple).FullName))
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Tuple<>))
                    return typeof(Tuple<>);
                if (genericTypeDefinition == typeof(Tuple<,>))
                    return typeof(Tuple<,>);
                if (genericTypeDefinition == typeof(Tuple<,,>))
                    return typeof(Tuple<,,>);
                if (genericTypeDefinition == typeof(Tuple<,,,>))
                    return typeof(Tuple<,,,>);
                if (genericTypeDefinition == typeof(Tuple<,,,,>))
                    return typeof(Tuple<,,,,>);
                if (genericTypeDefinition == typeof(Tuple<,,,,,>))
                    return typeof(Tuple<,,,,,>);
                if (genericTypeDefinition == typeof(Tuple<,,,,,,>))
                    return typeof(Tuple<,,,,,,>);
                if (genericTypeDefinition == typeof(Tuple<,,,,,,,>))
                    return typeof(Tuple<,,,,,,,>);
                return null;
            }
            return null;
        }

        private static IEnumerable<Type> GetTupleTypesRec(this Type type)
        {
            if (typeof(ITuple).IsAssignableFrom(type))
            {
                var actualType = type;
                while (actualType.DeconstructTupleType() == null)
                {
                    actualType = actualType.BaseType;
                    if (actualType == null)
                    {
                        yield return type;
                        yield break;
                    }
                }
                foreach (var subType in actualType.GenericTypeArguments)
                {
                    foreach (var subSubType in subType.GetTupleTypesRec())
                        yield return subSubType;
                }
            }
            else
            {
                yield return type;
                yield break;
            }
        }

        public static IEnumerable<Type> GetTupleTypes(this Type type)
        {
            if (typeof(ITuple).IsAssignableFrom(type))
            {
                var actualType = type;
                while (actualType.DeconstructTupleType() == null)
                {
                    actualType = actualType.BaseType;
                    if (actualType == null)
                    {
                        return null;
                    }
                }
                var result = Enumerable.Empty<Type>();
                foreach (var subType in actualType.GenericTypeArguments)
                {
                    result = result.Concat(subType.GetTupleTypesRec());
                }
                return result;
            }
            return null;
        }

        public static IEnumerable<Type> GetTypes(this ITuple tuple)
        {
            if (tuple.Length < 1 || tuple.Length > 8)
                throw new ArgumentException($"Argument passed is a bad ITuple. Number of arguments is {tuple.Length} (min = 1, max = 8).", nameof(tuple));
            var result = tuple.GetType().GetTupleTypes();

            if (result == null)
                throw new ArgumentException($"Argument passed is a bad ITuple. Not convertable to a Tuple<...> type.", nameof(tuple));

            return result;
        }
    }
}
