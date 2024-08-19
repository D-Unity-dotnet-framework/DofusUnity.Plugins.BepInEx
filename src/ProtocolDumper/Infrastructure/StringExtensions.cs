using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolDumper.Infrastructure;
internal static class StringExtensions
{
	public static string GetLastSegment(this string typeName, char separator = '.')
	{
		var lastIndexOf = typeName.LastIndexOf(separator);
		return lastIndexOf == -1 ? typeName : typeName[(lastIndexOf + 1)..];
	}
}
