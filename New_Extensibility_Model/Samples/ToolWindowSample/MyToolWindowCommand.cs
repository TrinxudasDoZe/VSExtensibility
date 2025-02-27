﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ToolWindowSample;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

/// <summary>
/// A sample command for showing a tool window.
/// </summary>
[VisualStudioContribution]
public class MyToolWindowCommand : Command
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MyToolWindowCommand" /> class.
	/// </summary>
	/// <param name="extensibility">Extensibility object instance.</param>
	public MyToolWindowCommand(VisualStudioExtensibility extensibility)
		: base(extensibility)
	{
	}

	/// <inheritdoc />
	public override CommandConfiguration CommandConfiguration => new("%ToolWindowSample.MyToolWindowCommand.DisplayName%")
	{
		Placements = new[] { CommandPlacement.KnownPlacements.ToolsMenu },
		Icon = new(ImageMoniker.KnownValues.ToolWindow, IconSettings.IconAndText),
	};

	/// <inheritdoc />
	public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
	{
		await this.Extensibility.Shell().ShowToolWindowAsync<MyToolWindow>(activate: true, cancellationToken);
	}
}
