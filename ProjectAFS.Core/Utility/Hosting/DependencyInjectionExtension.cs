using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProjectAFS.Core.Utility.Hosting;

public static class DependencyInjectionExtension
{
	extension(IHostBuilder builder)
	{
		public IHostBuilder AddSingleService(object service, ServiceLifetime lifetime)
		{
			return builder.ConfigureServices((ctx, s) =>
			{
				var descriptor = new ServiceDescriptor(service.GetType(), provider => service, lifetime);
				s.Add(descriptor);
			});
		}

		public IHostBuilder AddSingleService<TService>(TService service, ServiceLifetime lifetime) where TService : class
		{
			return builder.ConfigureServices((ctx, s) =>
			{
				var descriptor = new ServiceDescriptor(typeof(TService), provider => service, lifetime);
				s.Add(descriptor);
			});
		}

		public IHostBuilder AddSingleService<TService, TImplementation>(TImplementation service, ServiceLifetime lifetime)
			where TService : class
			where TImplementation : class, TService
		{
			return builder.ConfigureServices((ctx, s) =>
			{
				var descriptor = new ServiceDescriptor(typeof(TService), provider => service, lifetime);
				s.Add(descriptor);
			});
		}

		public IHostBuilder AddSingleton(object service)
		{
			return builder.AddSingleService(service, ServiceLifetime.Singleton);
		}

		public IHostBuilder AddSingleton<TService>(TService service) where TService : class
		{
			return builder.AddSingleService<TService>(service, ServiceLifetime.Singleton);
		}

		public IHostBuilder AddSingleton<TService, TImplementation>(TImplementation service)
			where TService : class
			where TImplementation : class, TService
		{
			return builder.AddSingleService<TService, TImplementation>(service, ServiceLifetime.Singleton);
		}

		public IHostBuilder AddScoped(object service)
		{
			return builder.AddSingleService(service, ServiceLifetime.Scoped);
		}

		public IHostBuilder AddScoped<TService>(TService service) where TService : class
		{
			return builder.AddSingleService<TService>(service, ServiceLifetime.Scoped);
		}

		public IHostBuilder AddScoped<TService, TImplementation>(TImplementation service)
			where TService : class
			where TImplementation : class, TService
		{
			return builder.AddSingleService<TService, TImplementation>(service, ServiceLifetime.Scoped);
		}

		public IHostBuilder AddTransient(object service)
		{
			return builder.AddSingleService(service, ServiceLifetime.Transient);
		}

		public IHostBuilder AddTransient<TService>(TService service) where TService : class
		{
			return builder.AddSingleService<TService>(service, ServiceLifetime.Transient);
		}

		public IHostBuilder AddTransient<TService, TImplementation>(TImplementation service)
			where TService : class
			where TImplementation : class, TService
		{
			return builder.AddSingleService<TService, TImplementation>(service, ServiceLifetime.Transient);
		}
	}
}