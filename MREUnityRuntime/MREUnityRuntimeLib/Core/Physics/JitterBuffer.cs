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
	public class Snapshot
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

			/// <todo>
			/// use int as transform id
			/// </todo>
			public Guid Id { get; private set; }

			public RigidBodyTransform Transform { get; private set; }
		}

		public Snapshot(float time, List<TransformInfo> transforms)
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
	public class SnapsotBuffer
	{
		/// <summary>
		/// Add snapshot to buffer if snapshot with same timestamp exists in buffer.
		/// </summary>
		public void addSnapshot(Snapshot snapshot)
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

		/// <summary>
		/// Snapshots sorted by snapshot timestamp.
		/// </summary>
		public SortedList<float, Snapshot> Snapshots = new SortedList<float, Snapshot>();
	}

	public class MultiSourceCombinedSnapshot
	{
		public struct RigidBodyState
		{
			public RigidBodyState(Guid id, float time, RigidBodyTransform transform, bool hasUpdate)
			{
				Id = id;
				LocalTime = time;
				Transform = transform;
				HasUpdate = hasUpdate;
			}

			public Guid Id;

			public float LocalTime;

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

				public void shift(float time)
				{
					average += time;
				}

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

			int interpolate = 0;
			int predict = 0;

			private RunningStats _stats = new RunningStats();

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

			List<float> delay = new List<float>(5000);
			List<float> mean = new List<float>(5000);
			List<float> variance = new List<float>(5000);
			List<float> stddev = new List<float>(5000);
			List<float> target = new List<float>(5000);
			List<float> biasedTarget = new List<float>(5000);

			float bufferedTimeRunningAerage = 0.0f;

			public void step(float timestep)
			{
				if (_mode == Mode.Init)
				{
					if (SnapshotBuffer.Snapshots.Count >= 4)
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


					float targetValue = _stats.getAverage() - _stats.getStandardDeviation();

					target.Add(targetValue);

					const float dt = 1.0f / 60;
					float biasedTargetValue = targetValue / dt;

					if (biasedTargetValue > 0)
					{
						biasedTargetValue = ((int)biasedTargetValue) * dt;
					}
					else
					{
						biasedTargetValue = ((int)biasedTargetValue-1) * dt;
					}

					biasedTarget.Add(biasedTargetValue);

					if (delay.Count >= 5000)
					{
						delay.RemoveRange(0, delay.Count - 5000);
						mean.RemoveRange(0, delay.Count - 5000);
						variance.RemoveRange(0, delay.Count - 5000);
						stddev.RemoveRange(0, delay.Count - 5000);
						target.RemoveRange(0, delay.Count - 5000);
						biasedTarget.RemoveRange(0, delay.Count - 5000);
					}

					float biasedTargetDiff = /*bufferedTime -*/ biasedTargetValue;

					// check if time shift is required
					if (Math.Abs(biasedTargetDiff) > 0.001)
					{
						float shift = Math.Min(0.5f * timestep, 0.2f * Math.Abs(biasedTargetDiff));

						if (biasedTargetDiff > 0)
						{
							nextTime += shift;

							_stats.shift(-shift);
						}
						else
						{
							nextTime -= shift;

							_stats.shift(+shift);
						}
					}

					if (nextTime <= SnapshotBuffer.Snapshots.Last().Value.Time)
					{
						interpolate++;

						HasUpdate = true;

						Snapshot prev, next;
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

								List<Snapshot.TransformInfo> transforms = new List<Snapshot.TransformInfo>(next.Transforms.Count);

								int prevIndex = 0;
								int nextIndex = 0;

								for (; nextIndex < next.Transforms.Count; nextIndex++)
								{
									// find corresponding transform in prev snapshot
									while (prevIndex < prev.Transforms.Count && prev.Transforms[prevIndex].Id.CompareTo(next.Transforms[nextIndex].Id) < 0) prevIndex++;

									if (prevIndex < prev.Transforms.Count &&
										prev.Transforms[prevIndex].Id == next.Transforms[nextIndex].Id)
									{
										RigidBodyTransform t = new RigidBodyTransform();
										{
											t.Lerp(prev.Transforms[prevIndex].Transform, next.Transforms[nextIndex].Transform, frac);
										}

										transforms.Add(new Snapshot.TransformInfo(next.Transforms[nextIndex].Id, t));
									}
									else
									{
										transforms.Add(new Snapshot.TransformInfo(next.Transforms[nextIndex].Id, next.Transforms[nextIndex].Transform));
									}
								}

								// interpolated snapshot
								CurrentLocalTime = nextTime;
								CurrentSnapshot = new Snapshot(nextTime, transforms);
							}
						}
						else
						{
							// if prev snapshot is missing, just use current transforms with past timestamp
							CurrentLocalTime = nextTime;
							CurrentSnapshot = next;
						}

						HasUpdate = true;
					}
					else
					{
						predict++;

						CurrentLocalTime = nextTime;
						CurrentSnapshot = new Snapshot(CurrentLocalTime, CurrentSnapshot.Transforms);

						HasUpdate = false;
					}

					output.Add(CurrentLocalTime);
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
			private SnapsotBuffer SnapshotBuffer = new SnapsotBuffer();


			private float _targetBufferSize = 2.0f;
			private float _averageBufferDelay = 0.0f;

			Mode _mode = Mode.Init;

			public bool HasUpdate = false;

			float CurrentLocalTime;

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
							new MultiSourceCombinedSnapshot.RigidBodyState(t.Id, source.CurrentSnapshot.Time, t.Transform, source.HasUpdate));
					}
				}
			}

			return snapshot;
		}

		private Dictionary<Guid, SourceInfo> Sources = new Dictionary<Guid, SourceInfo>();
	}
}
