// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Util
{
	internal class ObjectManager<T>
	{
		private Dictionary<Guid, T> _managedObjects = new Dictionary<Guid, T>();

		internal delegate T FactoryFn(Guid id);

		internal ObjectManager()
		{

		}

		internal T Create(FactoryFn factory)
		{
			var id = Guid.NewGuid();
			T obj = factory(id);
			_managedObjects.Add(id, obj);
			return obj;
		}

		internal void Add(Guid id, T obj)
		{
			_managedObjects.Add(id, obj);
		}

		internal void Remove(Guid id)
		{
			if (_managedObjects.ContainsKey(id))
			{
				_managedObjects.Remove(id);
			}
			else
			{
				MREAPI.Logger.LogWarning($"Removing a managed object of type {typeof(T)} with ID: {id} that is not managed by the manager.");
			}
		}

		internal T Get(Guid id)
		{
			if (_managedObjects.ContainsKey(id))
			{
				return _managedObjects[id];
			}

			return default(T);
		}

		internal IEnumerable<T> GetAll()
		{
			return _managedObjects.Values;
		}
	}
}
