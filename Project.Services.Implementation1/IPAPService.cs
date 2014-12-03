using System.ServiceModel;
using ScrapDragon.Custom.PAP.RnR;

namespace ScrapDragon.Custom.PAP
{

    [ServiceContract]
    public interface IPAPService
    {
        [OperationContract]
        string TestConnection(string name);

        [OperationContract]
        ReceivePapDataResponse ReceivePapData(ReceivePapDataRequest request);

        [OperationContract]
        SendPapDataResponse SendPapData(SendPapDataRequest request);
    }
}