using Google.Protobuf.Reflection;
using Il2CppInterop.Generator.Extensions;

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

		Dictionary<int, List<FieldDescriptorProto>>? oneOfProperties = null; 
		foreach (var field in message.Field.array)
		{
			if (field is null) continue;
			
			if (field.HasOneofIndex)
			{
				oneOfProperties ??= [];
				oneOfProperties.GetOrCreate(field.OneofIndex, static _ => [])
					.Add(field);
				
				continue;
			}

			writer.AppendLine(
				GetLabelFor(field) + 
				GetTypeNameFor(field) + 
				field.Name + " = " + field.Number + ";"
			);
		}

		if (oneOfProperties is null) 
			return writer.CloseBlock();

		foreach (var (index, fields) in oneOfProperties)
		{
			if (fields.Count == 0) continue;


			if (fields.Count == 1)
			{
				var field = fields[0];

				writer.AppendLine(
					"optional " +
					GetTypeNameFor(field) +
					field.Name + " = " + field.Number + ";"
				);

				continue;
			}

			writer.AppendLine($$"""oneof {{message.OneofDecl.array[index].Name}} {""");
			writer.Indentation++;

			foreach (var field in fields)
			{
				writer.AppendLine(
					GetTypeNameFor(field) +
					field.Name + " = " + field.Number + ";"
				);
			}

			writer.CloseBlock();
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

		return writer.CloseBlock();

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
					? proto.TypeName 
					: throw new InvalidOperationException("Missing type name for enum or message !"),

			_ => GetPrimitiveTypeName(proto.Type) + " "
		};
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

		return writer.CloseBlock();
	}

	public static SourceWriter WriteTo(this ServiceDescriptorProto service, SourceWriter writer)
	{
		writer.AppendLine($$"""service {{service.Name}} {""");
		writer.Indentation++;

		return writer.CloseBlock();
	}
}
