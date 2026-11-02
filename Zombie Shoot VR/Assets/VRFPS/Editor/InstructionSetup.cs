namespace VRFPS.Editor
{
    public static class InstructionSetup
    {
        public static void Configure()
        {
            MenuSetup.Configure();
            BootstrapSetup.Configure();
            UnityEngine.Debug.Log("VRFPS_INSTRUCTIONS_CONFIGURED");
        }
        public static void Build()
        {
            BootstrapSetup.Build();
            WaveLabSetup.Build();
        }
    }
}
