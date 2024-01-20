using ProtoBuf;

namespace AnanaceDev.AnalogGridControl.Network
{

  [ProtoContract]
  [ProtoInclude(5, typeof(AnalogAvailabilityRequest))]
  [ProtoInclude(6, typeof(AnalogAvailabilityResponse))]
  [ProtoInclude(7, typeof(AnalogInputUpdate))]
  public class AnalogGridControlPacket
  {
  };

}
