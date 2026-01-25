using Microsoft.Extensions.Hosting;
using ProjectAFS.Core.Utility.Threading;

namespace ProjectAFS.Core.Abstracts.Services.Storage;

public interface IStorageService : IHostedService
{
	AFSTask<IEnumerable<string>> SelectFolderAsync(string? initialPath = null, string? title = null, bool allowMultiSelect = false);
}