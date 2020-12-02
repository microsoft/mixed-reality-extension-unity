// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.Messaging
{
	public enum TraceSeverity
	{
		Debug,
		Info,
		Warning,
		Error
	}

	public class Trace
	{
		public TraceSeverity Severity;
		public string Message;
	}
}
