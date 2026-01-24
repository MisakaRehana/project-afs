// ReSharper disable UnusedType.Global
using System;
using System.Collections.Generic;
using ProjectAFS.Core.Abstracts.Projects;
using ProjectAFS.Core.Abstracts.Services.Extensibility;
using ProjectAFS.Template.StandardFanmadeProject.Data;

namespace ProjectAFS.Template.StandardFanmadeProject
{
	public sealed class TemplatePlugin : IPlugin, IProjectTemplateProvider
	{
		private bool isDisposed;
		
		public IEnumerable<IProjectTemplate> GetAvailableProjectTemplates()
		{
			ObjectDisposedException.ThrowIf(isDisposed, this);
			return
			[
				new StandardFanmadeProjectTemplate()
			];
		}

		public void Dispose()
		{
			Dispose(true);
		}
		
		private void Dispose(bool disposing)
		{
			if (isDisposed) return;
			if (disposing)
			{
				// Dispose managed resources here if any
			}
				
			// Dispose unmanaged resources here if any
				
			isDisposed = true;
		}
	}
}