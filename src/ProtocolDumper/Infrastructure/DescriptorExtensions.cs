using Google.Protobuf.Reflection;

namespace ProtocolDumper.Infrastructure;

internal static class DescriptorExtensions
{
	public static string ToProtoFile(this FileDescriptor descriptor) 
		=> descriptor.ToProto().ToSourceWriter().ToString();

	public static SourceWriter ToSourceWriter(this FileDescriptorProto proto, SourceWriter? writer = null)
	{
		writer ??= new SourceWriter();

		if (proto.HasSyntax)
			writer.AppendLine($"""syntax = "{proto.Syntax}";""")
				.AppendLine();

		foreach (var dependency in proto.Dependency.array)
			if (!string.IsNullOrWhiteSpace(dependency))
				writer.AppendLine($"import {dependency};");

		if (proto.Dependency.Count > 0)
			writer.AppendLine(); // add a newline after dependencies if there are any

		if (proto.HasPackage)
			writer.AppendLine($"package {proto.Package};")
				.AppendLine();

		foreach (var message in proto.MessageType.array)
		{
			if (message is null) continue;

			message.WriteTo(writer);
			writer.AppendLine();
		}
		
		foreach (var enumType in proto.EnumType.array)
		{
			if (enumType is null) continue;

			enumType.WriteTo(writer);
			writer.AppendLine();
		}
		
		foreach (var service in proto.Service.array)
		{
			if (service is null) continue;

			service.WriteTo(writer);
			writer.AppendLine();
		}

		return writer;
	}

	public static SourceWriter WriteTo(this DescriptorProto message, SourceWriter writer)
	{
		writer.AppendLine($$"""message {{message.Name}} {""");
		writer.Indentation++;
		
		return writer.CloseBlock();
	}

	public static SourceWriter WriteTo(this EnumDescriptorProto enumType, SourceWriter writer)
	{
		writer.AppendLine($$"""enum {{enumType.Name}} {""");
		writer.Indentation++;

		return writer.CloseBlock();
	}

	public static SourceWriter WriteTo(this ServiceDescriptorProto service, SourceWriter writer)
	{
		writer.AppendLine($$"""service {{service.Name}} {""");
		writer.Indentation++;

		return writer.CloseBlock();
	}
}
