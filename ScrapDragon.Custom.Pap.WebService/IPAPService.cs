using System.ServiceModel;
using ScrapDragon.Custom.Pap.WebService.RnR;

namespace ScrapDragon.Custom.Pap.WebService
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