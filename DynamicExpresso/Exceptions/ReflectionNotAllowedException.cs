using System;
using System.Runtime.Serialization;

namespace DynamicExpresso
{
	public class ReflectionNotAllowedException : ParseException
	{
		public ReflectionNotAllowedException()
			: base("Reflection expression not allowed. To enable reflection use Interpreter.EnableReflection().", 0) 
		{
		}
	}
}
