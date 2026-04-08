using System.Reflection;

namespace PMQ.Mediator;

internal static class AssemblyScanner
{
    private static readonly Type[] SingleHandlerInterfaces =
    [
        typeof(IRequestHandler<,>),
        typeof(IRequestHandler<>),
        typeof(IStreamRequestHandler<,>)
    ];

    public static IReadOnlyList<Assembly> ResolveAssemblies(PmqMediatorOptions options)
    {
        if (options.Assemblies.Count > 0)
            return options.Assemblies;

        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToList();

        if (options.AssemblyPrefixes.Count > 0)
        {
            return allAssemblies
                .Where(a => options.AssemblyPrefixes.Any(p =>
                    a.GetName().Name?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
        }

        return allAssemblies;
    }

    public static IEnumerable<(Type ServiceType, Type ImplementationType)> FindSingleHandlers(IReadOnlyList<Assembly> assemblies)
    {
        foreach (var (type, iface) in ScanInterfaces(assemblies))
        {
            var genericDef = iface.GetGenericTypeDefinition();
            if (SingleHandlerInterfaces.Contains(genericDef))
                yield return (iface, type);
        }
    }

    public static IEnumerable<(Type ServiceType, Type ImplementationType)> FindNotificationHandlers(IReadOnlyList<Assembly> assemblies)
    {
        foreach (var (type, iface) in ScanInterfaces(assemblies))
        {
            if (iface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                yield return (iface, type);
        }
    }

    public static IEnumerable<(Type ServiceType, Type ImplementationType)> FindValidators(IReadOnlyList<Assembly> assemblies)
    {
        var validatorInterfaceType = typeof(FluentValidation.IValidator<>);

        var concreteTypes = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                if (iface.GetGenericTypeDefinition() == validatorInterfaceType)
                    yield return (iface, type);
            }
        }
    }

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).ToArray()!;
        }
    }

    private static IEnumerable<(Type Type, Type Interface)> ScanInterfaces(IReadOnlyList<Assembly> assemblies)
    {
        var concreteTypes = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var type in concreteTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                    yield return (type, iface);
            }
        }
    }
}
