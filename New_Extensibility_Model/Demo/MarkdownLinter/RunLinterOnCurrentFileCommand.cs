using System.Diagnostics;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Definitions;
using Microsoft.VisualStudio.Extensions.MarkdownLinter;

namespace MarkdownLinter
{
	[CommandIcon(KnownMonikers.Extension, IconSettings.IconAndText)]
	[Command(
		"MarkdownExtension.RunLinterOnCurrentFile",
		"Run markdown linter on current file",
		placement: CommandPlacement.ToolsMenu)]
	[CommandEnabledWhen(
		"FileSelected",
		new string[] { "FileSelected"},
		new string[] { "ClientContext:Shell.ActiveSelectionFileName=.md$" })]
	internal class RunLinterOnCurrentFileCommand : Command
	{
		private TraceSource traceSource;
		private readonly MarkdownDiagnosticsService markdownDiagnosticsService;

		public RunLinterOnCurrentFileCommand(
			VisualStudioExtensibility extensibility,
			TraceSource traceSource,
			MarkdownDiagnosticsService markdownDiagnosticsService,
			string id)
			: base(extensibility, id)
		{
			this.traceSource = Requires.NotNull(traceSource, nameof(traceSource));
			this.markdownDiagnosticsService = Requires.NotNull(markdownDiagnosticsService, nameof(markdownDiagnosticsService));
		}

		public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				// Get the selected item URIs from IDE context that reprents the state when command was executed.
				var selectedItemPaths = new Uri[] { await context.GetSelectedPathAsync(cancellationToken) };

				// Enumerate through each selection and run linter on each selected item.
				foreach (var selectedItem in selectedItemPaths.Where(p => p.IsFile))
				{
					await this.markdownDiagnosticsService.ProcessFileAsync(selectedItem, cancellationToken);
				}
			}
			catch (InvalidOperationException ex)
			{
				this.traceSource.TraceEvent(TraceEventType.Error, 0, ex.ToString());
			}
		}
	}
}
