using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensions.MarkdownLinter;

namespace MarkdownLinter
{
	/// <summary>
	/// Entry class for markdown linter extension.
	/// </summary>
	internal class MarkdownLinterExtension : Extension
	{
		protected override void InitializeServices(IServiceCollection serviceCollection)
		{
			base.InitializeServices(serviceCollection);
			serviceCollection.AddScoped<MarkdownDiagnosticsService>();
		}
	}
}
