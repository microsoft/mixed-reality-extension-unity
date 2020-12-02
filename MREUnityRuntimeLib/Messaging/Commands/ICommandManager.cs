// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Messaging.Commands
{
	internal interface ICommandManager
	{
		void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload, Action onCompleteCallback);
	}
}
