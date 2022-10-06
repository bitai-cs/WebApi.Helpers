using System;
using System.Collections.Generic;
using System.Text;

namespace Bitai.WebApi.Server
{
	public class ResourceNotFoundException : Exception
	{
		public ResourceNotFoundException(string message) : base(message) { }

		public ResourceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
	}
}