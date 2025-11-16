using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class TeamMemberSocialData : BlobType
{
    public ProfessionData? ProfessionData;
    public EquipData? EquipData;

    public TeamMemberSocialData()
    {
    }

    public TeamMemberSocialData(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.TeamMemberSocialData.ProfessionDataFieldNumber:
                ProfessionData = new(blob);
                return true;
            case Zproto.TeamMemberSocialData.EquipDataFieldNumber:
                EquipData = new(blob);
                return true;
            default:
                return false;
        }
    }
}
