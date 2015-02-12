using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;

namespace ParticleTracking
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IParticleTrackingService" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IParticleTrackingServiceCallback))]
    public interface IParticleTrackingService
    {
        [OperationContract]
        void Init(string localServer,
            string localDatabase,
            short datasetID,
            string tableName,
            int atomDim,
            int spatialInterp,
            bool development);

        [OperationContract(IsOneWay = true)]
        void Finish();

        [OperationContract(IsOneWay = true)]
        void DoParticleTrackingWork(List<SQLUtility.TrackingInputRequest> particles);

        [OperationContract(IsOneWay = true)]
        void DoParticleTrackingWorkOneParticle(SQLUtility.TrackingInputRequest one_particle);
    }

    public interface IParticleTrackingServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void DoneParticles(List<SQLUtility.TrackingInputRequest> particles);
    }
}
