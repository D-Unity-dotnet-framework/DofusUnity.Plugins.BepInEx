using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Google.Protobuf.Reflection;

namespace ProtocolDumper.Infrastructure;

internal static class ReflectionExtensions
{
	public static bool IsProtocolAssembly(this Assembly assembly)
		=> assembly.FullName?.Contains("Dofus.Protocol") ?? false;

	public static bool TryGetFileDescriptor(this Type type, [NotNullWhen(true)] out FileDescriptor? fileDescriptor)
	{
		fileDescriptor = null;

		if (type.GetProperty("Descriptor") is not { } descriptorProperty)
			return false;

		if (descriptorProperty.PropertyType != typeof(FileDescriptor))
			return false;

		if (descriptorProperty.GetValue(null) is not FileDescriptor descriptor)
			return false;

		fileDescriptor = descriptor;
		return true;
	}
}