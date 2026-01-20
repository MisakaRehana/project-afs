using System.Text;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Abstracts.Services.ResourceManagement;

public interface IAFSResManager : IDisposable
{
	Stream OpenReader(string resourceName);
	bool TryOpenReader(string resourceName, out Stream? stream);
	AFSTask<Stream> OpenReaderAsync(string resourceName);
	TextReader OpenTextReader(string resourceName);
	TextReader OpenTextReader(string resourceName, Encoding encoding);
	bool TryOpenTextReader(string resourceName, out TextReader? reader);
	bool TryOpenTextReader(string resourceName, Encoding encoding, out TextReader? reader);
	AFSTask<TextReader> OpenTextReaderAsync(string resourceName);
	AFSTask<TextReader> OpenTextReaderAsync(string resourceName, Encoding encoding);
}