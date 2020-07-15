// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//#define DEBUG_JITTER_BUFFER

using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;
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
		[Flags]
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
				SetZeroVelocities();
			}

			public void SetZeroVelocities()
			{
				LinearVelocity.Set(0.0F, 0.0F, 0.0F);
				AngularVelocity.Set(0.0F, 0.0F, 0.0F);
			}

			public void SetVelocitiesFromTransformsDiff(float DT, RigidBodyTransform current,
				RigidBodyTransform prev)
			{
				if (Math.Abs(DT) > 0.0005f)
				{
					float invUpdateDT = 1.0f / (DT); // DT has to be different from zero
					UnityEngine.Vector3 eulerAngles = (UnityEngine.Quaternion.Inverse(prev.Rotation) * current.Rotation).eulerAngles;
					UnityEngine.Vector3 radianAngles = UtilMethods.TransformEulerAnglesToRadians(eulerAngles);
					//if (radianAngles.sqrMagnitude < 0.02F)
					//{
					//	Debug.Log("NULL DT=" + DT + " prevRot=" + prev.Rotation.ToString() + " next=" + current.Rotation.ToString());
					//}
					AngularVelocity = radianAngles * invUpdateDT;
					LinearVelocity = (current.Position - prev.Position) * invUpdateDT;
				}
			}

			public Guid RigidBodyId;

			public float Time;

			//public Guid SourceId;

			public RigidBodyTransform Transform;

			public Vector3 LinearVelocity;

			public Vector3 AngularVelocity;

			public MotionType MotionType;

			public bool Updated;

#if DEBUG_JITTER_BUFFER
			public GameObject GO = null;
