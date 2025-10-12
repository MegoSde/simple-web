using System.Reflection;
using cms.Data;
using cms.Services;
namespace cms.Extensions;


public static class ComponentExtensions
{
    /// <param name="services"></param>
    /// <param name="sharedJsPath">Absolute or relative file path to shared.js</param>
    /// <param name="assemblies">Optionally restrict which assemblies to scan.</param>
    public static IServiceCollection AddComponents(
        this IServiceCollection services,
        string sharedJsPath,
        params Assembly[]? assemblies)
    {
        services.AddSingleton<IEditorComponentService>(_ =>
            new EditorComponentService(sharedJsPath, assemblies is not null && assemblies.Length > 0 ? assemblies : null));

        return services;
    }
}