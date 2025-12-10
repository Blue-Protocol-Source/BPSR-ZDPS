using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class FightPointSubData : BlobType
{
    public int? FunctionType;
    public int? RootFunctionType;
    public int? Point;

    public FightPointSubData()
    {
    }

    public FightPointSubData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.FightPointSubData.FunctionTypeFieldNumber:
                FunctionType = blob.ReadInt();
                return true;
            case Zproto.FightPointSubData.RootFunctionTypeFieldNumber:
                RootFunctionType = blob.ReadInt();
                return true;
            case Zproto.FightPointSubData.PointFieldNumber:
                Point = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}