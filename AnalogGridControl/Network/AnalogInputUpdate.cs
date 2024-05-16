using ProtoBuf;

namespace AnanaceDev.AnalogGridControl.Network
{

  [ProtoContract]
  public class AnalogInputUpdate : AnalogGridControlPacket
  {
    [ProtoMember(0, IsRequired = true)]
    public long GridId { get; set; }
    [ProtoMember(1, IsRequired = false)]
    public float? BrakeForce { get; set; } = null;
    [ProtoMember(2, IsRequired = false)]
    public float? AccelForce { get; set; } = null;
  };

}
