using System.Runtime.Loader;
using System.Text;
using ProjectAFS.Core.Abstracts.Services.ResourceManagement;
using ProjectAFS.Core.Utility.Strings;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Services.ResourceManagement;

public sealed class AFSResManager : IAFSResManager
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
	
	public bool TryOpenReader(string resourceName, out Stream? stream)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		string fullName = $"ProjectAFS.Resources.{resourceName.ReplaceAll(['/', '\\'], '.')}";
		stream = _asmResources.Assemblies
			.Select(asm => asm.GetManifestResourceStream(fullName))
			.FirstOrDefault(s => s != null);
		return stream != null;
	}
	
	public async AFSTask<Stream> OpenReaderAsync(string resourceName)
	{
		await AFSTask.SwitchToThreadPool();
		return OpenReader(resourceName);
	}
	
	public TextReader OpenTextReader(string resourceName)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		var stream = OpenReader(resourceName);
		return new StreamReader(stream, Encoding.UTF8);
	}
	
	public TextReader OpenTextReader(string resourceName, Encoding encoding)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		var stream = OpenReader(resourceName);
		return new StreamReader(stream, encoding);
	}
	
	public bool TryOpenTextReader(string resourceName, out TextReader? reader)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		if (TryOpenReader(resourceName, out var stream))
		{
			reader = new StreamReader(stream!, Encoding.UTF8);
			return true;
		}
		else
		{
			reader = null;
			return false;
		}
	}
	
	public bool TryOpenTextReader(string resourceName, Encoding encoding, out TextReader? reader)
	{
		ObjectDisposedException.ThrowIf(isDisposed, this);
		if (TryOpenReader(resourceName, out var stream))
		{
			reader = new StreamReader(stream!, encoding);
			return true;
		}
		else
		{
			reader = null;
			return false;
		}
	}
	
	public async AFSTask<TextReader> OpenTextReaderAsync(string resourceName)
	{
		await AFSTask.SwitchToThreadPool();
		var stream = await OpenReaderAsync(resourceName);
		return new StreamReader(stream, Encoding.UTF8);
	}
	
	public async AFSTask<TextReader> OpenTextReaderAsync(string resourceName, Encoding encoding)
	{
		await AFSTask.SwitchToThreadPool();
		var stream = await OpenReaderAsync(resourceName);
		return new StreamReader(stream, encoding);
	}

	public void Dispose()
	{
		if (isDisposed) return;
		_asmResources.Unload();
		isDisposed = true;
	}
}