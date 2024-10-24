using System.Diagnostics.CodeAnalysis;
using Antelcat.AspNetCore.ProtooSharp;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

public static class ProtooServerExtension
{
    private static IServiceCollection AddRoom(this IServiceCollection collection)
    {
        collection.TryAddTransient<Room>();
        return collection;
    }

    /// <summary>
    /// Add <see cref="WebSocketServer"/> as default protoo server
    /// </summary>
    /// <param name="collection">service collection</param>
    /// <returns></returns>
    public static IServiceCollection AddProtooServer(this IServiceCollection collection) =>
        collection.AddProtooServer<WebSocketServer>();

    /// <summary>
    /// Add an instance of <see cref="TProtooServer"/> as protoo server
    /// </summary>
    /// <param name="collection">service collection</param>
    /// <param name="protooServer">protoo server</param>
    /// <typeparam name="TProtooServer"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProtooServer<TProtooServer>(
        this IServiceCollection collection,
        TProtooServer protooServer)
        where TProtooServer : WebSocketServer => collection.AddRoom().AddSingleton(protooServer);

    public static IServiceCollection AddProtooServer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProtooServer>(
        this IServiceCollection collection, WebSocketAcceptContext? config = null) 
        where TProtooServer : WebSocketServer
    {
        if (config != null) collection.TryAddSingleton(config);
        collection.AddRoom().TryAddSingleton<TProtooServer>();
        return collection;
    }

    /// <summary>
    /// Use protoo server <see cref="TProtooServer"/> in the request pipeline
    /// Call <see cref="Microsoft.AspNetCore.Builder.WebSocketMiddlewareExtensions.UseWebSockets(IApplicationBuilder)"/>
    /// before calling <see cref="MapProtooServer{TProtooServer}"/>
    /// </summary>
    /// <param name="application">application</param>
    /// <param name="pattern">route pattern</param>
    /// <typeparam name="TProtooServer">protoo server type</typeparam>
    /// <returns></returns>
    public static IEndpointConventionBuilder MapProtooServer<TProtooServer>(
        this WebApplication application,
        [StringSyntax("Route")] string pattern = "/")
        where TProtooServer : WebSocketServer
    {
        var server = application.Services.GetRequiredService<TProtooServer>();
        return application.Map(pattern, context => server.OnRequest(context));
    }
}