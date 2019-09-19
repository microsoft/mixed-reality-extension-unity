// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using UnityEngine;

namespace MixedRealityExtension.Util.Unity
{
	internal delegate void ContinuationHandler(object result);

	internal class MWContinuation
	{
		public object Result;
		private readonly MonoBehaviour _owner;
		private readonly IEnumerator _task;
		private readonly ContinuationHandler _then;

		public MWContinuation(MonoBehaviour owner, IEnumerator task, ContinuationHandler then = null)
		{
			_owner = owner;
			_task = task;
			_then = then;
		}

		public Coroutine Start()
		{
			return _owner.StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			if (_task != null)
			{
				while (_task.MoveNext())
				{
					Result = _task.Current;
					yield return Result;
				}
			}
			else
			{
				yield return null;
			}

			_then?.Invoke(Result);
		}
	}
}
