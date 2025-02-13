using System.Reflection;

namespace HollywoodFX;

public static class ReflectionUtils
{
    public static FieldInfo GetField<T>(object obj, string fieldName)
    {
        return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
    }
    
    public static T2 GetFieldValue<T1, T2>(object obj, string fieldName)
    {
        var field = GetField<T1>(obj, fieldName);
        return (T2)field?.GetValue(obj);
    }

    public static void SetFieldValue<T>(object obj, string fieldName, object value)
    {
        var field = GetField<T>(obj, fieldName);
        field?.SetValue(obj, value);
    }
}