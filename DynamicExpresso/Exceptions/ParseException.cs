using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Runtime.Serialization;

namespace DynamicExpresso
{
	public class ParseException : DynamicExpressoException
	{
		const string PARSE_EXCEPTION_FORMAT = "{0} (at index {1}).";

		public ParseException(string message, int position)
			: base(string.Format(PARSE_EXCEPTION_FORMAT, message, position)) 
		{
			Position = position;
		}

		public int Position { get; private set; }

	}
}