#endif
		}

		public Guid SourceId { get; }

		public float CurrentLocalTime { get; private set; } = float.MinValue;

		public float LastSnapshotLocalTime { get; private set; } = float.MinValue;

		private float _prevStepLastSnapshotLocalTime = float.MinValue;

		public bool HasUpdate { get; private set; } = false;

		/// <summary>
		/// Snapshots sorted by snapshot timestamp.
		/// </summary>
		private SortedList<float, Snapshot> _snapshots = new SortedList<float, Snapshot>();

		/// <summary>
		/// We might need last applied snapshot for further intepolations.
		/// </summary>
		private Snapshot _lastAppliedSnapshot = null;

		/// <summary>
		/// Rigid body data sorted by rigid body id.
		/// </summary>
		private SortedList<Guid, RigidBodyData> _rigidBodies = new SortedList<Guid, RigidBodyData>();

		/// <summary>
		/// Pending rigid body add/remove actions since last step.
		/// </summary>
		private SortedList<Guid, Action> _pendingRigidBodyManagementActions = new SortedList<Guid, Action>();

		/// <summary>
		/// Running average for packet availability at different time checkpoints relative to current output.
		/// </summary>
		private RunningStats _runningStats = new RunningStats();

		/// <summary>
		/// Tracks updated time within the last second.
		/// </summary>
		private UpdateTimeHealth _upateHealth = new UpdateTimeHealth();

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
				// there are still no updates from this source
				HasUpdate = false;
        
				return;
			}

			// No need for an update if all rigid bodies are sleeping and no new snapshots
			if (_areAllRigidBodiesSleeping && _snapshots.Count == 0)
			{
				// If all bodies are sleeping, there are no new updates.
				// However, since all the bodies are sleeping we can assume state is still valid.
				HasUpdate = true;

				CurrentLocalTime = float.MinValue;
				_prevStepLastSnapshotLocalTime = float.MinValue;

				return;
			}

			float receivedUpdateTime = _prevStepLastSnapshotLocalTime > 0 ? LastSnapshotLocalTime - _prevStepLastSnapshotLocalTime : 0.0f;
			_prevStepLastSnapshotLocalTime = LastSnapshotLocalTime;

			_upateHealth.addSample(receivedUpdateTime);
			_isNetworkHealthy = _upateHealth.UpdatedTime >= 0.9f;

			// Next timestamp may vary depending network jitter
			float nextTimeStamp = caclNextTimeStamp(timestep);

			if (!_wasNetworkHealthy && _isNetworkHealthy)
			{
				// if there is recovery from network glitch, allow next time stamp to go backwards in time
				nextTimeStamp = Math.Min(nextTimeStamp, LastSnapshotLocalTime);
			}

			// if network is healthy, we don't want large buffer time
			if (_isNetworkHealthy)
			{
				const float maxBufferTime = 0.1f;
				nextTimeStamp = Math.Max(nextTimeStamp, LastSnapshotLocalTime - maxBufferTime);
			}

			// Update state from available snapshots
			stepBufferAndUpdateRigidBodies(nextTimeStamp);

			_wasNetworkHealthy = _isNetworkHealthy;
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

		#region Stats

		bool _isNetworkHealthy = true;
		bool _wasNetworkHealthy = true;

		/// <summary>
		/// Based on time covered by updates received with last second.
		/// </summary>
		class UpdateTimeHealth
		{
			/// <summary>
			/// Running average time covered with updates within past second.
			/// </summary>
			public float UpdatedTime = 1.0f;

			private int _runningWindowsSize = 60;

			public void addSample(float updateTime)
			{
				UpdatedTime -= UpdatedTime / _runningWindowsSize;
				UpdatedTime += updateTime;
			}
		}

		/// <summary>
		/// Running average for update availability at different time checkpoints relative to current output.
		/// </summary>
		class RunningStats
		{
			const int _numRunningWindowSamples = 100;

			public const float TimeStep = 1.0f / 120;

			const float _timespan = 0.1f;

			const int _count = (int)(2 * _timespan / TimeStep) + 1;

			const int _current = _count / 2;

			private float[] _bufferTimeHeuristics = new float[_count];

			/// <summary>
			/// Estimated update availability if output is slowed down.
			/// </summary>
			public float SlowDownIndicator { get { return _bufferTimeHeuristics[_current + 1]; } }

			/// <summary>
			/// Estimated update availability with current output rate.
			/// </summary>
			public float CurrentRateQuality { get { return _bufferTimeHeuristics[_current]; } }

			/// <summary>
			/// Estimated update availability if output is sped up.
			/// </summary>
			public float SpeedUpIndicator { get { return _bufferTimeHeuristics[_current - 1]; } }

			public float AverageBufferTime = 0.0f;

			public RunningStats()
			{
				Reset();
			}

			public void Reset()
			{
				for (int i = _current; i < _bufferTimeHeuristics.Length; i++)
				{
					// at current buffering time and later, updates are always available
					_bufferTimeHeuristics[i] = 1.0f;
				}

				for (int i = _current - 1; i >= 0; i--)
				{
					// halve the initial probability for each folowing speedup increment
					_bufferTimeHeuristics[i] = _bufferTimeHeuristics[i + 1] / 2;
				}
			}

			public void addSample(float bufferTime)
			{
				// Update buffer time running average
				AverageBufferTime -= AverageBufferTime / _numRunningWindowSamples;
				AverageBufferTime += bufferTime / _numRunningWindowSamples;

				const float increment = 1.0f / _numRunningWindowSamples;

				// update fixed step buffer time availability
				for (int i = 0; i < _bufferTimeHeuristics.Length; i++)
				{
					int offset = i - _current;
					float value = bufferTime + offset * TimeStep;

					_bufferTimeHeuristics[i] -= _bufferTimeHeuristics[i] / _numRunningWindowSamples;
					_bufferTimeHeuristics[i] = Math.Max(0.0f, _bufferTimeHeuristics[i]);

					if (value >= 0.0f)
					{
						_bufferTimeHeuristics[i] += increment;
						_bufferTimeHeuristics[i] = Math.Min(1.0f, _bufferTimeHeuristics[i]);
					}
				}
			}

			/// <summary>
			/// Output time is shifting, we need to shift heuristic values as well.
			/// </summary>
			public void SlowDown()
			{
				int i = 0;
				for (; i < _bufferTimeHeuristics.Length - 1; i++)
				{
					_bufferTimeHeuristics[i] = _bufferTimeHeuristics[i + 1];
				}

				_bufferTimeHeuristics[i] = 0.0f;
			}

			/// <summary>
			/// Output time is shifting, we need to shift heuristic values as well.
			/// </summary>
			public void SpeedUp()
			{
				int i = _bufferTimeHeuristics.Length - 1;
				for (; i > 0; i--)
				{
					_bufferTimeHeuristics[i] = _bufferTimeHeuristics[i - 1];
				}

				_bufferTimeHeuristics[i] = 0.0f;
			}

		}

		#endregion

		#region Time Management

		/// <summary>
		/// Time precision is 1 millisecond.
		/// </summary>
		private const float _timePrecision = 0.001f;

		private float caclNextTimeStamp(float timestep)
		{
			// if it was not running just use last received value
			if (CurrentLocalTime < 0.0f)
			{
				float initBufferTime = Math.Max(_runningStats.AverageBufferTime, 0.08f);
				initBufferTime = Math.Min(initBufferTime, 0.05f);

				// start a bit conservative, give buffer time to fill up and avoid jitter in first few frames
				return LastSnapshotLocalTime - initBufferTime;
			}

			// if there is a durable packet loss, skip updating stats and just pretend time passes as expected
			if (!_isNetworkHealthy)
			{
				return CurrentLocalTime + timestep * 0.999f; // let's be a bit conservative
			}

			float nextTimeStamp = CurrentLocalTime + timestep;
			float bufferedTime = LastSnapshotLocalTime - nextTimeStamp;

			_runningStats.addSample(bufferedTime);

			// there is bad quality with current output time pace, slow down if it would help
			if (_runningStats.CurrentRateQuality < 0.85f &&
				_runningStats.SlowDownIndicator >= _runningStats.CurrentRateQuality)
			{
				_runningStats.SlowDown();
				return nextTimeStamp - RunningStats.TimeStep;
			}

			// speed up if can have good quality with shorted buffer time
			if (_runningStats.SpeedUpIndicator > 0.95f)
			{
				_runningStats.SpeedUp();
				return nextTimeStamp + RunningStats.TimeStep;
			}

			return nextTimeStamp;
		}

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
			// used only for velocity estimation
			private Snapshot _prevSnapshot;
			private int _iteratorIndex = 0;
			private int _prevIndex = 0;

			public SnapshotSource(Snapshot snapshot, Snapshot prevsnapShot)
			{
				_snapshot = snapshot;
				_prevSnapshot = prevsnapShot;
			}

			public void Update(ref RigidBodyData entry)
			{
				if (_prevSnapshot != null)
				{
					while (_prevIndex < _prevSnapshot.Transforms.Count &&
						_prevSnapshot.Transforms[_prevIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0)
					{
						_prevIndex++;
					}
				}

				while (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0)
				{
					_iteratorIndex++;
				}

				//Debug.Log(" SnapshotSource Update cI:" + _iteratorIndex + " pI:" + _prevIndex +
				//	" DT=" + (_snapshot.Time - _prevSnapshot.Time));

				if (_iteratorIndex < _snapshot.Transforms.Count &&
					_snapshot.Transforms[_iteratorIndex].RigidBodyId == entry.RigidBodyId)
				{
					var rb = _snapshot.Transforms[_iteratorIndex];

					entry.MotionType = rb.MotionType;
					entry.Transform = rb.Transform;
					entry.Time = _snapshot.Time;
					entry.SetZeroVelocities();

					if (_prevSnapshot != null)
					{
						if (_prevIndex < _prevSnapshot.Transforms.Count &&
							_prevSnapshot.Transforms[_prevIndex].RigidBodyId == entry.RigidBodyId)
						{
							// estimate velocity
							float DT = _snapshot.Time - _prevSnapshot.Time;
							var prevRB = _prevSnapshot.Transforms[_prevIndex].Transform;
							entry.SetVelocitiesFromTransformsDiff(DT, rb.Transform, prevRB);
						}
					}

					entry.Updated = true;
				}
				else if (entry.MotionType == MotionType.Sleeping)
				{
					entry.Time = _snapshot.Time;
					entry.Updated = true;
					entry.SetZeroVelocities();
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

				while (_prevIndex < _prev.Transforms.Count &&
					_prev.Transforms[_prevIndex].RigidBodyId.CompareTo(entry.RigidBodyId) < 0)
				{
					_prevIndex++;
				}

				if (_nextIndex >= _next.Transforms.Count ||
					_next.Transforms[_nextIndex].RigidBodyId != entry.RigidBodyId)
				{
					// velocity is zero
					entry.SetZeroVelocities();

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

				if (_prevIndex < _prev.Transforms.Count &&
						_prev.Transforms[_prevIndex].RigidBodyId == _next.Transforms[_nextIndex].RigidBodyId)
				{
					RigidBodyTransform t = new RigidBodyTransform();
					{
						t.Lerp(_prev.Transforms[_prevIndex].Transform, _next.Transforms[_nextIndex].Transform, _frac);
					}
					entry.Transform = t;
					entry.SetZeroVelocities();
					// estimate velocity
					float DT = _next.Time - _prev.Time;
					var prevT = _prev.Transforms[_prevIndex].Transform;
					var currentT = _next.Transforms[_nextIndex].Transform;
					entry.SetVelocitiesFromTransformsDiff(DT, currentT, prevT);
				}
				else
				{
					entry.Transform = _next.Transforms[_nextIndex].Transform;
					entry.SetZeroVelocities();
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
			// No available snapshot and snapshot can not be interpolated from the buffer.
			// Keep whatever state is already there, but set the flag that we don't have correct state.
			if (_snapshots.Count == 0)
			{
				CurrentLocalTime = nextTimestamp;
				HasUpdate = false;

				return;
			}

			float appliedSnapshotTimestamp = float.MinValue;
			bool reset = false;
			int index = 0;

			// go through all the snapshots in the past
			while (index < _snapshots.Count && _snapshots.Keys[index] - nextTimestamp <= _timePrecision)
			{
				//Debug.Log(" stepBufferAndUpdateRigidBodies index=" + index + " count=" + _snapshots.Count);
				updateRigidBodies(new SnapshotSource(_snapshots.Values[index], _lastAppliedSnapshot));

				bool resetCurrent = _snapshots.Values[index].Flags == Snapshot.SnapshotFlags.ResetJitterBuffer;
				reset = reset || resetCurrent;

				_lastAppliedSnapshot = resetCurrent ? null : _snapshots.Values[index];
				appliedSnapshotTimestamp = _snapshots.Keys[index];

				index++;
			}

			if (Math.Abs(appliedSnapshotTimestamp - nextTimestamp) <= _timePrecision)
			{
				// we have a matching snapshot
				HasUpdate = true;
				CurrentLocalTime = appliedSnapshotTimestamp;
			}
			else
			{
				CurrentLocalTime = nextTimestamp;

				if (appliedSnapshotTimestamp < nextTimestamp)
				{
					// if there are more snapshots so we can interpolate
					if (_lastAppliedSnapshot != null && index < _snapshots.Count)
					{
						HasUpdate = true;
						updateRigidBodies(new InterpolationSource(_lastAppliedSnapshot, _snapshots.Values[index], nextTimestamp));
					}
					else
					{
						HasUpdate = false;
					}
				}
				else
				{
					HasUpdate = true;
				}
			}

			// delete processed snapshots
			for (int r = 0; r < index; r++) _snapshots.RemoveAt(0);

			if (reset)
			{
				reset = false;
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
			public RigidBodyState(Guid id, float time, RigidBodyTransform transform,
				Vector3 linearVelocity, Vector3 angularVelocity,
				bool hasUpdate, MotionType mType)
			{
				Id = id;
				LocalTime = time;
				Transform = transform;
				LinearVelocity = linearVelocity;
				AngularVelocity = angularVelocity;
				HasUpdate = hasUpdate;
				motionType = mType;
			}

			public Guid Id;

			public float LocalTime;

			public MotionType motionType;

			public bool HasUpdate;

			public RigidBodyTransform Transform;

			public Vector3 LinearVelocity;

			public Vector3 AngularVelocity;
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
					// Unregister from old source
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

				// Unregister from source
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
			// Update transforms held by each source
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
								rb.Key, source.CurrentLocalTime, data.Transform,
								data.LinearVelocity, data.AngularVelocity,
								source.HasUpdate, data.MotionType));
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
