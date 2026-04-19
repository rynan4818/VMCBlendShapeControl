namespace VMCBlendShapeControl.Configuration
{
    public class VmcBlendShapeActionConfig
    {
        public virtual string BlendShape { get; set; } = "";
        public virtual float Value { get; set; } = 1f;
        public virtual float DurationSec { get; set; } = 0f;
        public virtual float TransitionSpeed { get; set; } = -1f;
        public virtual float BackToNeutralDelaySec { get; set; } = 0f;
        public virtual bool NoBlink { get; set; } = false;
        public virtual bool StopLipSync { get; set; } = false;

        public VmcBlendShapeActionConfig Clone()
        {
            return new VmcBlendShapeActionConfig
            {
                BlendShape = BlendShape,
                Value = Value,
                DurationSec = DurationSec,
                TransitionSpeed = TransitionSpeed,
                BackToNeutralDelaySec = BackToNeutralDelaySec,
                NoBlink = NoBlink,
                StopLipSync = StopLipSync
            };
        }
    }
}
