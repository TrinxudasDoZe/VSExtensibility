---
title: Debugger visualizers reference
description: A reference for VisualStudio.Extensibility debugger visualizers
date: 2023-2-7
---

# Debugger visualizers

Debugger visualizers are a Visual Studio feature that allows providing a custom visualization for a variables or objects of a specific .NET type during a debug session.

Debugger visualizers are accessible from the *Watch* window:

![Debugger visualizers in the watch window](watchWindow.png "Debugger visualizers in the watch window")

## Getting started

Follow the [Create the extension project](./../../getting-started/create-your-first-extension.md) section in the Getting Started section.

Then add a class extending `DebuggerVisualizerProvider` and apply the `VisualStudioContribution` attribute to it:

```csharp
/// <summary>
/// Debugger visualizer provider class for <see cref="System.String"/>.
/// </summary>
[VisualStudioContribution]
internal class StringDebuggerVisualizerProvider : DebuggerVisualizerProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringDebuggerVisualizerProvider"/> class.
    /// </summary>
    /// <param name="extension">Extension instance.</param>
    /// <param name="extensibility">Extensibility object.</param>
    public RegexMatchDebuggerVisualizerProvider(RegexMatchVisualizerExtension extension, VisualStudioExtensibility extensibility)
        : base(extension, extensibility)
    {
    }

    /// <inheritdoc/>
    public override DebuggerVisualizerProviderConfiguration DebuggerVisualizerProviderConfiguration => new("My string visualizer", typeof(string));

    /// <inheritdoc/>
    public override async Task<IRemoteUserControl> GetVisualizerAsync(VisualizerTarget visualizerTarget, CancellationToken cancellationToken)
    {
        string targetObjectValue = await visualizerTarget.ObjectSource.RequestDataAsync<string>(jsonSerializer: null, cancellationToken);

        return new MyStringVisualizerControl(targetObjectValue);
    }
}
```

The sample above defines a new debugger visualizer which applies to objects of type `string`:

- The `DebuggerVisualizerProviderConfiguration` property defines the visualizer display name and the supported .NET type.
- The `GetVisualizerAsync` is invoked by Visual Studio when the user requests the display of the debugger visualizer for a certain value. `GetVisualizerAsync` uses the `VisualizerTarget` object to retrieve the value to be visualized and passes it to a custom remote user control (reference the [Remote UI](./../../inside-the-sdk/remote-ui.md) documentation). The remote user control is then returned and will be shown in a popup window in Visual Studio.

## The object source

The *object source* is a .NET class that is loaded by the debugger in the process being debugged. The debugger visualizer can retrieve data from the *object source* using methods exposed by `VisualizerTarget.ObjectSource`.

