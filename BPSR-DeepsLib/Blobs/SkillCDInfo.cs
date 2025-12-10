using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class SkillCDInfo : BlobType
{
    public int? SkillLevelId;
    public long? SkillBeginTime;
    public int? Duration;
    public uint? SkillCdType;
    public long? ProfessionHoldBeginTime;
    public int? ChargeCount;
    public int? ValidCdTime;
    public int? SubCdRatio;
    public long? SubCdFixed;
    public int? AccelerateCdRatio;

    public SkillCDInfo()
    {
    }

    public SkillCDInfo(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.SkillCDInfo.SkillLevelIdFieldNumber:
                SkillLevelId = blob.ReadInt();
                return true;
            case Zproto.SkillCDInfo.SkillBeginTimeFieldNumber:
                SkillBeginTime = blob.ReadLong();
                return true;
            case Zproto.SkillCDInfo.DurationFieldNumber:
                Duration = blob.ReadInt();
                return true;
            case Zproto.SkillCDInfo.SkillCdTypeFieldNumber:
                SkillCdType = blob.ReadUInt();
                return true;
            case Zproto.SkillCDInfo.ProfessionHoldBeginTimeFieldNumber:
                ProfessionHoldBeginTime = blob.ReadLong();
                return true;
            case Zproto.SkillCDInfo.ChargeCountFieldNumber:
                ChargeCount = blob.ReadInt();
                return true;
            case Zproto.SkillCDInfo.ValidCdTimeFieldNumber:
                ValidCdTime = blob.ReadInt();
                return true;
            case Zproto.SkillCDInfo.SubCdRatioFieldNumber:
                SubCdRatio = blob.ReadInt();
                return true;
            case Zproto.SkillCDInfo.SubCdFixedFieldNumber:
                SubCdFixed = blob.ReadLong();
                return true;
            case Zproto.SkillCDInfo.AccelerateCdRatioFieldNumber:
                AccelerateCdRatio = blob.ReadInt();
                return true;
            default:
                return false;
        }
    }
}