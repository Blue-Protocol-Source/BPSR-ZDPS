using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class FightPointData : BlobType
{
    public int? FunctionType;
    public int? TotalPoint;
    public int? Point;
    public Dictionary<int, FightPointSubData>? SubFunctionData;

    public FightPointData()
    {
    }

    public FightPointData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.FightPointData.FunctionTypeFieldNumber:
                FunctionType = blob.ReadInt();
                return true;
            case Zproto.FightPointData.TotalPointFieldNumber:
                TotalPoint = blob.ReadInt();
                return true;
            case Zproto.FightPointData.PointFieldNumber:
                Point = blob.ReadInt();
                return true;
            case Zproto.FightPointData.SubFunctionDataFieldNumber:
                SubFunctionData = blob.ReadHashMap<int, FightPointSubData>();
                return true;
            default:
                return false;
        }
    }
}