using System;
using System.Collections.Generic;
using VRage.Game;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDebugAPI
{
    // TODO: rename file same as the class after commit
    public class DebugObjectHost
    {
        // Important for IDs to start from 1 to avoid default values being used to remove, such as HudNotification(ref id) where id is just a default value field.
        public const int StartId = 1;

        int NextId = StartId;
        public readonly Dictionary<int, DebugObjectBase> Objects = new Dictionary<int, DebugObjectBase>();

        static readonly List<int> RemoveIds = new List<int>();

        public DebugObjectHost(IMyProgrammableBlock pb)
        {
        }

        public int Add(DebugObjectBase obj)
        {
            int id = NextId;
            Objects.Add(id, obj);

            NextId++;
            if(NextId == int.MaxValue)
                throw new Exception($"Object id overflow, you really already added {int.MaxValue} objects for one PB ?!");

            return id;
        }

        public void Remove(int id)
        {
            DebugObjectBase drawObject;
            if(Objects.TryGetValue(id, out drawObject))
            {
                Objects.Remove(id);
                drawObject.Dispose();
            }
        }

        public void RemoveAll()
        {
            try
            {
                foreach(DebugObjectBase drawObject in Objects.Values)
                {
                    drawObject.Dispose();
                }
            }
            finally
            {
                Objects.Clear();

                // probably more reliable if it doesn't reset...
                //NextId = StartId;
            }
        }

        public void RemoveTypes<T>() where T : class
        {
            RemoveIds.Clear();

            foreach(KeyValuePair<int, DebugObjectBase> kv in Objects)
            {
                DebugObjectBase drawObject = kv.Value;
                if(drawObject is T)
                {
                    drawObject.Dispose();
                    RemoveIds.Add(kv.Key);
                }
            }

            foreach(int id in RemoveIds)
            {
                Objects.Remove(id);
            }

            RemoveIds.Clear();
        }

        public bool Update()
        {
            bool paused = MyParticlesManager.Paused;

            foreach(KeyValuePair<int, DebugObjectBase> kv in Objects)
            {
                DebugObjectBase drawObj = kv.Value;

                drawObj.Update();

                if(!paused && drawObj.LiveSeconds >= 0)
                {
                    drawObj.LiveSeconds -= (1f / 60f);
                    if(drawObj.LiveSeconds <= 0)
                        RemoveIds.Add(kv.Key);
                }
            }

            if(RemoveIds.Count > 0)
            {
                foreach(int id in RemoveIds)
                {
                    Objects.Remove(id);
                }

                RemoveIds.Clear();
            }

            return true;
        }
    }
}