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
	/// <todo>
	/// Replace all usages with Unity.Mathematics.RigidTransform once com.unity.mathematics package becomes availbale.
	/// </todo>
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
		/// flags that mark special properties of the snapshot
		public enum SnapshotFlags : byte
		{
			/// no special treatment
			NoFlags = 0,
			/// reset the jitter buffer
			ResetJitterBuffer = 1,
		};

		/// <summary>
		/// Transform identifier and respective transform.
		/// </summary>
		public struct TransformInfo
		{
			public TransformInfo(Guid id, RigidBodyTransform transform, MotionType mType)
			{
				Id = id;
				Transform = transform;
				motionType = mType;
			}

			/// <todo>
			/// use int as transform id
			/// </todo>
			public Guid Id { get; private set; }

			/// the type of the motion
			public MotionType motionType { get; set; }

			public RigidBodyTransform Transform { get; private set; }
		}

		public Snapshot(float time, List<TransformInfo> transforms,
			SnapshotFlags snapFlags = SnapshotFlags.NoFlags)
		{
			Time = time;
			Transforms = transforms;
			Flags = snapFlags;
		}

		/// returns true if this snapshot should be send even if it has no transforms
		public bool DoSendThisSnapshot() { return (Transforms.Count > 0 || Flags != SnapshotFlags.NoFlags);  }

		///special flag for a snapshot
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
		/// <summary>
		/// Add snapshot to buffer if snapshot with same timestamp exists in buffer.
		/// </summary>
		public void addSnapshot(Snapshot snapshot)
		{

			// Reset the jitter buffer if this is requested by the update flag
			if (snapshot.Flags == Snapshot.SnapshotFlags.ResetJitterBuffer)
			{
#if MRE_PHYSICS_DEBUG
				Debug.Log(" RESET JB FLAGS time:" + snapshot.Time  + " size:" + Snapshots.Count);
#endif
				Snapshots.Clear();
				areAllBodiesSleepingInTheLastSnapshot = false;
			}

			if (!Snapshots.ContainsKey(snapshot.Time))
			{
				// get the previous snapshot that should contain the list of all sleeping bodies, but only if there is a last snapshot
				if (Snapshots.Count > 0)
				{
					int indCurrent = 0, indLast = 0;
					List<Snapshot.TransformInfo> mergedWithSleepingListTransforms = new List<Snapshot.TransformInfo>();

					var lastsnapshot = Snapshots.Last().Value;
					bool isEverythingSleeping = true;

					while (indCurrent < snapshot.Transforms.Count || indLast < lastsnapshot.Transforms.Count)
					{
						// find the next sleeping in the last list that will be propagated further
						while (indLast < lastsnapshot.Transforms.Count && lastsnapshot.Transforms[indLast].motionType != MotionType.Sleeping)
						{
							indLast++;
						}

						// here merge the 2 lists items such that they are in an incremental order
						bool r1 = indCurrent < snapshot.Transforms.Count;
						bool r2 = indLast < lastsnapshot.Transforms.Count;
						if (r1 && r2)
						{
							int cmpValue = snapshot.Transforms[indCurrent].Id.CompareTo(lastsnapshot.Transforms[indLast].Id);
							if (cmpValue <= 0)
							{
								isEverythingSleeping = isEverythingSleeping && (snapshot.Transforms[indCurrent].motionType == MotionType.Sleeping);
								mergedWithSleepingListTransforms.Add(snapshot.Transforms[indCurrent]);
								indCurrent++;
								indLast += (cmpValue == 0) ? 1 : 0;
							}
							else
							{
								isEverythingSleeping = isEverythingSleeping && (lastsnapshot.Transforms[indLast].motionType == MotionType.Sleeping);
								mergedWithSleepingListTransforms.Add(lastsnapshot.Transforms[indLast]);
								indLast++;
							}
						}
						else
						{
							if (r1)
							{
								isEverythingSleeping = isEverythingSleeping && (snapshot.Transforms[indCurrent].motionType == MotionType.Sleeping);
								mergedWithSleepingListTransforms.Add(snapshot.Transforms[indCurrent]);
								indCurrent++;
							}
							else
							{
								if (r2)
								{
									isEverythingSleeping = isEverythingSleeping && (lastsnapshot.Transforms[indLast].motionType == MotionType.Sleeping);
									mergedWithSleepingListTransforms.Add(lastsnapshot.Transforms[indLast]);
									indLast++;
								}
							}
						}
					}
#if MRE_PHYSICS_DEBUG
					Debug.Log(" After merge: " + mergedWithSleepingListTransforms.Count + " before:" + snapshot.Transforms.Count
						+ " last:" + lastsnapshot.Transforms.Count + " time:" + snapshot.Time + " isEverythingSleeping=" + isEverythingSleeping
						+ " total size:" + Snapshots.Count);
#endif
					var snapshotExtended = new Snapshot(snapshot.Time, mergedWithSleepingListTransforms);

					// if all bodies were sleeping in the previous update then clear all the jitter buffer
					if (areAllBodiesSleepingInTheLastSnapshot)
					{
#if MRE_PHYSICS_DEBUG
						Debug.Log(" RESET JB  time:" + snapshot.Time + " isEverythingSleeping=" + isEverythingSleeping
							+ " size:" + Snapshots.Count);
#endif
						Snapshots.Clear();
					}
					
					Snapshots.Add(snapshot.Time, snapshotExtended);
					areAllBodiesSleepingInTheLastSnapshot = isEverythingSleeping;
				}
				else
				{
					Snapshots.Add(snapshot.Time, snapshot);
					areAllBodiesSleepingInTheLastSnapshot = false;
				}
			}
		}

		/// <summary>
		/// Get previous and next snapshot for specified timestamp.
		/// Snapshots older than previous will be deleted.
		/// </summary>
		public void step(float time, out Snapshot previous, out Snapshot next)
		{
			// find appropriate snapshots
			int index = 0;
			while (index < Snapshots.Count && time > Snapshots.Keys[index]) index++;

			previous = index == 0 ? null : Snapshots.Values[index - 1];
			next = index < Snapshots.Count ? Snapshots.Values[index] : null;

			// remove old snapshots, todo: find better way
			for (int r = 0; r < index - 1; r++) Snapshots.RemoveAt(0);
		}

		/// in order to reset the jitter buffer properly we need to know if the last updates only has sleeping bodies
		public bool areAllBodiesSleepingInTheLastSnapshot = false;

		/// <summary>
		/// Snapshots sorted by snapshot timestamp.
		/// </summary>
		public SortedList<float, Snapshot> Snapshots = new SortedList<float, Snapshot>();
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
		public class SourceInfo
		{
			/// <todo>
			/// is this required?
			/// </todo>
			private enum Mode
			{
				Init,
				Play,
			}

			/// <todo>
			/// add comments
			/// </todo>
			class RunningStats
			{
				int _count = 0;

				float _mean = 0.0f;
				float _variance = 0.0f;

				// todo: expose as a parameter
				int _windowSize = 120;

				public void shiftTime(float time)
				{
					_mean += time;
				}

				public void addSample(float value)
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

				public float mean() { return _mean; }

				public float variance()
				{
					return ((_count > 1) ? _variance / (_count - 1) : 0.0f);
				}

				public float deviation()
				{
					return (float)Math.Sqrt(variance());
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

			public SourceInfo(Guid id)
			{
				Id = id;
				CurrentSnapshot = null;
				CurrentLocalTime = float.MinValue;
			}

			public void addSnapshot(Snapshot snapshot)
			{
				SnapshotBuffer.addSnapshot(snapshot);
			}

			public void step(float timestep)
			{
				if (_mode == Mode.Init || SnapshotBuffer.Snapshots.Count < 4)
				{
					// todo: do we need this warm up?
					if (SnapshotBuffer.Snapshots.Count >= 4)
					{
						_mode = Mode.Play;

						CurrentSnapshot = SnapshotBuffer.Snapshots.First().Value;
						CurrentLocalTime = CurrentSnapshot.Time;
					}
					else
					{
						if (_mode != Mode.Init)
						{
							CurrentSnapshot = SnapshotBuffer.Snapshots.First().Value;
							CurrentLocalTime = CurrentSnapshot.Time;
						}
					}
				}
				else if (_mode != Mode.Init)
				{
					// todo: consider exposing as a parameter or calculating based on local and remote timesteps.
					const float bufferTimeBiasTimeUnit = 1.0f / 60;

					float targetBufferTime = _stats.mean() - _stats.deviation();
					float biasedTargetBufferTime = targetBufferTime / bufferTimeBiasTimeUnit;

					biasedTargetBufferTime = biasedTargetBufferTime > 0 ?
						((int)biasedTargetBufferTime) * bufferTimeBiasTimeUnit : ((int)biasedTargetBufferTime - 1) * bufferTimeBiasTimeUnit;

					float nextTimestamp = CurrentLocalTime + timestep;
					float bufferedTime = SnapshotBuffer.Snapshots.Last().Value.Time - nextTimestamp;

					_stats.addSample(bufferedTime);

					// check if time shift is required
					float timeShift;
					if (Math.Abs(biasedTargetBufferTime) > 0.001)
					{
						// todo: limit slow-down, don't allow time to move to the past
						timeShift = biasedTargetBufferTime > 0 ?
							_speedUpCoef * biasedTargetBufferTime : _speedDownCoef * biasedTargetBufferTime;

						nextTimestamp += timeShift;
						_stats.shiftTime(-timeShift);
					}
					else
					{
						timeShift = 0;
					}

#if DEBUG_JITTER_BUFFER
					_debugStats.add(bufferedTime, _stats.mean(), targetBufferTime, biasedTargetBufferTime, timeShift, nextTimestamp);
#endif

					if (nextTimestamp <= SnapshotBuffer.Snapshots.Last().Value.Time)
					{
						// Snapshot can be interpolated from the buffers
						HasUpdate = true;

						Snapshot prev, next;
						SnapshotBuffer.step(nextTimestamp, out prev, out next);

						if (Math.Abs(next.Time - nextTimestamp) < 0.001)
						{
							// if offset is less than a millisecond, just use 'next' snapshot time
							CurrentLocalTime = next.Time;
							CurrentSnapshot = next;
						}
						else if (prev != null)
						{
							if (Math.Abs(prev.Time - nextTimestamp) < 0.001)
							{
								// if offset is less than a millisecond, just use 'prev' snapshot time
								CurrentLocalTime = prev.Time;
								CurrentSnapshot = prev;
							}
							else
							{
								float frac = (nextTimestamp - prev.Time) / (next.Time - prev.Time);

								List<Snapshot.TransformInfo> transforms = new List<Snapshot.TransformInfo>(next.Transforms.Count);

								int prevIndex = 0;
								int nextIndex = 0;

								for (; nextIndex < next.Transforms.Count; nextIndex++)
								{
									// find corresponding transform in prev snapshot
									while (prevIndex < prev.Transforms.Count && prev.Transforms[prevIndex].Id.CompareTo(next.Transforms[nextIndex].Id) < 0)
									{
										prevIndex++;
									}

									if (prevIndex < prev.Transforms.Count &&
										prev.Transforms[prevIndex].Id == next.Transforms[nextIndex].Id)
									{
										RigidBodyTransform t = new RigidBodyTransform();
										{
											t.Lerp(prev.Transforms[prevIndex].Transform, next.Transforms[nextIndex].Transform, frac);
										}

										transforms.Add(new Snapshot.TransformInfo(next.Transforms[nextIndex].Id, t,
											next.Transforms[nextIndex].motionType ));
									}
									else
									{
										transforms.Add(new Snapshot.TransformInfo(next.Transforms[nextIndex].Id,
											next.Transforms[nextIndex].Transform, next.Transforms[nextIndex].motionType));
									}
								}

								// interpolated snapshot
								CurrentLocalTime = nextTimestamp;
								CurrentSnapshot = new Snapshot(nextTimestamp, transforms, prev.Flags);
							}
						}
						else
						{
							// if prev snapshot is missing, just use current transforms with past timestamp
							CurrentLocalTime = nextTimestamp;
							CurrentSnapshot = next;
						}
					}
					else
					{
						// Snapshot can not be interpolated from the buffers
						HasUpdate = false;

						CurrentLocalTime = nextTimestamp;
						CurrentSnapshot = new Snapshot(CurrentLocalTime, CurrentSnapshot.Transforms);
					}
				}
			}

			/// <summary>
			/// Source identifier.
			/// </summary>
			public Guid Id { get; private set; }

			/// <summary>
			/// This source snapshots.
			/// </summary>
			private SnapshotBuffer SnapshotBuffer = new SnapshotBuffer();

			/// <todo>
			/// can this be avoided?
			/// </todo>
			private Mode _mode = Mode.Init;

			/// <summary>
			/// Calc running average buffered time and deviation.
			/// </summary>
			private RunningStats _stats = new RunningStats();

			/// <summary>
			/// todo: add comment
			/// </summary>
			private float _speedUpCoef = 0.2f;

			/// <summary>
			/// todo: add comment
			/// </summary>
			private float _speedDownCoef = 0.5f;

			/// <summary>
			/// Specifies if there is an update since the last step.
			/// </summary>
			public bool HasUpdate = false;

			/// <summary>
			/// Timestamp of the snapshot in local time of the source.
			/// </summary>
			float CurrentLocalTime = float.MinValue;

			/// <summary>
			/// Timestamp and list of rigid body transforms.
			/// </summary>
			public Snapshot CurrentSnapshot { get; private set; }
		}

		/// <summary>
		/// Add snapshot for specified source.
		/// Register source if entry does not exist.
		/// </summary>
		/// <param name="sourceId">Snapshot owner.</param>
		/// <param name="snapshot">List of transform at specified timestamp.</param>
		public void addSnapshot(Guid sourceId, Snapshot snapshot)
		{
			if (!Sources.ContainsKey(sourceId))
			{
				Sources.Add(sourceId, new SourceInfo(sourceId));
			}

			Sources[sourceId].addSnapshot(snapshot);
		}

		/// <summary>
		/// Generate next snapshot with specified timestep.
		/// </summary>
		/// <param name="timestep"></param>
		/// <returns></returns>
		public MultiSourceCombinedSnapshot GetNextSnapshot(float timestep)
		{
			MultiSourceCombinedSnapshot snapshot = new MultiSourceCombinedSnapshot();

			foreach (SourceInfo source in Sources.Values)
			{
				// move snapshot forward for specified timestep
				source.step(timestep);

				// if it's new source, it may have no snapshot
				if (source.CurrentSnapshot != null)
				{
					foreach (var t in source.CurrentSnapshot.Transforms)
					{
						snapshot.RigidBodies.Add(t.Id,
							new MultiSourceCombinedSnapshot.RigidBodyState(t.Id, source.CurrentSnapshot.Time,
							t.Transform, source.HasUpdate, t.motionType));
					}
				}
			}

			return snapshot;
		}

		/// There are sleeping bodies that are kept in the jitter buffer without any update, and if
		/// body is removed or when the ownership is transfered these bodies should not be kept further
		/// in the buffer from that client and should no be marked as sleeping. 
		public void DelteBodyFromBuffer(Guid bodyID)
		{
#if MRE_PHYSICS_DEBUG
			Debug.Log(" Called DelteBodyFromBufferIfSleeping body: " + bodyID.ToString() + " size:" + Sources.Count);
#endif
			int sourceIndex = 0;
			//foreach (SourceInfo source in Sources.Values)
			while (sourceIndex < Sources.Count)
			{
				var currentSource = Sources.ElementAt(sourceIndex);
				// if it's new source, it may have no snapshot
				if (currentSource.Value.CurrentSnapshot != null)
				{
					int ind = 0;
#if MRE_PHYSICS_DEBUG
					Debug.Log(" source : " + currentSource.Key.ToString() + " size:" + currentSource.Value.CurrentSnapshot.Transforms.Count);
#endif
					while (ind < currentSource.Value.CurrentSnapshot.Transforms.Count)
					{
						int cmp = bodyID.CompareTo(currentSource.Value.CurrentSnapshot.Transforms[ind].Id);
						if (cmp == 0)
						{
							Sources[currentSource.Key].CurrentSnapshot.Transforms.RemoveAt(ind);
#if MRE_PHYSICS_DEBUG
				Debug.Log(" Found DelteBodyFromBufferIfSleeping body: " + bodyID.ToString()
					+ " new size:" + Sources[currentSource.Key].CurrentSnapshot.Transforms.Count);
#endif
							break;
						}
						// this is a sorted list so once we passed this we can safely break
						if (cmp < 0)
						{
							break;
						}
						ind++;
					}
				}
				sourceIndex++;
			}
		}

		private Dictionary<Guid, SourceInfo> Sources = new Dictionary<Guid, SourceInfo>();
	}
}
