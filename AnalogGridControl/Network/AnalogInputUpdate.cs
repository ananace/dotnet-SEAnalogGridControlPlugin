using ProtoBuf;

namespace AnanaceDev.AnalogGridControl.Network
{

  [ProtoContract]
  public class AnalogInputUpdate : AnalogGridControlPacket
  {
    [ProtoMember(0, IsRequired = true)]
    public long GridId { get; set; }
    [ProtoMember(1, IsRequired = true)]
    public float BrakeForce { get; set; }
  };

}
