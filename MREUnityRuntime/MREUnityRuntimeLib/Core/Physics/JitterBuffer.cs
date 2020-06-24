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
	/// TODO: Replace with Unity.Mathematics.RigidTransform from com.unity.mathematics package.
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
			public TransformInfo(Guid id, RigidBodyTransform transform, MotionType motionType)
			{
				Id = id;
				Transform = transform;
				MotionType = motionType;
			}

			/// <summary>
			/// TODO: Use int for transform id.
			/// </summary>
			public Guid Id { get; private set; }

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
		public bool DoSendThisSnapshot() { return (Transforms.Count > 0 || Flags != SnapshotFlags.NoFlags);  }

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
		// TODO: better name, comment, calc uninitialized pieces
		public class RB_Entry
		{
			public RB_Entry(Guid id)
			{
				Id = id;
				Time = float.MinValue;
			}

			public Guid Id;

			public float Time;

			public Guid SourceId;

			public RigidBodyTransform Transform;

			public Vector3 LinearVelocity;

			public Vector3 AngualrVelocity;

			public MotionType MotionType;

			public bool Updated;
		}

		public Guid Id { get; }

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
		private SortedList<Guid, RB_Entry> _rigidBodies = new SortedList<Guid, RB_Entry>();

		/// <summary>
		/// Pending rigid body add/remove actions since last step.
		/// </summary>
		private SortedList<Guid, Action> _pendingRigidBodyManagementActions = new SortedList<Guid, Action>();

		/// <summary>
		/// Running average buffered time and deviation.
		/// </summary>
		private RunningStats _runningStats = new RunningStats();

		/// in order to reset the jitter buffer properly we need to know if the last updates only has sleeping bodies
		private bool _areAllBodiesSleeping = true;

		#region Public API

		public SnapshotBuffer(Guid id)
		{
			Id = id;
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
			// First snapshot has not arrived yet.
			if (LastSnapshotLocalTime < 0)
			{
				return;
			}

			// No need for an update if all rigid bodies are sleeping and no new snapshots or pending actions
			if (_areAllBodiesSleeping && _snapshots.Count == 0 /*&& _pendingRigidBodyManagementActions.Count == 0*/)
			{
				CurrentLocalTime = float.MinValue;

				return;
			}

			// Next timestamp may vary depending network jitter
			float nextTimeStep = caclNextTimeStamp(timestep);

			// Update state from available snapshots
			stepBufferAndUpdateRigidBodies(nextTimeStep);

			if ((_areAllBodiesSleeping && CurrentLocalTime - LastSnapshotLocalTime >= 0.001f) ||
				(!_areAllBodiesSleeping && _snapshots.Count > 0 && _snapshots.Last().Value.Flags == Snapshot.SnapshotFlags.ResetJitterBuffer))
			{
				_snapshots.Clear();
				_runningStats.Reset();
				_areAllBodiesSleeping = true;
			}
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
				_mean = 0.0f;
				_variance = 0.0f;
			}

			public void ShiftTime(float time)
			{
				_mean += time;
			}

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
					_debugStats.add(bufferedTime, _stats.mean(), targetBufferTime, biasedTargetBufferTime, timeShift, nextTimestamp);
