namespace BPSR_ZDPSLib.ServiceMethods;

public enum GrpcCharactorNtf
{
    Login = 0x1, // 1
    CreateChar = 0x2, // 2
    SelectChar = 0x3, // 3
    DeleteChar = 0x4, // 4
    Reconnect = 0x5, // 5
    ExitGame = 0x6, // 6
    ReportMSdk = 0xA, // 10
    GetFaceUpToken = 0x11, // 17
    UploadFaceSuccess = 0x12, // 18
    GetFaceUploadData = 0x13, // 19
    GetFaceDataUrl = 0x14, // 20
    CancelDeleteChar = 0x16, // 22
    PrivilegeActivate = 0x17, // 23
    TakeAwardByCdKey = 0x19, // 25
    SyncLanguage = 0x1D, // 29
}