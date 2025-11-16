using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class ProfessionData : BlobType
{
    public int? ProfessionId;
    public int? WeaponSkin;

    public ProfessionData()
    {
    }

    public ProfessionData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.ProfessionData.ProfessionIdFieldNumber:
                ProfessionId = blob.ReadInt();
                return true;
            case Zproto.ProfessionData.WeaponSkinFieldNumber:
                WeaponSkin = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}
