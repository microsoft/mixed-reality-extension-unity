// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//#define DEBUG_JITTER_BUFFER

using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MixedRealityExtension.Core.Physics
{
	/// <summary>
	/// Rigid body transform (position and rotation).
	/// </summary>
	public struct RigidBodyTransform
	{
		public Vector3 Position;

		public Quaternion Rotation;

		public void Lerp(RigidBodyTransform t0, RigidBodyTransform t1, float f)
		{
			Position = Vector3.Lerp(t0.Position, t1.Position, f);
			Rotation = Quaternion.Lerp(t0.Rotation, t1.Rotation, f);
		}
	}

	/// <summary>
	/// Snapshot of rigid body transforms at specified point in time.
	/// </summary>
	public class Snapshot
	{
		/// <summary>
		/// Flags that mark special properties of the snapshot
		/// </summary>
		public enum SnapshotFlags : byte
		{
			/// No special treatment
			NoFlags = 0,

			/// Reset the jitter buffer
			ResetJitterBuffer = 1,
		};

		/// <summary>
		/// Transform identifier and respective transform.
		/// </summary>
		public struct TransformInfo
		{
			public TransformInfo(Guid rigidBodyId, RigidBodyTransform transform, MotionType motionType)
			{
				RigidBodyId = rigidBodyId;
				Transform = transform;
				MotionType = motionType;
			}

			public Guid RigidBodyId { get; private set; }

			/// <summary>
			/// The type of the motion
			/// </summary>
			public MotionType MotionType { get; private set; }

			public RigidBodyTransform Transform { get; private set; }
		}

		public Snapshot(float time, List<TransformInfo> transforms, SnapshotFlags snapshotFlags = SnapshotFlags.NoFlags)
		{
			Time = time;
			Transforms = transforms;
			Flags = snapshotFlags;
		}

		/// <summary>
		/// Returns true if this snapshot should be send even if it has no transforms
		/// </summary>
		public bool DoSendThisSnapshot()
		{
			return (Transforms.Count > 0 || Flags != SnapshotFlags.NoFlags);
		}

		/// <summary>
		/// Special flag for a snapshot.
		/// </summary>
		public SnapshotFlags Flags { get; private set; }

		/// <summary>
		/// Timestamp of the snapshot.
		/// </summary>
		public float Time { get; private set; }

		/// <summary>
		/// All transforms in the snapshot.
		/// </summary>
		public List<TransformInfo> Transforms { get; private set; }
	}

	/// <summary>
	/// Stored snapshots from a single source.
	/// </summary>
	public class SnapshotBuffer
	{
		public class RigidBodyData
		{
			public RigidBodyData(Guid id)
			{
				RigidBodyId = id;
				Time = float.MinValue;
			}

			public Guid RigidBodyId;

			public float Time;

			//public Guid SourceId;

			public RigidBodyTransform Transform;

			//public Vector3 LinearVelocity;

			//public Vector3 AngualrVelocity;

			public MotionType MotionType;

			public bool Updated;

#if DEBUG_JITTER_BUFFER
			public GameObject GO = null;
#endif
		}

		public Guid SourceId { get; }

		public float CurrentLocalTime { get; private set; } = float.MinValue;

		public float LastSnapshotLocalTime { get; private set; }  = float.MinValue;

		public bool HasUpdate { get; private set; } = false;

		/// <summary>
		/// Snapshots sorted by snapshot timestamp.
		/// </summary>
		private SortedList<float, Snapshot> _snapshots = new SortedList<float, Snapshot>();

		/// <summary>
		/// Rigid body data sorted by rigid body id.
		/// </summary>
		private SortedList<Guid, RigidBodyData> _rigidBodies = new SortedList<Guid, RigidBodyData>();

		/// <summary>
		/// Pending rigid body add/remove actions since last step.
		/// </summary>
		private SortedList<Guid, Action> _pendingRigidBodyManagementActions = new SortedList<Guid, Action>();

		/// <summary>
		/// Running average buffered time and deviation.
		/// </summary>
		private RunningStats _runningStats = new RunningStats();

		/// in order to reset the jitter buffer properly we need to know if the last updates only has sleeping bodies
		private bool _areAllRigidBodiesSleeping = true;

#region Public API

		public SnapshotBuffer(Guid id)
		{
			SourceId = id;
		}

		/// <summary>
		/// Add snapshot to the buffer if snapshot with same timestamp does not exists in the buffer.
		/// </summary>
		public void AddSnapshot(Snapshot snapshot)
		{
			if (!_snapshots.ContainsKey(snapshot.Time))
			{
				_snapshots.Add(snapshot.Time, snapshot);

				if (snapshot.Time > LastSnapshotLocalTime)
				{
					LastSnapshotLocalTime = snapshot.Time;
				}
			}
		}

		/// <summary>
		///  Forwards the buffer for time depending requested time stap and snapshot availibility.
		/// </summary>
		/// <param name="timestep">Requested time to forward.</param>
		public void Step(float timestep)
		{
			// Create or delete rigid body entries
			processPendingActions();

			// First snapshot has not arrived yet.
			if (LastSnapshotLocalTime < 0)
			{
				return;
			}

			// No need for an update if all rigid bodies are sleeping and no new snapshots
			if (_areAllRigidBodiesSleeping && _snapshots.Count == 0)
			{
				CurrentLocalTime = float.MinValue;

				return;
			}

			// Next timestamp may vary depending network jitter
			float nextTimeStamp = caclNextTimeStamp(timestep);

			// Update state from available snapshots
			stepBufferAndUpdateRigidBodies(nextTimeStamp);
		}

		private enum Action
		{
			Add,
			Remove
		}

		public void RegisterRigidBody(Guid id)
		{
			_pendingRigidBodyManagementActions[id] = Action.Add;
		}

		public void UnregisterRigidBody(Guid id)
		{
			_pendingRigidBodyManagementActions[id] = Action.Remove;
		}

#endregion

#region Running Stats

		/// <summary>
		/// Calculates running stats: mean, variance and deviation.
		/// </summary>
		class RunningStats
		{
			private int _count;
			private float _mean;
			private float _variance;

			/// <summary>
			/// Number of samples to build running stats.
			/// </summary>
			int _windowSize = 120;

			public float Mean => _mean;

			public float Variance => ((_count > 1) ? _variance / (_count - 1) : 0.0f);

			public float Deviation => (float)Math.Sqrt(Variance);

			public RunningStats()
			{
				Reset();
			}

			public void Reset()
			{
				_count = 0;
				_mean = 0.016f;
				_variance = 0.0f;
			}

			// if out time if shifted, we need to apply that shift here as well
			// e.g. if we decided to hold packets longer, we need to put that into running stats as well
			public void ShiftTime(float time)
			{
				_mean += time;
			}

			// add buffer remaining time to running stats
			public void AddSample(float value)
			{
				_count = Math.Min(_windowSize, _count + 1);

				float averageOld = _mean;
				float varianceOld = _variance;

				float oldDiff = value - _mean;

				_mean -= _mean / _count;
				_mean += value / _count;

				float newDiff = value - _mean;

				_variance -= _variance / _count;
				_variance += oldDiff * newDiff;
			}
		}

#if DEBUG_JITTER_BUFFER
			private class DebugStats
			{
				const int _capacity = 5000;

				public void add(float bufferTime, float meanBufferTime, float targetBufferTime, float biasedTargetBufferTime, float timeShift, float outputTime)
				{
					_bufferTime.Add(bufferTime);
					_meanBufferTime.Add(meanBufferTime);
					_targetBufferTime.Add(targetBufferTime);
					_biasedTargetBufferTime.Add(biasedTargetBufferTime);
					_timeShift.Add(timeShift);
					_outputTime.Add(outputTime);
				}

				List<float> _bufferTime = new List<float>(_capacity);
				List<float> _meanBufferTime = new List<float>(_capacity);
				List<float> _targetBufferTime = new List<float>(_capacity);
				List<float> _biasedTargetBufferTime = new List<float>(_capacity);
				List<float> _timeShift = new List<float>(_capacity);
				List<float> _outputTime = new List<float>(_capacity);
			}

			private DebugStats _debugStats = new DebugStats();
#endif

#endregion

#region Time Management

		/// <summary>
		/// Time precision is 1 millisecond.
		/// </summary>
		private const float _timePrecision = 0.001f;

		/// <summary>
		/// todo: add comment
		/// </summary>
		private float _speedUpCoef = 0.2f;

		/// <summary>
		/// todo: add comment
		/// </summary>
		private float _speedDownCoef = 0.5f;

		private float caclNextTimeStamp(float timestep)
		{
			if (CurrentLocalTime < 0.0f)
			{
				return LastSnapshotLocalTime;
			}

			const float bufferTimeBiasTimeUnit = 1.0f / 60;

			float targetBufferTime = _runningStats.Mean - _runningStats.Deviation;

			float biasedTargetBufferTime = targetBufferTime / bufferTimeBiasTimeUnit;
			biasedTargetBufferTime = targetBufferTime > 0 ?
				((int)biasedTargetBufferTime) * bufferTimeBiasTimeUnit : ((int)biasedTargetBufferTime - 1) * bufferTimeBiasTimeUnit;

			float nextTimestamp = CurrentLocalTime + timestep;
			float bufferedTime = LastSnapshotLocalTime - nextTimestamp;

			if (bufferedTime > 1.0f)
			{
				int x = 0;
				x++;
			}

			_runningStats.AddSample(bufferedTime);

			// check if time shift is required
			float timeShift;
			if (Math.Abs(biasedTargetBufferTime) > 0.001)
			{
				// todo: limit slow-down, don't allow time to move to the past
				timeShift = biasedTargetBufferTime > 0 ?
					_speedUpCoef * biasedTargetBufferTime : _speedDownCoef * biasedTargetBufferTime;

				nextTimestamp += timeShift;
				_runningStats.ShiftTime(-timeShift);
			}
			else
			{
				timeShift = 0;
			}

#if DEBUG_JITTER_BUFFER
					_debugStats.add(bufferedTime, _runningStats.Mean, targetBufferTime, biasedTargetBufferTime, timeShift, nextTimestamp);
#endif

			return nextTimestamp;
		}

#endregion

#region Buffer Management

		/// <summary>
		/// Get previous and next snapshot for specified timestamp.
		/// Snapshots older than timestamp will be deleted.
		/// </summary>
		//private void findSnapshots(float time, out Snapshot previous, out Snapshot nextOrCurrent)
		//{
		//	UnityEngine.Debug.Assert(_snapshots.Count == 0 || time <= _snapshots.Count);

		//	// Find appropriate snapshots
		//	int index = 0;
		//	while (index < _snapshots.Count && time - _snapshots.Keys[index] > _timePrecision) index++;

		//	previous = index == 0 ? null : _snapshots.Values[index - 1];
		//	nextOrCurrent = index < _snapshots.Count ? _snapshots.Values[index] : null;

		//	// Remove old snapshots
		//	if (nextOrCurrent != null)
		//	{
		//		int removeThreshold = time - nextOrCurrent.Time >= -_timePrecision ? index : index - 1;
		//		for (int r = 0; r < removeThreshold; r++) _snapshots.RemoveAt(0);
		//	}
		//	else
		//	{
		//		_snapshots.Clear();
		//	}
		//}

#endregion

#region Rigid Body Management

		private void processPendingActions()
		{
			if (_pendingRigidBodyManagementActions.Count == 0)
			{
				return;
			}

			int maxNewCount = _rigidBodies.Count + _pendingRigidBodyManagementActions.Count;
			SortedList<Guid, RigidBodyData> newRigidBodyList = new SortedList<Guid, RigidBodyData>(maxNewCount);

			int rigidBodyIndex = 0;

			foreach (var action in _pendingRigidBodyManagementActions)
			{
				while (rigidBodyIndex < _rigidBodies.Count &&
					_rigidBodies.Values[rigidBodyIndex].RigidBodyId.CompareTo(action.Key) < 0)
				{
					var rb = _rigidBodies.Values[rigidBodyIndex];
					newRigidBodyList.Add(rb.RigidBodyId, rb);

					rigidBodyIndex++;
				}

				if (rigidBodyIndex < _rigidBodies.Count && _rigidBodies.Values[rigidBodyIndex].RigidBodyId == action.Key)
				{
					if (action.Value == Action.Remove)
					{
						// just skip it
#if DEBUG_JITTER_BUFFER
						if (_rigidBodies.Values[rigidBodyIndex].GO)
						{
							GameObject.Destroy(_rigidBodies.Values[rigidBodyIndex].GO);
						}
#endif
					}
					else
					{
						var rb = _rigidBodies.Values[rigidBodyIndex];
						newRigidBodyList.Add(rb.RigidBodyId, rb);
					}

					rigidBodyIndex++;
				}
				else if (action.Value == Action.Add)
				{
					newRigidBodyList.Add(action.Key, new RigidBodyData(action.Key));
				}
			}

			for (; rigidBodyIndex < _rigidBodies.Count; rigidBodyIndex++)
			{
				var rb = _rigidBodies.Values[rigidBodyIndex];
				newRigidBodyList.Add(rb.RigidBodyId, rb);
			}

			newRigidBodyList.TrimExcess();

			_pendingRigidBodyManagementActions.Clear();
			_rigidBodies.Clear();

			_rigidBodies = newRigidBodyList;
		}

		/// <summary>
		/// Rigid body update provider
		/// </summary>
		private interface ISource
		{
			void Update(ref RigidBodyData entry);
		}

		/// <summary>
		/// Update rigid body from snapshot.
		/// </summary>
		private class SnapshotSource : ISource
		{
			private Snapshot _snapshot;
			private int _iteratorIndex = 0;

			public SnapshotSource(Snapshot snapshot)
			{
				_snapshot = snapshot;
			}

			public void Update(ref RigidBodyData entry)
			{
				while (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0) _iteratorIndex++;

				if (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].RigidBodyId == entry.RigidBodyId)
				{
					var rb = _snapshot.Transforms[_iteratorIndex];

					entry.MotionType = rb.MotionType;
					entry.Transform = rb.Transform;
					entry.Time = _snapshot.Time;

					entry.Updated = true;
				}
				else if (entry.MotionType == MotionType.Sleeping)
				{
					entry.Time = _snapshot.Time;
					entry.Updated = true;
				}
				else
				{
					entry.Updated = false;
				}
			}
		}

		/// <summary>
		/// Update rigid body interpolating between two snapshots.
		/// </summary>
		private class InterpolationSource : ISource
		{
			private Snapshot _prev;
			private Snapshot _next;
			private float _frac;

			int _prevIndex = 0;
			int _nextIndex = 0;

			float _timestamp;

			public InterpolationSource(Snapshot prev, Snapshot next, float timestamp)
			{
				_prev = prev;
				_next = next;
				_timestamp = timestamp;
				_frac = (timestamp - prev.Time) / (next.Time - prev.Time); ;
			}

			public void Update(ref RigidBodyData entry)
			{
				while (_nextIndex < _next.Transforms.Count &&
					_next.Transforms[_nextIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0)
				{
					_nextIndex++;
				}

				if (_nextIndex >= _next.Transforms.Count ||
					_next.Transforms[_nextIndex].RigidBodyId != entry.RigidBodyId)
				{
					if (entry.MotionType == MotionType.Sleeping)
					{
						entry.Time = _timestamp;
						entry.Updated = true;
					}
					else
					{
						entry.Updated = false;
					}

					return;
				}

				while (_prevIndex < _prev.Transforms.Count &&
					_prev.Transforms[_prevIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0)
				{
					_prevIndex++;
				}

				if (_prevIndex < _prev.Transforms.Count &&
						_prev.Transforms[_prevIndex].RigidBodyId == _next.Transforms[_nextIndex].RigidBodyId)
				{
					RigidBodyTransform t = new RigidBodyTransform();
					{
						t.Lerp(_prev.Transforms[_prevIndex].Transform, _next.Transforms[_nextIndex].Transform, _frac);
					}

					entry.Transform = t;
				}
				else
				{
					
					entry.Transform = _next.Transforms[_nextIndex].Transform;
				}

				entry.Time = _timestamp;
				entry.MotionType = _next.Transforms[_nextIndex].MotionType;
				entry.Updated = true;
			}
		}

		private void updateRigidBodies(ISource source)
		{
			// Early-out if there are no rigid bodies
			if (_rigidBodies.Count == 0)
			{
				return;
			}

			int numSleeping = 0;
			for (int i = 0; i < _rigidBodies.Count; i++)
			{
				RigidBodyData data = _rigidBodies.Values[i];

				// Update data from snapshot update
				source.Update(ref data);

				if (data.Updated && data.MotionType == MotionType.Sleeping)
				{
					numSleeping++;
				}
			}

			_areAllRigidBodiesSleeping = numSleeping == _rigidBodies.Count;
		}

		private void stepBufferAndUpdateRigidBodies(float nextTimestamp)
		{
			// No available snapshot, snapshot can not be interpolated from the buffers
			// Keep whatever state is already there
			if (_snapshots.Count == 0)
			{
				HasUpdate = false;
				CurrentLocalTime = nextTimestamp;

				return;
			}

			float appliedSnapshotTimestamp = float.MinValue;
			bool reset = false;
			int index = 0;

			while (index < _snapshots.Count && _snapshots.Keys[index] - nextTimestamp <= _timePrecision)
			{
				updateRigidBodies(new SnapshotSource(_snapshots.Values[index]));

				reset = reset || _snapshots.Values[index].Flags == Snapshot.SnapshotFlags.ResetJitterBuffer;
				appliedSnapshotTimestamp = _snapshots.Keys[index];
				index++;
			}

			if (Math.Abs(appliedSnapshotTimestamp - nextTimestamp) <= _timePrecision)
			{
				// we have matching snapshot
				CurrentLocalTime = appliedSnapshotTimestamp;
			}
			else
			{
				CurrentLocalTime = nextTimestamp;

				if (appliedSnapshotTimestamp < nextTimestamp)
				{
					// if there are more snapshots so we can interpolate
					if (index != 0 && index < _snapshots.Count && _snapshots.Count > 1)
					{
						updateRigidBodies(new InterpolationSource(_snapshots.Values[index-1], _snapshots.Values[index], nextTimestamp));
					}
				}
			}

			// delete processed snapshots
			for (int r = 0; r < index; r++) _snapshots.RemoveAt(0);

			if (reset)
			{
				reset = false;
				_runningStats.Reset();
				_areAllRigidBodiesSleeping = true;
			}
		}

#endregion

#region Iterator

		int _iteratorIndex = 0;

		internal void ResetIterator()
		{
			_iteratorIndex = 0;
		}

		internal bool Find(Guid key, out RigidBodyData rigidBodyData)
		{
			while (_iteratorIndex < _rigidBodies.Count && _rigidBodies.Keys[_iteratorIndex].CompareTo(key) < 0) _iteratorIndex++;

			if (_iteratorIndex < _rigidBodies.Count &&
				_rigidBodies.Keys[_iteratorIndex] == key &&
				_rigidBodies.Values[_iteratorIndex].Time >= 0)
			{
				rigidBodyData = _rigidBodies.Values[_iteratorIndex++];
				return true;
			}
			else
			{
				rigidBodyData = null;
				return false;
			}
		}

#endregion

#region Debug

#if DEBUG_JITTER_BUFFER
		internal void UpdateDebugDisplay(Transform root)
		{
			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.GO == null)
				{
					rb.GO = new GameObject();
					rb.GO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					var sc = rb.GO.GetComponent<SphereCollider>();
					sc.enabled = false;

					// display 1.cm sphere
					rb.GO.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
				}


				rb.GO.transform.position = root.TransformPoint(rb.Transform.Position);
				rb.GO.transform.rotation = root.rotation * rb.Transform.Rotation;
			}
		}
#endif

#endregion
	}

	/// <summary>
	/// Combines rigid body transforms from multiple sources.
	/// </summary>
	public class MultiSourceCombinedSnapshot
	{
		public struct RigidBodyState
		{
			public RigidBodyState(Guid id, float time, RigidBodyTransform transform, bool hasUpdate, MotionType mType)
			{
				Id = id;
				LocalTime = time;
				Transform = transform;
				HasUpdate = hasUpdate;
				motionType = mType;
			}

			public Guid Id;

			public float LocalTime;

			public MotionType motionType;

			public bool HasUpdate;

			public RigidBodyTransform Transform;
		}

		public SortedList<Guid, RigidBodyState> RigidBodies = new SortedList<Guid, RigidBodyState>();
	}

	/// <summary>
	/// Multi-source time and snapshot manager.
	/// </summary>
	public class TimeSnapshotManager
	{
		private Dictionary<Guid, SnapshotBuffer> _sources = new Dictionary<Guid, SnapshotBuffer>();

		private SortedDictionary<Guid, Guid> _rigidBodySourceMap = new SortedDictionary<Guid, Guid>();

		public void RegisterOrUpateRigidBody(Guid rigidBodyId, Guid sourceId)
		{
			if (!_sources.ContainsKey(sourceId))
			{
				_sources[sourceId] = new SnapshotBuffer(sourceId);
			}

			if (_rigidBodySourceMap.ContainsKey(rigidBodyId))
			{
				Guid oldSourceId = _rigidBodySourceMap[rigidBodyId];

				if (oldSourceId != sourceId)
				{
					// Unregidter from old source
					_sources[oldSourceId].UnregisterRigidBody(rigidBodyId);

					// Register for new source
					_sources[sourceId].RegisterRigidBody(rigidBodyId);
					_rigidBodySourceMap[rigidBodyId] = sourceId;

				}
			}
			else
			{
				// Register for appropriate source
				_sources[sourceId].RegisterRigidBody(rigidBodyId);
				_rigidBodySourceMap[rigidBodyId] = sourceId;
			}
		}

		public void UnregisterRigidBody(Guid rigidBodyId)
		{
			if (_rigidBodySourceMap.ContainsKey(rigidBodyId))
			{
				Guid oldSourceId = _rigidBodySourceMap[rigidBodyId];

				// Unregidter from source
				_sources[oldSourceId].UnregisterRigidBody(rigidBodyId);
				_rigidBodySourceMap.Remove(rigidBodyId);
			}
		}

		/// <summary>
		/// Add snapshot for specified source.
		/// Register source if entry does not exist.
		/// </summary>
		/// <param name="sourceId">Snapshot source.</param>
		/// <param name="snapshot">List of transform at specified timestamp.</param>
		public void addSnapshot(Guid sourceId, Snapshot snapshot)
		{
			if (!_sources.ContainsKey(sourceId))
			{
				_sources.Add(sourceId, new SnapshotBuffer(sourceId));
			}

			_sources[sourceId].AddSnapshot(snapshot);
		}

		public void Step(float timestep, out MultiSourceCombinedSnapshot snapshotOut)
		{
			// Update transforms holded by each source
			foreach (var source in _sources.Values)
			{
				// Update current state
				source.Step(timestep);

				// Reset rigid body iterator
				source.ResetIterator();
			}

			MultiSourceCombinedSnapshot snapshot = new MultiSourceCombinedSnapshot();

			// Prepare rigid bodies for output
			foreach (var rb in _rigidBodySourceMap)
			{
				var source = _sources[rb.Value];

				SnapshotBuffer.RigidBodyData data;

				if (source.Find(rb.Key, out data))
				{
					snapshot.RigidBodies.Add(rb.Key,
							new MultiSourceCombinedSnapshot.RigidBodyState(
								rb.Key, source.CurrentLocalTime, data.Transform, source.HasUpdate, data.MotionType));
				}
			}

			snapshotOut = snapshot;
		}

		internal void Clear()
		{
			_sources.Clear();
			_rigidBodySourceMap.Clear();
		}

		internal void UpdateDebugDisplay(UnityEngine.Transform root)
		{
#if DEBUG_JITTER_BUFFER
			foreach (var source in _sources.Values)
			{
				source.UpdateDebugDisplay(root);
			}
#endif
		}
	}
}
