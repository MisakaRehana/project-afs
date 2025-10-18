using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectAFS.Core.Utility.Services;

/// <summary>
/// Represents the default implementation for one or more abstraction services.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class DefaultImplementationAttribute : Attribute
{
	/// <summary>
	/// The primary abstraction type that this implementation serves.
	/// </summary>
	public Type PrimaryAbstractionType { get; }
	
	/// <summary>
	/// Additional abstraction types that this implementation serves.<br />
	/// All additional abstraction type instances will be registered as the same instance as the primary abstraction type.
	/// </summary>
	public Type[] AdditionalAbstractionTypes { get; }
	
	/// <summary>
	/// The service lifetime for the implementation.<br />
	/// All registered abstraction types will share the same lifetime.
	/// </summary>
	public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

	/// <summary>
	/// Instantiates a new instance of <see cref="DefaultImplementationAttribute"/> as the default <see cref="ServiceLifetime.Singleton"/> implementation for the specified abstraction type(s).
	/// </summary>
	/// <param name="absType">The primary abstraction type.</param>
	/// <param name="extAbsTypes">Additional abstraction types.</param>
	public DefaultImplementationAttribute(Type absType, params Type[] extAbsTypes)
	{
		PrimaryAbstractionType = absType;
		AdditionalAbstractionTypes = extAbsTypes;
	}
	
	/// <summary>
	/// Instantiates a new instance of <see cref="DefaultImplementationAttribute"/> as the specified service lifetime implementation for the specified abstraction type(s).
	/// </summary>
	/// <param name="lifetime">The service lifetime.</param>
	/// <param name="absType">The primary abstraction type.</param>
	/// <param name="extAbsTypes">Additional abstraction types.</param>
	public DefaultImplementationAttribute(ServiceLifetime lifetime, Type absType, params Type[] extAbsTypes)
		: this(absType, extAbsTypes)
	{
		Lifetime = lifetime;
	}
	
	/// <summary>
	/// Gets all abstraction types that this implementation serves.
	/// </summary>
	/// <returns>>An enumerable of all abstraction types.</returns>
	public IEnumerable<Type> GetAllAbstractionTypes()
	{
		yield return PrimaryAbstractionType;
		foreach (var extType in AdditionalAbstractionTypes)
		{
			yield return extType;
		}
	}
}

public static class DefaultImplExtensions
{
	public static IServiceCollection AddDefaultImplementation<TAbs>(this IServiceCollection services) where TAbs : class
	{
		if (!typeof(TAbs).IsInterface) throw new ArgumentException($"{typeof(TAbs).FullName} is not an interface type.");

		var implMap = FindDefaultImplForAbstraction<TAbs>();
		
		if (implMap.ImplType == null)
		{
			throw new InvalidOperationException($"No default implementation found for abstraction type {typeof(TAbs).FullName}.");
		}
		CheckImplValidity<TAbs>(implMap);

		switch (implMap.Lifetime)
		{
			case ServiceLifetime.Singleton:
				services.AddSingleton(typeof(TAbs), sp => ActivatorUtilities.CreateInstance(sp, implMap.ImplType));
				foreach (var extType in implMap.ExtTypes)
				{
					services.AddSingleton(extType, provider => provider.GetRequiredService<TAbs>());
				}
				break;
			case ServiceLifetime.Scoped:
				services.AddScoped(typeof(TAbs), sp => ActivatorUtilities.CreateInstance(sp, implMap.ImplType));
				foreach (var extType in implMap.ExtTypes)
				{
					services.AddScoped(extType, provider => provider.GetRequiredService<TAbs>());
				}
				break;
			case ServiceLifetime.Transient:
				services.AddTransient(typeof(TAbs), sp => ActivatorUtilities.CreateInstance(sp, implMap.ImplType));
				foreach (var extType in implMap.ExtTypes)
				{
					services.AddTransient(extType, provider => provider.GetRequiredService<TAbs>());
				}
				break;
			default:
				throw new ArgumentOutOfRangeException($"Invalid service lifetime {implMap.Lifetime} for implementation type {implMap.ImplType.FullName}.");
		}

		return services;
	}
	
	private static void CheckImplValidity<TAbs>((Type? ImplType, List<Type> ExtTypes, ServiceLifetime Lifetime) implMap) where TAbs : class
	{
		if (!implMap.ImplType!.IsClass || implMap.ImplType.IsAbstract)
		{
			throw new InvalidOperationException($"The default implementation type {implMap.ImplType.FullName} for abstraction type {typeof(TAbs).FullName} is not a concrete class.");
		}
		
		if (!implMap.ImplType.IsAssignableTo(typeof(TAbs)))
		{
			throw new InvalidOperationException($"The default implementation type {implMap.ImplType.FullName} does not implement the abstraction type {typeof(TAbs).FullName}.");
		}
		
		if (implMap.ExtTypes.Any(t => !implMap.ImplType.IsAssignableTo(t)))
		{
			var invalidTypes = implMap.ExtTypes.Where(t => !implMap.ImplType.IsAssignableTo(t)).ToList();
			throw new InvalidOperationException($"The default implementation type {implMap.ImplType.FullName} does not implement the following additional abstraction types: {string.Join(", ", invalidTypes.Select(t => t.FullName))}.");
		}
	}

	private static (Type? ImplType, List<Type> ExtTypes, ServiceLifetime Lifetime) FindDefaultImplForAbstraction<TAbs>() where TAbs : class
	{
		var allMappings = BuildImplMappings();
		var absType = typeof(TAbs);

		foreach (var (implType, (lifetime, primAbsType, extAbsTypes)) in allMappings)
		{
			if (primAbsType == absType || extAbsTypes.Contains(absType))
			{
				return (implType, extAbsTypes.Where(t => t != absType).ToList(), lifetime);
			}
		}

		return (null, [], ServiceLifetime.Singleton); // default as singleton
	}

	private static Dictionary<Type, (ServiceLifetime, Type, List<Type>)> BuildImplMappings()
	{
		var result = new Dictionary<Type, (ServiceLifetime, Type, List<Type>)>();
		var allTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(asm => asm.GetTypes())
			.Where(type => type is {IsClass: true, IsAbstract: false});
		foreach (var implType in allTypes)
		{
			var attr = implType.GetCustomAttribute<DefaultImplementationAttribute>();
			if (attr == null) continue;
			var allAbsTypes = new HashSet<Type>(attr.GetAllAbstractionTypes());
			if (allAbsTypes.Count > 0)
			{
				result[implType] = (attr.Lifetime, attr.PrimaryAbstractionType, allAbsTypes.ToList());
			}
		}

		return result;
	}
}