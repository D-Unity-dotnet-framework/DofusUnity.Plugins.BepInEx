using BepInEx;
using UnityEngine;
using BepInEx.Logging;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using Google.Protobuf.Reflection;
using ProtocolDumper.Infrastructure;

namespace ProtocolDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ProtocolDumperPlugin : BasePlugin
{
	static readonly string ProtocolDumpPath = Path.Combine(Paths.GameRootPath, $"protocol-{DateTime.Now:yyyyMMddTHHmmss}");
	
	public override void Load()
	{
		Log.LogInfo($"Plugin '{MyPluginInfo.PLUGIN_NAME}' has successfully been loaded!");

		// First we need to make sure that we have all the protocol assemblies loaded
		var gameAssembliesPath = Path.Combine(Paths.BepInExRootPath, "interop");
		var gameAssemblies = Directory.GetFiles(gameAssembliesPath, "*.dll");

		var protocolAssembliesPaths = gameAssemblies
			.Where(static p => p.Contains("Protocol"))
			.ToArray();

		// Compare the protocol assemblies against the loaded assemblies
		var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (var protocolAssemblyPath in protocolAssembliesPaths)
		{
			var assemblyName = Path.GetFileNameWithoutExtension(protocolAssemblyPath);
			Log.LogDebug($"Checking if assembly '{assemblyName}' is loaded...");

			var hasAssembly = loadedAssemblies.Any(assembly => assembly.GetName().Name?.Contains(assemblyName) ?? false);
			Log.LogDebug($"Assembly '{assemblyName}' is {(hasAssembly ? "already" : "not yet")} loaded.");

			if (hasAssembly)
				continue;

			Log.LogDebug($"Loading assembly '{protocolAssemblyPath}'...");
			Assembly.LoadFrom(protocolAssemblyPath);
		}

		// Now we can dump the protocol files, let's create a new GameObject to do that
		AddComponent<DumpProtocolBehavior>();
	}

	class DumpProtocolBehavior : MonoBehaviour
	{
		readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DumpProtocolBehavior));

		void Start()
		{
			try
			{
				var protocolTypes = AppDomain.CurrentDomain.GetAssemblies()
					.Where(ReflectionExtensions.IsProtocolAssembly)
					.SelectMany(assembly => assembly.GetTypes());

				foreach (var type in protocolTypes)
				{
					if (!type.Name.EndsWith("Reflection"))
						continue;

					if (type.GetProperty("Descriptor") is not { } descriptorProperty) 
						continue;

					if (descriptorProperty.PropertyType != typeof(FileDescriptor)) 
						continue;

					if (descriptorProperty.GetValue(null) is not FileDescriptor descriptor) 
						continue;

					logger.LogDebug($"Dumping protocol for '{descriptor.Name}'...");

					var protoFile = descriptor.ToProtoFile();
					var fileName = descriptor.Name.StartsWith("message")
						? "message." + type.Assembly.GetName().Name!.GetLastSegment().ToLowerInvariant() + ".proto" 
						: descriptor.Name;

					var filePath = Path.Combine(ProtocolDumpPath, fileName);
					Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
					File.WriteAllText(filePath, protoFile);

					logger.LogDebug($"Successfully dumped protocol at '{filePath}'");
				}

				logger.LogInfo($"Protocol files have successfully been dumped at '{ProtocolDumpPath}' ! ");
			}
			finally { Destroy(this); } // we only need to run this once
		}

		void OnDestroy()
		{
			try { logger.LogDebug("DumpProtocolBehavior.OnDestroy()"); }
			finally { logger.Dispose(); } // gotta clean up
		}
	}
}