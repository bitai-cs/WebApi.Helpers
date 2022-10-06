using System;
using System.Collections.Generic;
using System.Text;

namespace Bitai.WebApi.Server
{
	public class UnauthorizedException : Exception
	{
		public UnauthorizedException(string message) : base(message) { }

		public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
	}
}