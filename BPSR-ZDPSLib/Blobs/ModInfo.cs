using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPSLib.Blobs;

public class ModInfo : BlobType
{
    public List<int>? PartIds;
    //public List<ModPartUpgradeRecord>? UpgradeRecords;
    public int? SuccessRate;
    public List<int>? InitLinkNums;

    public ModInfo()
    {
    }

    public ModInfo(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.ModInfo.PartIdsFieldNumber:
                PartIds = blob.ReadList<int>();
                return true;
            case Zproto.ModInfo.UpgradeRecordsFieldNumber:

                return false;
            case Zproto.ModInfo.SuccessRateFieldNumber:
                SuccessRate = blob.ReadInt();
                return true;
            case Zproto.ModInfo.InitLinkNumsFieldNumber:
                InitLinkNums = blob.ReadList<int>();
                return true;
            default:
                return false;
        }
    }
}
