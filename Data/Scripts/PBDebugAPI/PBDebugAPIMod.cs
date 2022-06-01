using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDebugAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public partial class PBDebugAPIMod : MySessionComponentBase
    {
        const int LimitObjectsPerPB = 1000;

        bool Requirements => MyAPIGateway.Session.IsServer && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        public Dictionary<IMyProgrammableBlock, DebugObjectHost> DrawPerPB;
        List<IMyProgrammableBlock> RemovePBs;

        public ChatCommands ChatCommands;
        public PBInterface PBInterface;

        Dictionary<Type, object> DrawObjectPools;

        public const string TerminalPropertyId = "DebugAPI";

        public override void LoadData()
        {
            try
            {
                Log.ModName = "PB DebugAPI";

                if(!Requirements)
                    return;

                DrawPerPB = new Dictionary<IMyProgrammableBlock, DebugObjectHost>();
                RemovePBs = new List<IMyProgrammableBlock>();

                DrawObjectPools = new Dictionary<Type, object>();

                ChatCommands = new ChatCommands(this);

                PBInterface = new PBInterface(this, TerminalPropertyId);
                SetupAPI();
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");
            }
        }

        public override void BeforeStart()
        {
            if(!Requirements)
            {
                string msg = $"Can't use {Log.ModName} mod in any online mode (currently={MyAPIGateway.Session.OnlineMode.ToString()})";
                MyLog.Default.WriteLine($"ERROR: {msg}");
                MyAPIGateway.Utilities.ShowMessage("ERROR", msg);
            }
        }

        protected override void UnloadData()
        {
            ChatCommands?.Dispose();

            PBInterface?.Dispose();
        }

        public void VerifyAPI(IMyProgrammableBlock pb)
        {
            if(!ValidationProgram.APIVerified)
                new ValidationProgram(pb);
        }

        public override void Draw()
        {
            try
            {
                if(DrawPerPB == null || DrawPerPB.Count <= 0)
                    return;

                foreach(KeyValuePair<IMyProgrammableBlock, DebugObjectHost> kv in DrawPerPB)
                {
                    DebugObjectHost handler = kv.Value;
                    if(!handler.Update())
                    {
                        RemovePBs.Add(kv.Key);
                    }
                }

                if(RemovePBs.Count > 0)
                {
                    foreach(IMyProgrammableBlock pb in RemovePBs)
                    {
                        DrawPerPB.Remove(pb);
                    }

                    RemovePBs.Clear();
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        void PBClosing(IMyEntity ent)
        {
            try
            {
                IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
                if(pb != null)
                {
                    DrawPerPB.GetValueOrDefault(pb)?.RemoveAll();
                    DrawPerPB.Remove(pb);
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        MyConcurrentPool<T> GetPool<T>() where T : DebugObjectBase, new()
        {
            Type type = typeof(T);
            object obj;
            if(!DrawObjectPools.TryGetValue(type, out obj))
                DrawObjectPools[type] = obj = new MyConcurrentPool<T>();

            return (MyConcurrentPool<T>)obj;
        }

        int AddDebugObject(IMyProgrammableBlock pb, DebugObjectBase drawObject)
        {
            DebugObjectHost handler;
            if(!DrawPerPB.TryGetValue(pb, out handler))
            {
                DrawPerPB[pb] = handler = new DebugObjectHost(pb);

                IMyTerminalBlock tb = (IMyTerminalBlock)pb;
                tb.OnMarkForClose += PBClosing;
            }

            if(handler.Objects.Count > LimitObjectsPerPB)
                throw new Exception($"More than {LimitObjectsPerPB} active DrawObjects on this PB.");

            return handler.Add(drawObject);
        }
    }
}
