using System;
using System.Reflection;

namespace KomUniMunVesselRectifier
{
    internal static class ReflectionUtils
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        public static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, ReflectionFlags);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        public static MethodInfo FindMethodInHierarchy(Type type, string methodName)
        {
            while (type != null)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    ReflectionFlags,
                    null,
                    Type.EmptyTypes,
                    null
                );
                if (method != null)
                    return method;
                type = type.BaseType;
            }
            return null;
        }
    }
}