#endif

			return nextTimestamp;
		}

		#endregion

		#region Buffer Management

		/// <summary>
		/// Get previous and next snapshot for specified timestamp.
		/// Snapshots older than previous will be deleted.
		/// </summary>
		private void findSnapshots(float time, out Snapshot previous, out Snapshot next)
		{
			// find appropriate snapshots
			int index = 0;
			while (index < _snapshots.Count && time - _snapshots.Keys[index] > 0.001f) index++;

			previous = index == 0 ? null : _snapshots.Values[index - 1];
			next = index < _snapshots.Count ? _snapshots.Values[index] : null;

			// remove old snapshots
			// TODO: find better way
			for (int r = 0; r < index - 1; r++) _snapshots.RemoveAt(0);
		}

		#endregion

		#region Rigid Body State Management

		/// <summary>
		/// Rigid body update provider
		/// </summary>
		private interface ISource
		{
			void Update(ref RB_Entry entry);
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

			public void Update(ref RB_Entry entry)
			{
				while (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].Id.CompareTo(entry.Id) < 0) _iteratorIndex++;

				if (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].Id == entry.Id)
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

			public void Update(ref RB_Entry entry)
			{
				while (_nextIndex < _next.Transforms.Count &&
					_next.Transforms[_nextIndex].Id.CompareTo(entry.Id) < 0)
				{
					_nextIndex++;
				}

				if (_nextIndex >= _next.Transforms.Count ||
					_next.Transforms[_nextIndex].Id != entry.Id)
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
					_prev.Transforms[_prevIndex].Id.CompareTo(entry.Id) < 0)
				{
					_prevIndex++;
				}

				if (_prevIndex < _prev.Transforms.Count &&
						_prev.Transforms[_prevIndex].Id == _next.Transforms[_nextIndex].Id)
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
			// Early-out if there are no rigid bodies or pending actions
			if (_rigidBodies.Count == 0 && _pendingRigidBodyManagementActions.Count == 0)
			{
				return;
			}

			int numSleeping = 0;

			int pi = 0;     // pending index
			int rbi = 0;    // rigid body index

			// todo: make a regular list
			SortedList<Guid, RB_Entry> newRigidBodyList = new SortedList<Guid, RB_Entry>();

			Guid? pid = pi < _pendingRigidBodyManagementActions.Count ? _pendingRigidBodyManagementActions.Keys[pi] : (Guid?)null;

			while (rbi < _rigidBodies.Count)
			{
				Guid rbid = _rigidBodies.Keys[rbi];

				RB_Entry entry = null;

				if (pid.HasValue)
				{
					int cmp = pid.Value.CompareTo(rbid);

					if (cmp < 0)
					{
						if (_pendingRigidBodyManagementActions.Values[pi] == Action.Add)
						{
							entry = new RB_Entry(pid.Value);

							pi++;
							pid = pi < _pendingRigidBodyManagementActions.Count ? _pendingRigidBodyManagementActions.Keys[pi] : (Guid?)null;
						}
						else
						{
							// if remove action, advance pending actions\
							pi++;
							pid = pi < _pendingRigidBodyManagementActions.Count ? _pendingRigidBodyManagementActions.Keys[pi] : (Guid?)null;
							continue;
						}
					}
					else if (cmp == 0)
					{
						if (_pendingRigidBodyManagementActions.Values[pi] == Action.Remove)
						{
							// just skip any action
							pi++;
							pid = pi < _pendingRigidBodyManagementActions.Count ? _pendingRigidBodyManagementActions.Keys[pi] : (Guid?)null;
							rbi++;

							continue;
						}
					}
					else
					{
						entry = _rigidBodies.Values[rbi];
						rbi++;
					}
				}
				else
				{
					entry = _rigidBodies.Values[rbi];
					rbi++;
				}

				// update entry from snapshot
				source.Update(ref entry);

				if (entry.Updated && entry.MotionType == MotionType.Sleeping)
				{
					numSleeping++;
				}

				newRigidBodyList.Add(entry.Id, entry);
			}

			while (pi < _pendingRigidBodyManagementActions.Count)
			{
				if (_pendingRigidBodyManagementActions.Values[pi] == Action.Add)
				{
					RB_Entry entry = new RB_Entry(pid.Value);

					// update entry from snapshot
					source.Update(ref entry);

					if (entry.Updated && entry.MotionType == MotionType.Sleeping)
					{
						numSleeping++;
					}

					newRigidBodyList.Add(entry.Id, entry);
				}

				pi++;
				pid = pi < _pendingRigidBodyManagementActions.Count ? _pendingRigidBodyManagementActions.Keys[pi] : (Guid?)null;
			}

			_areAllBodiesSleeping = numSleeping == newRigidBodyList.Count;

			_rigidBodies.Clear();
			_rigidBodies = newRigidBodyList;

			_pendingRigidBodyManagementActions.Clear();
		}

		private void stepBufferAndUpdateRigidBodies(float nextTimestamp)
		{
			// No available snapshot, snapshot can not be interpolated from the buffers
			// Keep whatever state is already there
			if (_snapshots.Count == 0 || nextTimestamp - LastSnapshotLocalTime > 0.001f)
			{
				HasUpdate = false;
				CurrentLocalTime = nextTimestamp;

				// todo: what if there are pending actions

				return;
			}

			// Snapshot can be interpolated from buffer
			HasUpdate = true;

			Snapshot prev, next;
			findSnapshots(nextTimestamp, out prev, out next);

			if (next == null)
			{
				int x = 0;
				x++;
			}

			// If time offset is less than a millisecond, just use 'next' snapshot time
			if (Math.Abs(next.Time - nextTimestamp) <= 0.001f)
			{
				CurrentLocalTime = next.Time;
				updateRigidBodies(new SnapshotSource(next));

				return;
			}

			// If prev snapshot is missing, just use current transforms with past timestamp
			if (prev == null)
			{
				CurrentLocalTime = nextTimestamp;
				updateRigidBodies(new SnapshotSource(next));

				return;
			}

			// If time offset is less than a millisecond, just use 'prev' snapshot time
			if (Math.Abs(prev.Time - nextTimestamp) <= 0.001)
			{
				CurrentLocalTime = prev.Time;
				updateRigidBodies(new SnapshotSource(prev));

				return;
			}

			// interpolate state between two snapshots
			CurrentLocalTime = nextTimestamp;
			updateRigidBodies(new InterpolationSource(prev, next, nextTimestamp));
		}

		#endregion

		#region Iterator

		int _iteratorIndex = 0;

		internal void ResetIterator()
		{
			_iteratorIndex = 0;
		}

		internal bool Find(Guid key, out RB_Entry rigidBodyData)
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
	}

	/// <todo>
	/// add comments
	/// </todo>
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

					//UnityEngine.Debug.Log(String.Format("Update {0}", sourceId));

				}
			}
			else
			{
				// Register for appropriate source
				_sources[sourceId].RegisterRigidBody(rigidBodyId);
				_rigidBodySourceMap[rigidBodyId] = sourceId;

				//UnityEngine.Debug.Log(String.Format("Register {0}", sourceId));
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
				//UnityEngine.Debug.Log(String.Format("AddSnapshot {0}", sourceId));

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

				SnapshotBuffer.RB_Entry rbState;

				if (source.Find(rb.Key, out rbState))
				{
					snapshot.RigidBodies.Add(rb.Key,
							new MultiSourceCombinedSnapshot.RigidBodyState(
								rb.Key, source.CurrentLocalTime, rbState.Transform, source.HasUpdate, rbState.MotionType));
				}
			}

			snapshotOut = snapshot;
		}
	}
}
