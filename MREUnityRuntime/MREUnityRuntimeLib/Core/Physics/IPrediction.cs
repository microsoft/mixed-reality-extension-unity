using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixedRealityExtension.Core;

namespace MixedRealityExtension.Core.Physics
{
	/// interface struct to pass time step informations
	public class PredictionTimeParameters
	{
		public PredictionTimeParameters(float timeStep)
		{
			setFromDT(timeStep);
		}

		/// current time step of the frame
		public float DT = 0.0f;
		/// half of the time step for time tolerance 
		public float halfDT = 0.0f;
		/// to avoid divisions the inverse of the delta time of this frame
		public float invDT = 0.0f;
		/// method to set all the fields from DT;
		/// <param name="timeStep"> time step of the current frame </param> 
		public void setFromDT(float timeStep)
		{
			DT = timeStep;
			halfDT = 0.5f * DT;
			invDT = (1.0f / DT);
		}
	}

	/// general interface to predict the remote bodies over multiple frames 
	public interface IPrediction
	{
		/// this signals to the interface that we will now start streaming into the prediction
		/// the remote and owned body pairs for the current frame
		void StartBodyPredicitonForNextFrame();

		/// before a remote body gets stepped this needs to be called for all potentially
		/// predicted remote bodies for one frame
		void AddAndProcessRemoteBodyForPrediction(RigidBodyPhysicsBridgeInfo rb,
			RigidBodyTransform transform, UnityEngine.Vector3 keyFramedPos,
			UnityEngine.Quaternion keyFramedOrientation, float timeOfSnapshot,
			PredictionTimeParameters timeInfo);

		/// In the last step within the frame the owned bodies are added to the prediction
		void PredictAllRemoteBodiesWithOwnedBodies(ref SortedList<Guid, RigidBodyPhysicsBridgeInfo> allRigidBodiesOfThePhysicsBridge,
			PredictionTimeParameters timeInfo);

		/// reset internal state
		void Clear();
	}
}
