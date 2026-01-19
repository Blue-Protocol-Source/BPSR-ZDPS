using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class CharSerialize(BlobReader blob) : BlobType(ref blob)
{
    public int? CharId;
    public CharBaseInfo? CharBaseInfo;
    public SceneData? SceneData;
    // ...
    public UserFightAttr? Attr;
    // ...
    public SeasonRankList? SeasonRankList;
    // ...
    public ProfessionList? ProfessionList;
    // ...
    public FightPoint? FightPoint;
    // ...

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.CharSerialize.CharIdFieldNumber:
                CharId = blob.ReadInt();
                return true;
            case Zproto.CharSerialize.CharBaseFieldNumber:
                CharBaseInfo = new(blob);
                return true;
            case Zproto.CharSerialize.SceneDataFieldNumber:
                SceneData = new(blob);
                return true;
            case Zproto.CharSerialize.AttrFieldNumber:
                Attr = new(blob);
                return true;
            case Zproto.CharSerialize.SeasonRankListFieldNumber:
                SeasonRankList = new(blob);
                return true;
            case Zproto.CharSerialize.ProfessionListFieldNumber:
                ProfessionList = new(blob);
                return true;
            case Zproto.CharSerialize.FightPointFieldNumber:
                FightPoint = new(blob);
                return true;
            default:
                return false;
        }
    }
}