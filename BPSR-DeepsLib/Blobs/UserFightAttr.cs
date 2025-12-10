using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_DeepsLib.Blobs;

public class UserFightAttr : BlobType
{
    public long? CurHp;
    public long? MaxHp;
    public float? OriginEnergy;
    public List<uint>? ResourceIds;
    public List<uint>? Resources;
    public int? IsDead;
    public long? DeadTime;
    public int? ReviveId;
    public SkillCDInfo? CDInfo;

    public UserFightAttr()
    {
    }

    public UserFightAttr(BlobReader blob) : base(ref blob)
    {
    }

    public override bool ParseField(int index, ref BlobReader blob)
    {
        switch (index)
        {
            case Zproto.UserFightAttr.CurHpFieldNumber:
                CurHp = blob.ReadLong();
                return true;
            case Zproto.UserFightAttr.MaxHpFieldNumber:
                MaxHp = blob.ReadLong();
                return true;
            case Zproto.UserFightAttr.OriginEnergyFieldNumber:
                OriginEnergy = blob.ReadFloat();
                return true;
            case Zproto.UserFightAttr.ResourceIdsFieldNumber:
                ResourceIds = blob.ReadList<uint>();
                return true;
            case Zproto.UserFightAttr.ResourcesFieldNumber:
                Resources = blob.ReadList<uint>();
                return true;
            case Zproto.UserFightAttr.IsDeadFieldNumber:
                IsDead = blob.ReadInt();
                return true;
            case Zproto.UserFightAttr.DeadTimeFieldNumber:
                DeadTime = blob.ReadLong();
                return true;
            case Zproto.UserFightAttr.ReviveIdFieldNumber:
                ReviveId = blob.ReadInt();
                return true;
            case Zproto.UserFightAttr.CdInfoFieldNumber:
                CDInfo = new(blob);
                return true;
            default:
                return false;
        }
    }
}
