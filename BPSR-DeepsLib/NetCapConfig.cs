namespace BPSR_DeepsLib;

public class NetCapConfig
{
    public string CaptureDeviceName { get; set; } = string.Empty;
    public string[] ExeNames { get; set; } = ["BPSR", "BPSR_STEAM"];
    public TimeSpan ConnectionScanInterval { get; set; } = TimeSpan.FromSeconds(10);
}