using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class SceneTable
    {
        public Dictionary<string, Scene> Data = new();
    }

    public class Scene
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SceneType { get; set; }
        public int SceneSubType { get; set; }
        public int ParentId { get; set; }
        public bool IsShereParentSceneData { get; set; }
        public int SceneResourceId { get; set; }
        public List<string> SceneUI { get; set; }
        public Vector2 MapSize { get; set; }
        public Vector2  MapOffset { get; set; }
        public List<List<int>> MapEntryCondition { get; set; }
        public List<string> AudioBank { get; set; }
        public int BornId { get; set; }
        public List<int> ReviveTableId { get; set; }
        public List<string> BGM { get; set; } // TODO: This may not be a string list
        public string LoadingBGM { get; set; }
        public float FallDis { get; set; }
        public int Weather { get; set; }
        public int DayAndNight { get; set; }
        public int CutsceneId { get; set; }
        public int MainUI { get; set; }
        public bool CanChangeLayer { get; set; }
        public List<int> PreloadCutscenes { get; set; }
        public List<int> PreloadEPFlows { get; set; }
        public int EPFlowId { get; set; }
        public int ShowMiniMap { get; set; }
        public int MiniMapRatio { get; set; }
        public string SubScene { get; set; }
        public string AmbEvent { get; set; }
        public string ReverEvent { get; set; }
        public int DefaultSceneArea { get; set; }
    }
}
