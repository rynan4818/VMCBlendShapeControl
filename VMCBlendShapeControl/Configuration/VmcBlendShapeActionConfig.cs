namespace VMCBlendShapeControl.Configuration
{
    public class VmcBlendShapeActionConfig
    {
        public virtual string BlendShape { get; set; } = "";
        public virtual float Value { get; set; } = 1f;
        public virtual float Duration { get; set; } = 0.8f;
        public virtual float Transition { get; set; } = 0.1f;

        public VmcBlendShapeActionConfig Clone()
        {
            return new VmcBlendShapeActionConfig
            {
                BlendShape = BlendShape,
                Value = Value,
                Duration = Duration,
                Transition = Transition
            };
        }
    }
}
