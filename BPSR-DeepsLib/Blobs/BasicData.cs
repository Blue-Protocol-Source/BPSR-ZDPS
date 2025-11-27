using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class BasicData : BlobType
{
    public long? CharId;
    public long? ShowId;
    public string Name;
    public int? Gender;
    public int? BodySize;
    public int? Level;
    public int? SceneId;
    public List<int>? PersonalState;
    public long? OfflineTime;
    public string SceneGuid;
    public long? CreateTime;
    public uint? CurTalentPoolId;
    public uint? BotAiId;
    public int? RegisterChannel;
    public ulong? CharState;
    public long? OnlineTime;
    public long? SumSaveDiamond;
    public bool? IsNewbie;

    public BasicData()
    {
    }

    public BasicData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.BasicData.CharIdFieldNumber:
                CharId = blob.ReadLong();
                return true;
            case Zproto.BasicData.ShowIdFieldNumber:
                ShowId = blob.ReadLong();
                return true;
            case Zproto.BasicData.NameFieldNumber:
                Name = blob.ReadString();
                return true;
            case Zproto.BasicData.GenderFieldNumber:
                Gender = blob.ReadInt();
                return true;
            case Zproto.BasicData.BodySizeFieldNumber:
                BodySize = blob.ReadInt();
                return true;
            case Zproto.BasicData.LevelFieldNumber:
                Level = blob.ReadInt();
                return true;
            case Zproto.BasicData.SceneIdFieldNumber:
                SceneId = blob.ReadInt();
                return true;
            case Zproto.BasicData.PersonalStateFieldNumber:
                PersonalState = blob.ReadList<int>();
                return true;
            case Zproto.BasicData.OfflineTimeFieldNumber:
                OfflineTime = blob.ReadLong();
                return true;
            case Zproto.BasicData.SceneGuidFieldNumber:
                SceneGuid = blob.ReadString();
                return true;
            case Zproto.BasicData.CreateTimeFieldNumber:
                CreateTime = blob.ReadLong();
                return true;
            case Zproto.BasicData.CurTalentPoolIdFieldNumber:
                CurTalentPoolId = blob.ReadUInt();
                return true;
            case Zproto.BasicData.BotAiIdFieldNumber:
                BotAiId = blob.ReadUInt();
                return true;
            case Zproto.BasicData.RegisterChannelFieldNumber:
                RegisterChannel = blob.ReadInt();
                return true;
            case Zproto.BasicData.CharStateFieldNumber:
                CharState = blob.ReadULong();
                return true;
            case Zproto.BasicData.OnlineTimeFieldNumber:
                OnlineTime = blob.ReadLong();
                return true;
            case Zproto.BasicData.SumSaveDiamondFieldNumber:
                SumSaveDiamond = blob.ReadLong();
                return true;
            default:
                return false;
        }
    }
}
