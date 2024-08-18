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

		foreach (var service in proto.Service.array.Where(static s => s is not null))
			service.WriteTo(writer);
		
		foreach (var enumType in proto.EnumType.array.Where(static e => e is not null)) 
			enumType.WriteTo(writer);

		foreach (var message in proto.MessageType.array.Where(static m => m is not null)) 
			message.WriteTo(writer);

		return writer;
	}

	public static SourceWriter WriteTo(this DescriptorProto message, SourceWriter writer)
	{
		writer.AppendLine($$"""message {{message.Name}} {""");
		writer.Indentation++;

		var indentBefore = writer.Indentation;
		for (var i = 0; i < message.Field.array.Count; i++)
		{
			var field = message.Field.array[i];
			if (field is null) continue;

			if (field.HasOneofIndex)
			{
				var isFirst = writer.Indentation == indentBefore;
				var hasNext = i < message.Field.array.Count - 1
					&& message.Field.array[i + 1] is not null
					&& message.Field.array[i + 1].HasOneofIndex
					&& message.Field.array[i + 1].OneofIndex == field.OneofIndex;

				if (isFirst && !hasNext) // treat it as a regular optional field
				{
					writer.AppendLine(
						"optional " +
						GetTypeNameFor(field) +
						field.Name + " = " + field.Number + ";"
					);

					continue;
				}

				if (isFirst) {
					writer.AppendLine($$"""oneof {{message.OneofDecl.array[field.OneofIndex].Name}} {""");
					writer.Indentation++;
				}

				writer.AppendLine(
					GetTypeNameFor(field) +
					field.Name + " = " + field.Number + ";"
				);

				if (!hasNext) writer.CloseBlock();

				continue;
			}

			writer.AppendLine(
				GetLabelFor(field) +
				GetTypeNameFor(field) +
				field.Name + " = " + field.Number + ";"
			);
		}

		if (message.NestedType.array.Length > 0)
			writer.AppendLine(); // add a newline after fields if there are any nested types

		for (var i = 0; i < message.NestedType.array.Count; i++)
		{
			var nestedMessage = message.NestedType.array[i];
			if (nestedMessage is null) continue;

			nestedMessage.WriteTo(writer);
			
			// add a newline after each nested message except the last one
			if (i < message.NestedType.array.Count - 1)
				writer.AppendLine(); 
		}

		if (message.EnumType.array.Length > 0)
			writer.AppendLine(); // add a newline after nested types if there are any enums

		for (var i = 0; i < message.EnumType.array.Count; i++)
		{
			var enumType = message.EnumType.array[i];
			if (enumType is null) continue;

			enumType.WriteTo(writer);

			// add a newline after each enum except the last one
			if (i < message.EnumType.array.Count - 1)
				writer.AppendLine();
		}

		return writer.AppendLine().CloseBlock();

		static string GetLabelFor(FieldDescriptorProto proto)
		{
			if (!proto.HasLabel) return string.Empty;
			var label = GetLabel(proto.Label);

			return string.IsNullOrWhiteSpace(label) ? label : label + " ";
		}

		static string GetLabel(FieldDescriptorProto.Types.Label label) => label switch {
			FieldDescriptorProto.Types.Label.Optional => "",
			FieldDescriptorProto.Types.Label.Required => "required",
			FieldDescriptorProto.Types.Label.Repeated => "repeated",
			_ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
		};

		static string GetTypeNameFor(FieldDescriptorProto proto) => proto.Type switch
		{
			FieldDescriptorProto.Types.Type.Message or FieldDescriptorProto.Types.Type.Enum
				=> proto.HasTypeName 
					? TrimTypeName(proto.TypeName) + " "
					: throw new InvalidOperationException("Missing type name for enum or message !"),

			_ => GetPrimitiveTypeName(proto.Type) + " "
		};

		static string TrimTypeName(string typeName)
		{
			var lastIndexOfDot = typeName.LastIndexOf('.');
			return lastIndexOfDot == -1 ? typeName : typeName[(lastIndexOfDot + 1)..];
		}

		static string GetPrimitiveTypeName(FieldDescriptorProto.Types.Type type) => type switch
		{
			FieldDescriptorProto.Types.Type.Double => "double",
			FieldDescriptorProto.Types.Type.Float => "float",
			FieldDescriptorProto.Types.Type.Int64 => "int64",
			FieldDescriptorProto.Types.Type.Uint64 => "uint64",
			FieldDescriptorProto.Types.Type.Int32 => "int32",
			FieldDescriptorProto.Types.Type.Fixed64 => "fixed64",
			FieldDescriptorProto.Types.Type.Fixed32 => "fixed32",
			FieldDescriptorProto.Types.Type.Bool => "bool",
			FieldDescriptorProto.Types.Type.String => "string",
			FieldDescriptorProto.Types.Type.Bytes => "bytes",
			FieldDescriptorProto.Types.Type.Uint32 => "uint32",
			FieldDescriptorProto.Types.Type.Sfixed32 => "sfixed32",
			FieldDescriptorProto.Types.Type.Sfixed64 => "sfixed64",
			FieldDescriptorProto.Types.Type.Sint32 => "sint32",
			FieldDescriptorProto.Types.Type.Sint64 => "sint64",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	public static SourceWriter WriteTo(this EnumDescriptorProto enumType, SourceWriter writer)
	{
		writer.AppendLine($$"""enum {{enumType.Name}} {""");
		writer.Indentation++;

		foreach (var value in enumType.Value.array)
		{
			if (value is null) continue;

			writer.AppendLine($"{value.Name} = {value.Number};");
		}

		return writer.AppendLine().CloseBlock();
	}

	public static SourceWriter WriteTo(this ServiceDescriptorProto service, SourceWriter writer)
	{
		writer.AppendLine($$"""service {{service.Name}} {""");
		writer.Indentation++;

		return writer.AppendLine().CloseBlock();
	}
}
