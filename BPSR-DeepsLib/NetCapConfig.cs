namespace BPSR_DeepsLib;

public class NetCapConfig
{
    public string CaptureDeviceName { get; set; } = string.Empty;
    public string ExeName { get; set; } = "BPSR";
    public TimeSpan ConnectionScanInterval { get; set; } = TimeSpan.FromSeconds(10);
}