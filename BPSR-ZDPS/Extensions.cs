using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this Zproto.Vec3 vec3)
        {
            return new Vector3(vec3.X, vec3.Y, vec3.Z);
        }

        public static Zproto.Vec3 ToVec3(this System.Numerics.Vector3 vector3)
        {
            var vec3 = new Zproto.Vec3()
            {
                X = vector3.X,
                Y = vector3.Y,
                Z = vector3.Z
            };
            return vec3;
        }

        public static DataTypes.Skills.SkillLevelInfo ToSkillLevelInfo(this Zproto.SkillLevelInfo skillLevelInfo)
        {
            var data = new DataTypes.Skills.SkillLevelInfo()
            {
                SkillId = skillLevelInfo.SkillId,
                CurrentLevel = skillLevelInfo.CurrentLevel,
                Tier = skillLevelInfo.RemodelLevel
            };

            return data;
        }
    }
}
