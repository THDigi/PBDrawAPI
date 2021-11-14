using System.Collections.Generic;
using VRage.Game;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDrawAPI
{
    public class PBDrawHandler
    {
        public int NextId;

        //public readonly IMyPB PB;
        public readonly Dictionary<int, DrawObject> DrawObjects = new Dictionary<int, DrawObject>();

        static readonly List<int> RemoveIds = new List<int>();

        public PBDrawHandler(IMyProgrammableBlock pb)
        {
            //PB = pb;
        }

        public bool Draw()
        {
            bool paused = MyParticlesManager.Paused;

            foreach(KeyValuePair<int, DrawObject> kv in DrawObjects)
            {
                DrawObject drawObj = kv.Value;

                drawObj.Draw();

                if(!paused && drawObj.TicksToLive >= 0 && --drawObj.TicksToLive <= 0)
                {
                    RemoveIds.Add(kv.Key);
                }
            }

            if(RemoveIds.Count > 0)
            {
                foreach(int id in RemoveIds)
                {
                    DrawObjects.Remove(id);
                }

                RemoveIds.Clear();
            }

            return true;
        }
    }
}