using System;
using System.Runtime.Serialization;

namespace ScrapDragon.Custom.PAP.RnR
{
    [DataContract]
    public class ReceivePapDataRequest
    {
        public ReceivePapDataRequest()
        {
            YardId = Guid.Empty;
        }
        [DataMember(IsRequired = true)]
        public Guid YardId { get; set; }
    }

    [DataContract]
    public class ReceivePapDataResponse
    {
        public ReceivePapDataResponse()
        {
            Success = false;
            FailureInformation = string.Empty;
            Data = string.Empty;
        }
        [DataMember(IsRequired = true)]
        public string Data { get; set; }
        [DataMember(IsRequired = true)]
        public bool Success { get; set; }
        [DataMember(IsRequired = true)]
        public string FailureInformation { get; set; }

    }
}
