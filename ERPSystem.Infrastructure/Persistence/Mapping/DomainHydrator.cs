using System.Reflection;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class DomainHydrator
{
    public static T Create<T>() where T : class =>
        (Activator.CreateInstance(typeof(T), nonPublic: true) as T)!;

    public static void Set<T>(T instance, string propertyName, object? value)
    {
        var type = typeof(T);
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetField($"_{char.ToLower(propertyName[0])}{propertyName[1..]}", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }
}
