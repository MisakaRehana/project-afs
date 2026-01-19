using System.Runtime.Loader;
using System.Text;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.ResourceManagement;

public sealed class AFSResManager : IDisposable
{
	// private readonly Assembly _asmResources;
	private readonly AssemblyLoadContext _asmResources;
	private bool isDisposed;
	
	public AFSResManager()
	{
		try
		{
			_asmResources = new AssemblyLoadContext("ProjectAFS.Resources", isCollectible: true); // enable unloading
			var asmPath = Path.Combine(AppContext.BaseDirectory, "ProjectAFS.Resources.dll");
			_asmResources.LoadFromAssemblyPath(asmPath);
			isDisposed = false;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Unable to initialize AFSResManager because ProjectAFS.Resources assembly is not found.", ex);
		}
	}

	public Stream OpenReader(string resourceName)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		string fullName = $"ProjectAFS.Resources.{resourceName.ReplaceAll(['/', '\\'], '.')}";
		var stream = _asmResources.Assemblies
			.Select(asm => asm.GetManifestResourceStream(fullName))
			.FirstOrDefault(s => s != null);
		if (stream == null)
		{
			throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly resources.");
		}
		return stream;
	}
	
	public async AFSTask<Stream> OpenReaderAsync(string resourceName)
	{
		await AFSTask.SwitchToThreadPool();
		return OpenReader(resourceName);
	}
	
	public IEnumerable<Stream> OpenMultipleReaders(IEnumerable<string> resourceNames)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		foreach (var resourceName in resourceNames)
		{
			yield return OpenReader(resourceName);
		}
	}
	
	public async IAsyncEnumerable<Stream> OpenMultipleReadersAsync(IEnumerable<string> resourceNames)
	{
		foreach (var resourceName in resourceNames)
		{
			yield return await OpenReaderAsync(resourceName);
		}
	}
	
	public TextReader OpenTextReader(string resourceName)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		var stream = OpenReader(resourceName);
		return new StreamReader(stream, Encoding.UTF8);
	}
	
	public async AFSTask<TextReader> OpenTextReaderAsync(string resourceName)
	{
		await AFSTask.SwitchToThreadPool();
		var stream = await OpenReaderAsync(resourceName);
		return new StreamReader(stream, Encoding.UTF8);
	}
	
	public IEnumerable<TextReader> OpenMultipleTextReaders(IEnumerable<string> resourceNames)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		foreach (var resourceName in resourceNames)
		{
			yield return OpenTextReader(resourceName);
		}
	}
	
	public async IAsyncEnumerable<TextReader> OpenMultipleTextReadersAsync(IEnumerable<string> resourceNames)
	{
		foreach (var resourceName in resourceNames)
		{
			yield return await OpenTextReaderAsync(resourceName);
		}
	}
	
	public TextReader OpenTextReader(string resourceName, Encoding encoding)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		var stream = OpenReader(resourceName);
		return new StreamReader(stream, encoding);
	}
	
	public IEnumerable<TextReader> OpenMultipleTextReaders(IEnumerable<string> resourceNames, Encoding encoding)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		foreach (var resourceName in resourceNames)
		{
			yield return OpenTextReader(resourceName, encoding);
		}
	}
	
	public async AFSTask<TextReader> OpenTextReaderAsync(string resourceName, Encoding encoding)
	{
		await AFSTask.SwitchToThreadPool();
		var stream = await OpenReaderAsync(resourceName);
		return new StreamReader(stream, encoding);
	}
	
	public async IAsyncEnumerable<TextReader> OpenMultipleTextReadersAsync(IEnumerable<string> resourceNames, Encoding encoding)
	{
		foreach (var resourceName in resourceNames)
		{
			yield return await OpenTextReaderAsync(resourceName, encoding);
		}
	}

	public void Dispose()
	{
		if (isDisposed) return;
		_asmResources.Unload();
		isDisposed = true;
	}
}