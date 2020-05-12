using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MixedRealityExtension.Core.Physics;

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
	public class Snapshot_WIP
	{
		/// <summary>
		/// Trnasform identifier and respective transform.
		/// </summary>
		public struct TransformInfo
		{
			public TransformInfo(Guid id, RigidBodyTransform transform)
			{
				Id = id;
				Transform = transform;
			}

			public Guid Id { get; private set; }

			public RigidBodyTransform Transform { get; private set; }
		}

		public Snapshot_WIP(float time, List<TransformInfo> transforms)
		{
			Time = time;
			Transforms = transforms;
		}

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
	public class SnapsotBuffer_WIP
	{
		/// <summary>
		/// Add snapshot to buffer if snapshot with same timestamp exists in buffer.
		/// </summary>
		public void addSnapshot(Snapshot_WIP snapshot)
		{
			if (!Snapshots.ContainsKey(snapshot.Time))
			{
				Snapshots.Add(snapshot.Time, snapshot);
			}
		}

		/// <summary>
		/// Get previous and next snapshot for specified timestamp.
		/// Snapshots older than previous will be deleted.
		/// </summary>
		public void step(float time, out Snapshot_WIP previous, out Snapshot_WIP next)
		{
			// find appropriate snapshots
			int index = 0;
			while (index < Snapshots.Count && time > Snapshots.Keys[index]) index++;

			previous = index == 0 ? null : Snapshots.Values[index - 1];
			next = index < Snapshots.Count ? Snapshots.Values[index] : null;

			// remove old snapshots, todo: find better way
			for (int r = 0; r < index - 1; r++) Snapshots.RemoveAt(0);
		}

		/// <summary>
		/// Snapshots sorted by snapshot timestamp.
		/// </summary>
		public SortedList<float, Snapshot_WIP> Snapshots = new SortedList<float, Snapshot_WIP>();
	}

	public class MultiSourceCombinedSnapshot
	{
		public struct RigidBodyState
		{
			public RigidBodyState(Guid id, float time, RigidBodyTransform transform)
			{
				Id = id;
				LocalTime = time;
				Transform = transform;
			}

			public Guid Id;

			public float LocalTime;

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
			private enum Mode
			{
				Init,
				Play,
			}

			class RunningStats
			{
				int count = 0;

				int windowSize = 120;

				float average = 0.0f;
				float variance = 0.0f;

				public void add(float value)
				{
					count = Math.Min(windowSize, count + 1);

					float averageOld = average;
					float varianceOld = variance;

					average -= average / count;
					average += value / count;

					float varianceOld2 = varianceOld - varianceOld / count;
					variance = varianceOld2 + (value - averageOld) * (value - average);
				}

				public float getAverage()
				{
					return average;
				}

				public float getVariance()
				{
					return ((count > 1) ? variance/(count - 1) : 0.0f );
				}

				public float getStandardDeviation()
				{
					return (float) Math.Sqrt(getVariance());
				}
			}

			private RunningStats _stats = new RunningStats();

			public SourceInfo(Guid id)
			{
				Id = id;
				CurrentSnapshot = null;
				CurrentLocalTime = float.MinValue;
			}

			public void addSnapshot(Snapshot_WIP snapshot)
			{
				SnapshotBuffer.addSnapshot(snapshot);
			}

			List<float> delay = new List<float>(5000);
			List<float> mean = new List<float>(5000);
			List<float> variance = new List<float>(5000);
			List<float> stddev = new List<float>(5000);

			float bufferedTimeRunningAerage = 0.0f;
			float _QoS = 0.5f;

			public void step(float timestep)
			{
				if (_mode == Mode.Init)
				{
					if (SnapshotBuffer.Snapshots.Count >= 10)
					{
						_mode = Mode.Play;

						CurrentSnapshot = SnapshotBuffer.Snapshots.First().Value;
						CurrentLocalTime = CurrentSnapshot.Time;
					}
				}
				else if (_mode != Mode.Init)
				{
					count++;

					float nextTime = CurrentLocalTime + timestep;

					float bufferedTime = SnapshotBuffer.Snapshots.Last().Value.Time - nextTime;

					bufferedTimeRunningAerage -= bufferedTimeRunningAerage / 120;
					bufferedTimeRunningAerage += bufferedTime / 120;

					_stats.add(bufferedTime);

					delay.Add(bufferedTime);
					mean.Add(_stats.getAverage());
					variance.Add(_stats.getVariance());
					stddev.Add(_stats.getStandardDeviation());

					if (delay.Count > 5000)
					{
						delay.RemoveRange(0, delay.Count - 5000);
						mean.RemoveRange(0, delay.Count - 5000);
						variance.RemoveRange(0, delay.Count - 5000);
						stddev.RemoveRange(0, delay.Count - 5000);
					}

					if (nextTime <= SnapshotBuffer.Snapshots.Last().Value.Time)
					{
						Snapshot_WIP prev, next;
						SnapshotBuffer.step(nextTime, out prev, out next);

						if (Math.Abs(next.Time - nextTime) < 0.001)
						{
							// if offset is less than a millisecond, just use 'next' snapshot time
							CurrentLocalTime = next.Time;
							CurrentSnapshot = next;
						}
						else if (prev != null)
						{
							if (Math.Abs(prev.Time - nextTime) < 0.001)
							{
								// if offset is less than a millisecond, just use 'prev' snapshot time
								CurrentLocalTime = prev.Time;
								CurrentSnapshot = prev;
							}
							else
							{
								float frac = (nextTime - prev.Time) / (next.Time - prev.Time);

								List<Snapshot_WIP.TransformInfo> transforms = new List<Snapshot_WIP.TransformInfo>(next.Transforms.Count);

								int prevIndex = 0;
								int nextIndex = 0;

								for (; nextIndex < next.Transforms.Count; nextIndex++)
								{
									// find corresponding transform in prev snapshot
									while (prevIndex < prev.Transforms.Count && prev.Transforms[prevIndex].Id.CompareTo(next.Transforms[nextIndex].Id) >= 0) prevIndex++;

									if (prevIndex < prev.Transforms.Count &&
										prev.Transforms[prevIndex].Id == next.Transforms[nextIndex].Id)
									{
										RigidBodyTransform t = new RigidBodyTransform();
										{
											t.Lerp(prev.Transforms[prevIndex].Transform, next.Transforms[nextIndex].Transform, frac);
										}

										transforms.Add(new Snapshot_WIP.TransformInfo(next.Transforms[nextIndex].Id, t));
									}
									else
									{
										transforms.Add(new Snapshot_WIP.TransformInfo(next.Transforms[nextIndex].Id, next.Transforms[nextIndex].Transform));
									}
								}

								// interpolated snapshot
								CurrentLocalTime = nextTime;
								CurrentSnapshot = new Snapshot_WIP(nextTime, transforms);
							}
						}
						else
						{
							// if prev snapshot is missing, just use current transforms with past timestamp
							CurrentLocalTime = nextTime;
							CurrentSnapshot = next;
						}

						_QoS = Math.Min(1.0f, _QoS + 1.0f / 120);
						output.Add(CurrentLocalTime);
					}
					else
					{
						_QoS -= 1.0f / 120;

						CurrentLocalTime += _QoS * timestep;
						CurrentSnapshot = new Snapshot_WIP(CurrentLocalTime, new List<Snapshot_WIP.TransformInfo>());
					}
				}
			}

			List<float> output = new List<float>();
			int count = 0;

			/// <summary>
			/// Source identifier.
			/// </summary>
			public Guid Id { get; private set; }

			/// <summary>
			/// This source snapshots.
			/// </summary>
			private SnapsotBuffer_WIP SnapshotBuffer = new SnapsotBuffer_WIP();


			private float _targetBufferSize = 2.0f;
			private float _averageBufferDelay = 0.0f;

			Mode _mode = Mode.Init;


			float CurrentLocalTime;

			public Snapshot_WIP CurrentSnapshot { get; private set; }
		}

		/// <summary>
		/// Add snapshot for specified source.
		/// Register source if entry does not exist.
		/// </summary>
		/// <param name="sourceId">Snapshot owner.</param>
		/// <param name="snapshot">List of transform at specified timestamp.</param>
		public void addSnapshot(Guid sourceId, Snapshot_WIP snapshot)
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
							new MultiSourceCombinedSnapshot.RigidBodyState(t.Id, source.CurrentSnapshot.Time, t.Transform));
					}
				}
			}

			return snapshot;
		}

		private Dictionary<Guid, SourceInfo> Sources = new Dictionary<Guid, SourceInfo>();
	}
}
