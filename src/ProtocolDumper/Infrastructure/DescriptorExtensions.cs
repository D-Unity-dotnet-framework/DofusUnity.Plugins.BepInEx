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

		foreach (var field in message.Field.array)
		{
			if (field is null) continue;

			writer.AppendLine(
				(field.HasLabel ? GetLabel(field.Label) + " " : "") + 
				(field.HasTypeName ? field.TypeName + " " : "")
				+ field.Name + " = " + field.Number + ";"
			);
		}

		return writer.CloseBlock();

		static string GetLabel(FieldDescriptorProto.Types.Label label) => label switch {
			FieldDescriptorProto.Types.Label.Optional => "optional",
			FieldDescriptorProto.Types.Label.Required => "required",
			FieldDescriptorProto.Types.Label.Repeated => "repeated",
			_ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
		};
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
