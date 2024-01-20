using ProtoBuf;

namespace AnanaceDev.AnalogGridControl.Network
{

  [ProtoContract]
  public class AnalogAvailabilityResponse : AnalogGridControlPacket
  {
    [ProtoMember(0, IsRequired = true)]
    public uint Version { get; set; }
  };

}
