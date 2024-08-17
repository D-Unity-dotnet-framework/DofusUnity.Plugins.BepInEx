using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace ProtocolDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ProtocolDumperPlugin : BasePlugin
{
	internal new static ManualLogSource Log = null!;

	public override void Load()
	{
		Log = base.Log;
		Log.LogInfo($"Plugin '{MyPluginInfo.PLUGIN_NAME}' has successfully been loaded!");
	}
}
