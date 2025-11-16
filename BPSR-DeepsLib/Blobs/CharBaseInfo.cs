using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class CharBaseInfo : BlobType
{
    public long? CharId;
    public string? AccountId;
    public long? ShowId;
    public uint? ServerId;
    public string? Name;
    public int? Gender;
    public float? X;
    public float? Y;
    public float? Z;
    public float? Dir;
    // FaceData
    public uint? CardId;
    public long? CreateTime;
    public long? OnlineTime;
    public long? OfflineTime;
    // ProfileInfo
    public CharTeam? TeamInfo;
    public ulong? CharState;
    // BodySize
    // UnionInfo
    // PersonalState
    // AvatarInfo
    public ulong? TotalOnlineTime;
    public string? OpenId;
    public int? SDKType;
    public int? OS;
    public int? InitProfessionId;
    public ulong? LastCalTotalTime;
    public int? AreaId;
    public string? ClientVersion;
    public int? FightPoint;
    public long? SumSave;
    public string? ClientResourceVersion;
    public long? LastOfflineTime;
    public int? DayAccDurTime;
    public long? LastAccDurTimestamp;
    public long? SaveSerial;

    public CharBaseInfo()
    {
    }

    public CharBaseInfo(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.CharBaseInfo.CharIdFieldNumber:
                CharId = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.AccountIdFieldNumber:
                AccountId = blob.ReadString();
                return true;
            case Zproto.CharBaseInfo.ShowIdFieldNumber:
                ShowId = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.ServerIdFieldNumber:
                ServerId = blob.ReadUInt();
                return true;
            case Zproto.CharBaseInfo.NameFieldNumber:
                Name = blob.ReadString();
                return true;
            case Zproto.CharBaseInfo.GenderFieldNumber:
                Gender = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.IsDeletedFieldNumber:
                // TODO: Implement blob.ReadBool();
                return false;
            case Zproto.CharBaseInfo.IsForbidFieldNumber:
                // TODO: Implement blob.ReadBool();
                return false;
            case Zproto.CharBaseInfo.IsMuteFieldNumber:
                // TODO: Implement blob.ReadBool();
                return false;
            case Zproto.CharBaseInfo.XFieldNumber:
                X = blob.ReadFloat();
                return true;
            case Zproto.CharBaseInfo.YFieldNumber:
                Y = blob.ReadFloat();
                return true;
            case Zproto.CharBaseInfo.ZFieldNumber:
                Z = blob.ReadFloat();
                return true;
            case Zproto.CharBaseInfo.DirFieldNumber:
                Dir = blob.ReadFloat();
                return true;
            case Zproto.CharBaseInfo.FaceDataFieldNumber:
                // TODO: Implement FaceData
                return false;
            case Zproto.CharBaseInfo.CardIdFieldNumber:
                CardId = blob.ReadUInt();
                return true;
            case Zproto.CharBaseInfo.CreateTimeFieldNumber:
                CreateTime = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.OnlineTimeFieldNumber:
                OnlineTime = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.OfflineTimeFieldNumber:
                OfflineTime = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.ProfileInfoFieldNumber:
                // TODO: Implement ProfileInfo
                return false;
            case Zproto.CharBaseInfo.TeamInfoFieldNumber:
                TeamInfo = new(blob);
                return true;
            case Zproto.CharBaseInfo.CharStateFieldNumber:
                CharState = blob.ReadULong();
                return true;
            case Zproto.CharBaseInfo.BodySizeFieldNumber:
                // TODO: Implement BodySize
                return false;
            case Zproto.CharBaseInfo.UnionInfoFieldNumber:
                // TODO: Implement UnionInfo
                return false;
            case Zproto.CharBaseInfo.PersonalStateFieldNumber:
                // TODO: Implement PersonalState
                return false;
            case Zproto.CharBaseInfo.AvatarInfoFieldNumber:
                // TODO: Implement AvatarInfo
                return false;
            case Zproto.CharBaseInfo.TotalOnlineTimeFieldNumber:
                TotalOnlineTime = blob.ReadULong();
                return true;
            case Zproto.CharBaseInfo.OpenIdFieldNumber:
                OpenId = blob.ReadString();
                return true;
            case Zproto.CharBaseInfo.SdkTypeFieldNumber:
                SDKType = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.OsFieldNumber:
                OS = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.InitProfessionIdFieldNumber:
                InitProfessionId = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.LastCalTotalTimeFieldNumber:
                LastCalTotalTime = blob.ReadULong();
                return true;
            case Zproto.CharBaseInfo.AreaIdFieldNumber:
                AreaId = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.ClientVersionFieldNumber:
                ClientVersion = blob.ReadString();
                return true;
            case Zproto.CharBaseInfo.FightPointFieldNumber:
                FightPoint = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.SumSaveFieldNumber:
                SumSave = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.ClientResourceVersionFieldNumber:
                ClientResourceVersion = blob.ReadString();
                return true;
            case Zproto.CharBaseInfo.LastOfflineTimeFieldNumber:
                LastOfflineTime = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.DayAccDurTimeFieldNumber:
                DayAccDurTime = blob.ReadInt();
                return true;
            case Zproto.CharBaseInfo.LastAccDurTimestampFieldNumber:
                LastAccDurTimestamp = blob.ReadLong();
                return true;
            case Zproto.CharBaseInfo.SaveSerialFieldNumber:
                SaveSerial = blob.ReadLong();
                return true;
            default:
                return false;
        }
    }
}
