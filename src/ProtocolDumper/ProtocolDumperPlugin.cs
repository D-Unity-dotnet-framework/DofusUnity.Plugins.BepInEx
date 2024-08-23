using BepInEx;
using UnityEngine;
using BepInEx.Logging;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using Google.Protobuf.Reflection;
using ProtocolDumper.Infrastructure;
using System.Net.Sockets;

namespace ProtocolDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ProtocolDumperPlugin : BasePlugin
{
	static readonly string ProtocolDumpPath = Path.Combine(Paths.GameRootPath, $"protocol-{DateTime.Now:yyyyMMddTHHmmss}");
	static List<Assembly>? _loadedAssemblies;

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

			var assembly = loadedAssemblies.FirstOrDefault(
				assembly => assembly.GetName().Name?.Contains(assemblyName) ?? false);

			Log.LogDebug($"Assembly '{assemblyName}' is {(assembly != null ? "already" : "not yet")} loaded.");

			_loadedAssemblies ??= new(gameAssemblies.Length);
			_loadedAssemblies.Add(assembly ?? Assembly.LoadFrom(protocolAssemblyPath));
		}

		// Now we can dump the protocol files, let's create a new GameObject to do that
		AddComponent<DumpProtocolBehavior>();
	}

	public override bool Unload()
	{
		Log.LogInfo($"Plugin '{MyPluginInfo.PLUGIN_NAME}' has successfully been unloaded!");
		_loadedAssemblies?.Clear();
		_loadedAssemblies = null;

		return base.Unload();
	}

	class DumpProtocolBehavior : MonoBehaviour
	{
		readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DumpProtocolBehavior));

		void Start()
		{
			if (_loadedAssemblies == null) {
				logger.LogFatal("No protocol assemblies could be loaded, aborting...");
				Destroy(this);
				return;
			}

			try
			{
				foreach (var assembly in _loadedAssemblies.Where(ReflectionExtensions.IsProtocolAssembly))
				{
					var dumpPath = Path.Combine(ProtocolDumpPath, assembly.GetName().Name!.GetLastSegment().ToLowerInvariant());
					DumpProtocolFor(assembly, Directory.CreateDirectory(dumpPath));
				}
			}
			finally { Destroy(this); } // we only need to run this once
		}

		void DumpProtocolFor(Assembly assembly, DirectoryInfo outputDirectory)
		{
			foreach (var type in assembly.GetTypes().Where(static t => t.Name.EndsWith("Reflection")))
			{
				if (!type.TryGetFileDescriptor(out var descriptor))
					continue;

				logger.LogDebug($"Dumping protocol for '{descriptor.Name}'...");
				var protoFile = descriptor.ToProtoFile();

				var filePath = Path.Combine(outputDirectory.FullName, descriptor.Name);
				Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
				File.WriteAllText(filePath, protoFile);

				logger.LogDebug($"Successfully dumped protocol at '{filePath}'");
			}

			logger.LogInfo($"Protocol files have successfully been dumped at '{ProtocolDumpPath}' ! ");
		}

		void OnDestroy()
		{
			try { logger.LogDebug("DumpProtocolBehavior.OnDestroy()"); }
			finally { logger.Dispose(); } // gotta clean up
		}
	}
}