The default *object source* allows debugger visualizers to retrieve the value of the object to be visualized by calling the [`RequestDataAsync<T>(JsonSerializer?, CancellationToken)`](../../api/Microsoft.VisualStudio.Extensibility.md#M-Microsoft-VisualStudio-Extensibility-DebuggerVisualizers-VisualizerObjectSourceClient-RequestDataAsync``1-Newtonsoft-Json-JsonSerializer,System-Threading-CancellationToken-) method. The default *object source* uses [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) to serialize the value, and the VisualStudio.Extensibility libraries also use Newtonsoft.Json for the deserialization. Alternatively you can use [`RequestDataAsync(CancellationToken)`](../../api/Microsoft.VisualStudio.Extensibility.md#M-Microsoft-VisualStudio-Extensibility-DebuggerVisualizers-VisualizerObjectSourceClient-RequestDataAsync-System-Threading-CancellationToken-) to retrieve the serialized value as a `JToken`.

If you want to visualize a .NET type that is natively supported by Newtonsoft.Json, or you want to visualize your own type and you can make it serializable, the instructions above are sufficient to create a simple debugger visualizer. Read on if you want to support more complex types or to leverage more advanced features.

## Using a custom object source

If the type to be visualized cannot be automatically serialized by Newtonsoft.Json, you can create a custom *object source* to handle the serialization.

- Create a new .NET class library project targeting `netstandard2.0`. You can target a more specific version of .NET Framework or .NET (E.g, `net472` or `net6.0`) if necessary to serialize the object to be visualized.
- Add a package reference to [Microsoft.VisualStudio.DebuggerVisualizers](https://www.nuget.org/packages/Microsoft.VisualStudio.DebuggerVisualizers).
- Add a class extending `VisualizerObjectSource` and override `GetData` writing the serialized value of `target` to the `outgoingData` stream.

```csharp
public class MyObjectSource : VisualizerObjectSource
{
    /// <inheritdoc/>
    public override void GetData(object target, Stream outgoingData)
    {
        MySerializableType result = Convert(match);
        SerializeAsJson(outgoingData, result);
    }

    private static MySerializableType Convert(object target)
    {
        // Add your code here to convert target into a type serializable by Newtonsoft.Json
        ...
    }
}
```

### Custom serialization

You can use the `VisualizerObjectSource.SerializeAsJson` method to serialize an object using Newtonsoft.Json to a `Stream` without adding a reference to Newtonsoft.Json to your library.

If you need to reference [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json), you should use the same version that is referenced by the `Microsoft.VisualStudio.Extensibility.Sdk` package, but it's preferrable to use `DataContract` and `DataMember` attributes to support object serialization instead of relying on Newtonsoft.Json types.

Alternatively, you can implement your own custom serialization (E.g., binary serialization) writing directly to `outgoingData`.

### Adding the object source DLL to the extension

Modify the extension *.csproj* file adding a `ProjectReference` to the *object source* library project which make sure that the *object source* library is built before the extension is packaged.

Also add a `Content` item including the *object source* library DLL into the `netstandard2.0` subfolder of the extension.

```xml
  <ItemGroup>
    <Content Include="pathToTheObjectSourceDllBinPath\$(Configuration)\netstandard2.0\MyObjectSourceLibrary.dll" Link="netstandard2.0\MyObjectSourceLibrary.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyObjectSourceLibrary\MyObjectSourceLibrary.csproj" />
  </ItemGroup>
```

Alternatively you can use the `net4.6.2` or `netcoreapp` subfolders if you built the *object source* library targeting .NET Framework or .NET. You can even include all three subfolders with different versions of the *object source* library, but it's generally preferrable to simply target `netstandard2.0` only.

You should try to minimize the number of dependencies of the *object source* library DLL. If your *object source* library has dependencies other than [Microsoft.VisualStudio.DebuggerVisualizers](https://www.nuget.org/packages/Microsoft.VisualStudio.DebuggerVisualizers) and libraries already guaranteed to be loaded in the process being debugged, make sure to also include those DLL files into the same subfolder as the *object source* library DLL.

### Updating the debugger visualizer provider to use the custom object source

You can then update your `DebuggerVisualizerProviderConfiguration` configuration to reference your custom *object source*:

```csharp
    public override DebuggerVisualizerProviderConfiguration DebuggerVisualizerProviderConfiguration => new("My visualizer", typeof(TypeToVisualize))
    {
        VisualizerObjectSourceType = new(typeof(MyObjectSource)),
    };

    public override async Task<IRemoteUserControl> GetVisualizerAsync(VisualizerTarget visualizerTarget, CancellationToken cancellationToken)
    {
        MySerializableType result = await visualizerTarget.ObjectSource.RequestDataAsync<MySerializableType>(jsonSerializer: null, cancellationToken);
        return new MyVisualizerUserControl(result);
    }
```

## Complex object source interactions

If the retrieval of data from the *object source* cannot be done with a single parameterless call to `RequestDataAsync`, you can instead perform a more complex message exchange with the *object source* by invoking [RequestDataAsync<TMessage, TResponse>(TMessage, JsonSerializer?, CancellationToken)](../../api/Microsoft.VisualStudio.Extensibility.md#M-Microsoft-VisualStudio-Extensibility-DebuggerVisualizers-VisualizerObjectSourceClient-RequestDataAsync``2-``0,Newtonsoft-Json-JsonSerializer,System-Threading-CancellationToken-) multiple times and sending different *messages* to the *object source*. Both the message and response will be serialized by the VisualStudio.Extensibility infrastructure using Newtonsoft.Json. Other overrides of `RequestDataAsync` allow you to use `JToken` objects or implement custom serialization and deserialization.

You can implement any custom protocol using different messages to retrieve information from the *object source*. The most common use case for this feature is breaking the retrieval of a potentially large object into multiple calls to avoid `RequestDataAsync` timing out.

This is an example of how you can retrieve the content of a potentially large collection one item at a time:

```csharp
for (int i = 0; ; i++)
{
    MySerializableType? collectionEntry = await visualizerTarget.ObjectSource.RequestDataAsync<int, MySerializableType?>(i, jsonSerializer: null, cancellationToken);
    if (collectionEntry is null)
    {
        break;
    }

    observableCollection.Add(collectionEntry);
}
```

The code above uses a simple index as message for the `RequestDataAsync` calls. The corresponding *object source* code would override the `TransferData` method (instead of `GetData`):

```csharp
public class MyCollectionTypeObjectSource : VisualizerObjectSource
{
    public override void TransferData(object target, Stream incomingData, Stream outgoingData)
    {
        var index = (int)DeserializeFromJson(incomingData, typeof(int))!;

        if (target is MyCollectionType collection && index < collection.Count)
        {
            var result = Convert(collection[index]);
            SerializeAsJson(outgoingData, result);
        }
        else
        {
            SerializeAsJson(outgoingData, null);
        }
    }

    private static MySerializableType Convert(object target)
    {
        // Add your code here to convert target into a type serializable by Newtonsoft.Json
        ...
    }
}
```

The *object source* above leverages the `VisualizerObjectSource.DeserializeFromJson` method to deserialize the message sent by the visualizer provider from `incomingData`.

When implementing a debugger visualizer provider that performs complex message interaction with the *object source*, it's usually better to pass the `VisualizerTarget` to the visualizer's `RemoteUserControl` so that the message exchange can happen asynchronously while the control is loaded. This also allow to send messages to the *object source* to retrieve data based on the user's interactions with the visualizer's UI.

```csharp
public override Task<IRemoteUserControl> GetVisualizerAsync(VisualizerTarget isualizerTarget, CancellationToken cancellationToken)
{
    return Task.FromResult<IRemoteUserControl>(new MyVisualizerUserControl(visualizerTarget));
}
```

```csharp
internal class MyVisualizerUserControl : RemoteUserControl
{
    private readonly VisualizerTarget visualizerTarget;

    public MyVisualizerUserControl(VisualizerTarget visualizerTarget)
        : base(new MyDataContext())
    {
        this.visualizerTarget = visualizerTarget;
    }

    public override async Task ControlLoadedAsync(CancellationToken cancellationToken)
    {
        // Start querying the VisualizerTarget here
        ...
    }
    ...
```

## Replacing the visualized object value

If `VisualizerTarget.IsTargetReplaceable` is true, the debugger visualizer can use the [ReplaceTargetObjectAsync](../../api/Microsoft.VisualStudio.Extensibility.md#M-Microsoft-VisualStudio-Extensibility-DebuggerVisualizers-VisualizerObjectSourceClient-ReplaceTargetObjectAsync``1-``0,Newtonsoft-Json-JsonSerializer,System-Threading-CancellationToken-) method to update the value of the visualized object in the process being debugged.

The *object source* must override the `CreateReplacementObject` method:

```csharp
public override object CreateReplacementObject(object target, Stream incomingData)
{
    // Use DeserializeFromJson to read from incomindData
    // the new value of the object being visualized
    ...
    return newValue;
}
```
