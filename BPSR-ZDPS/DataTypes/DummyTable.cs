using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class DummyTable
    {
        public Dictionary<string, Dummy> Data = new();
    }

    public class Dummy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SkillId { get; set; }
        public float WalkSpeed { get; set; }
        public float RunSpeed { get; set; }
        public List<int> SkillIds { get; set; }
        public int AITableReference { get; set; }
        public bool IsInitiative { get; set; }
        public bool IsFlying { get; set; }
        public string BirthEffect { get; set; }
        public string DeadEffect { get; set; }
        public string BirthAudio { get; set; }
        public string DeadAudio { get; set; }
        public List<int> Tags { get; set; }
        public bool IsNotGround { get; set; }
        public bool IsStatic { get; set; }
        public int SummonUpperLimit { get; set; }
        //public List<object> StatusInfo { get; set; }
        //public List<object> StatusTransition { get; set; }
        public int DefaultState { get; set; }
        //public List<object> InteractionTemplate { get; set; }
        //public List<object> InteractionMount { get; set; }
        //public List<object> ShowStatusInfo { get; set; }
        //public List<object> ShowStatusTransition { get; set; }
    }
}
