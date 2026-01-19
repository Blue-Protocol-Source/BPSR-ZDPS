using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class SocialData : BlobType
{
    public long? CharId;
    public string? AccountId;
    public BasicData? BasicData;
    public ProfessionData? ProfessionData;
    public EquipData? EquipData;
    public SceneData? SceneData;
    public CharTeam? TeamData;
    public SeasonRankData? SeasonRank;

    public SocialData()
    {
    }

    public SocialData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.SocialData.CharIdFieldNumber:
                CharId = blob.ReadLong();
                return true;
            case Zproto.SocialData.AccountIdFieldNumber:
                AccountId = blob.ReadString();
                return true;
            case Zproto.SocialData.BasicDataFieldNumber:
                BasicData = new(blob);
                return true;
            case Zproto.SocialData.ProfessionDataFieldNumber:
                ProfessionData = new(blob);
                return true;
            case Zproto.SocialData.EquipDataFieldNumber:
                EquipData = new(blob);
                return true;
            case Zproto.SocialData.SceneDataFieldNumber:
                SceneData = new(blob);
                return true;
            case Zproto.SocialData.TeamDataFieldNumber:
                TeamData = new(blob);
                return true;
            case Zproto.SocialData.SeasonRankFieldNumber:
                SeasonRank = new(blob);
                return true;
            default:
                return false;
        }
    }
}
