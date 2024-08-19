using System.Reflection;

namespace ProtocolDumper.Infrastructure;

internal static class ReflectionExtensions
{
	public static bool IsProtocolAssembly(this Assembly assembly)
		=> assembly.FullName?.Contains("Dofus.Protocol") ?? false;
}