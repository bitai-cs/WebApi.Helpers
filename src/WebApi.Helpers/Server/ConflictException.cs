using System;
using System.Collections.Generic;
using System.Text;

namespace Bitai.WebApi.Server
{
	public class ConflictException : Exception
	{
		public ConflictException(string message) : base(message) { }

		public ConflictException(string message, Exception innerException) : base(message, innerException) { }
	}
}