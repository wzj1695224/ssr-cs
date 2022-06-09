using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.SystemX.Diagnostics
{
	internal static class StackTraceExtension
	{
		public static string GetFramesString(this StackTrace self)
		{
			var stacks = self.GetFrames();
			if (stacks == null) return "";

			var sb = new StringBuilder();
			foreach (var stack in stacks)
				sb.Append('\t').Append(stack.GetMethod()).AppendLine();
			return sb.ToString();
		}

	}
}
