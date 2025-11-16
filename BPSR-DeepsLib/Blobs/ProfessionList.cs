using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class ProfessionList : BlobType
{
    public int? CurProfessionId;
    public List<int>? CurAssistProfessions;
    // ProfessionInfo
    // AoyiSkillInfoMap
    public uint? TotalTalentPoints;
    public uint? TotalTalentResetCount;
    // TalentList

    public ProfessionList()
    {
    }

    public ProfessionList(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.ProfessionList.CurProfessionIdFieldNumber:
                CurProfessionId = blob.ReadInt();
                return true;
            case Zproto.ProfessionList.CurAssistProfessionsFieldNumber:
                CurAssistProfessions = blob.ReadList<int>();
                return true;
            case Zproto.ProfessionList.TotalTalentPointsFieldNumber:
                TotalTalentPoints = blob.ReadUInt();
                return true;
            case Zproto.ProfessionList.TotalTalentResetCountFieldNumber:
                TotalTalentResetCount = blob.ReadUInt();
                return true;
            default:
                return false;
        }
    }
}
