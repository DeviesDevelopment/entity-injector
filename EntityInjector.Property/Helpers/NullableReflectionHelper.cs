using System.Reflection;

namespace EntityInjector.Property.Helpers;

internal static class NullableReflectionHelper
{
    private static readonly Type? NullabilityContextType =
        Type.GetType("System.Reflection.NullabilityInfoContext, System.Private.CoreLib");

    private static readonly MethodInfo? CreateMethod =
        NullabilityContextType?.GetMethod("Create", [typeof(MemberInfo)]);

    private static readonly PropertyInfo? WriteStateProperty =
        Type.GetType("System.Reflection.NullabilityInfo, System.Private.CoreLib")?
            .GetProperty("WriteState");

    private static readonly object? NullableStateNullable =
        Enum.GetValues(Type.GetType("System.Reflection.NullabilityState, System.Private.CoreLib")!)
            .Cast<object>().FirstOrDefault(x => x.ToString() == "Nullable");

    public static bool IsNullable(PropertyInfo prop)
    {
        try
        {
            if (NullabilityContextType == null || CreateMethod == null || WriteStateProperty == null)
                return true; // fallback: assume nullable

            var context = Activator.CreateInstance(NullabilityContextType);
            var nullabilityInfo = CreateMethod.Invoke(context, new object[] { prop });

            var writeState = WriteStateProperty.GetValue(nullabilityInfo);

            return Equals(writeState, NullableStateNullable);
        }
        catch
        {
            return true; // fallback: assume nullable
        }
    }
}