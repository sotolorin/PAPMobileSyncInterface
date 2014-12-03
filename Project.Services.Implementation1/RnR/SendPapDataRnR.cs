using System.Runtime.Serialization;

namespace ScrapDragon.Custom.PAP.RnR
{
    [DataContract]
    public class SendPapDataRequest
    {
        public SendPapDataRequest()
        {
            Data = string.Empty;
        }

        [DataMember(IsRequired = true)]
        public string Data { get; set; }
    }

    [DataContract]
    public class SendPapDataResponse
    {
        public SendPapDataResponse()
        {
            Success = false;
            FailureInformation = string.Empty;
        }

        [DataMember(IsRequired = true)]
        public bool Success { get; set; }
        [DataMember(IsRequired = true)]
        public string FailureInformation { get; set; }
    }
}
