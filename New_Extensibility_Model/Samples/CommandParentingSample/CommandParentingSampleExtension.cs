﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CommandParentingSample;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

/// <summary>
/// Extension entry point for the CommandParentingSample.
/// </summary>
[VisualStudioContribution]
public class CommandParentingSampleExtension : Extension
{
	/// <inheritdoc/>
	protected override void InitializeServices(IServiceCollection serviceCollection)
	{
		base.InitializeServices(serviceCollection);
	}
}
