using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static PB.Program;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDebugAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class PBDebugAPIMod : MySessionComponentBase
    {
        const int LimitObjectsPerPB = 1000;

        bool Requirements => MyAPIGateway.Session.IsServer && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        public Dictionary<IMyProgrammableBlock, DebugObjectHost> DrawPerPB;
        List<IMyProgrammableBlock> RemovePBs;

        public ChatCommands ChatCommands;
        public APIHandler APIHandler;

        bool UnitTesting = false;
        bool APIVerified = false;

        Dictionary<Type, object> DrawObjectPools;

        public const string TerminalPropertyId = "DebugAPI";

        public override void LoadData()
        {
            try
            {
                Log.ModName = "PB Debug API";

                if(!Requirements)
                    return;

                DrawPerPB = new Dictionary<IMyProgrammableBlock, DebugObjectHost>();
                RemovePBs = new List<IMyProgrammableBlock>();

                DrawObjectPools = new Dictionary<Type, object>();

                ChatCommands = new ChatCommands(this);

                APIHandler = new APIHandler(this, TerminalPropertyId);
                SetupAPI();

                MyEntities.OnEntityCreate += EntityCreated;
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
            MyEntities.OnEntityCreate -= EntityCreated;

            ChatCommands?.Dispose();

            APIHandler?.Dispose();
        }

        void EntityCreated(MyEntity ent) // NOTE: called from a thread
        {
            IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
            if(pb != null)
            {
                // only need the first PB
                MyEntities.OnEntityCreate -= EntityCreated;

                APIHandler.SubmitAndCreate();
            }
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

        class ValidationProgram : Sandbox.ModAPI.Ingame.MyGridProgram
        {
            public ValidationProgram(IMyProgrammableBlock pb)
            {
                ((IMyGridProgram)this).Me = pb;
            }
        }

        public void VerifyAPI(IMyProgrammableBlock pb)
        {
            if(APIVerified)
                return;

            APIVerified = true;

            try
            {
                UnitTesting = true;
                ValidationProgram program = new ValidationProgram(pb);
                DebugAPI api = new DebugAPI(program);
                api.DrawPoint(Vector3D.Zero, Color.White);
                api.DrawLine(Vector3D.Zero, Vector3D.One, Color.White);
                api.DrawAABB(new BoundingBoxD(-Vector3D.Half, Vector3D.Half), Color.White);
                api.DrawOBB(new MyOrientedBoundingBoxD(MatrixD.Identity), Color.White);
                api.DrawSphere(new BoundingSphereD(Vector3D.Zero, 1), Color.White);
                api.DrawMatrix(MatrixD.Identity);
                api.DrawGPS("", Vector3D.Zero, Color.Lime);
                api.Remove(0);
                api.PrintHUD("");
                api.PrintChat("");
                api.GetAdjustNumber(0);
                api.GetTick();
                api.RemoveDraw();
                api.RemoveAll();

                var inputs = (DebugAPI.Input[])Enum.GetValues(typeof(DebugAPI.Input));
                foreach(var input in inputs)
                {
                    int id;
                    api.DeclareAdjustNumber(out id, 1, 1, input);
                    api.RemoveAll(); // avoid allocating a lot of these
                }

                DrawPerPB.Remove(pb);

                MyLog.Default.WriteLineAndConsole($"{GetType().FullName}: API validation succesful!");
                Log.Info($"API validation succesful!");
            }
            catch(Exception e)
            {
                // these write errors to game's log too
                Log.Error($"Failed to validate API!");
                Log.Error(e);
            }
            finally
            {
                UnitTesting = false;
            }
        }

        void SetupAPI()
        {
            APIHandler.AddMethod("RemoveAll", new Action<IMyProgrammableBlock>(API_RemoveAll));
            APIHandler.AddMethod("RemoveDraw", new Action<IMyProgrammableBlock>(API_RemoveDraw));
            APIHandler.AddMethod("Remove", new Action<IMyProgrammableBlock, int>(API_Remove));
            APIHandler.AddMethod("Point", new Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int>(API_Point));
            APIHandler.AddMethod("Line", new Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int>(API_Line));
            APIHandler.AddMethod("AABB", new Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int>(API_AABB));
            APIHandler.AddMethod("OBB", new Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int>(API_OBB));
            APIHandler.AddMethod("Sphere", new Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int>(API_Sphere));
            APIHandler.AddMethod("Matrix", new Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int>(API_Matrix));
            APIHandler.AddMethod("GPS", new Func<IMyProgrammableBlock, string, Vector3D, Color?, float, int>(API_GPS));
            APIHandler.AddMethod("HUDNotification", new Func<IMyProgrammableBlock, string, string, float, int>(API_PrintHUD));
            APIHandler.AddMethod("Chat", new Action<IMyProgrammableBlock, string, string, Color?, string>(API_PrintChat));
            APIHandler.AddMethod("DeclareAdjustNumber", new Func<IMyProgrammableBlock, double, double, string, string, int>(API_DeclareAdjustNumber));
            APIHandler.AddMethod("GetAdjustNumber", new Func<IMyProgrammableBlock, int, double>(API_GetAdjustNumber));
            APIHandler.AddMethod("Tick", new Func<int>(API_GetTick));
        }

        void API_Remove(IMyProgrammableBlock pb, int id)
        {
            DrawPerPB.GetValueOrDefault(pb)?.Remove(id);
        }

        void API_RemoveAll(IMyProgrammableBlock pb)
        {
            DrawPerPB.GetValueOrDefault(pb)?.RemoveAll();
        }

        void API_RemoveDraw(IMyProgrammableBlock pb)
        {
            DrawPerPB.GetValueOrDefault(pb)?.RemoveTypes<IDrawObject>();
        }

        int API_Point(IMyProgrammableBlock pb, Vector3D origin, Color color, float size, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawPoint> pool = GetPool<DrawPoint>();
            DrawPoint obj = pool.Get();
            obj.Init(pool, origin, color, size, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_Line(IMyProgrammableBlock pb, Vector3D start, Vector3D end, Color color, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawLine> pool = GetPool<DrawLine>();
            DrawLine obj = pool.Get();
            obj.Init(pool, start, end, color, thickness, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_AABB(IMyProgrammableBlock pb, BoundingBoxD bb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawAABB> pool = GetPool<DrawAABB>();
            DrawAABB obj = pool.Get();
            obj.Init(pool, bb, color, style, thickness, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_OBB(IMyProgrammableBlock pb, MyOrientedBoundingBoxD obb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawOBB> pool = GetPool<DrawOBB>();
            DrawOBB obj = pool.Get();
            obj.Init(pool, obb, color, style, thickness, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_Sphere(IMyProgrammableBlock pb, BoundingSphereD sphere, Color color, int style, float thickness, int lineEveryDegrees, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawSphere> pool = GetPool<DrawSphere>();
            DrawSphere obj = pool.Get();
            obj.Init(pool, sphere, color, style, thickness, lineEveryDegrees, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_Matrix(IMyProgrammableBlock pb, MatrixD matrix, float length, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawMatrix> pool = GetPool<DrawMatrix>();
            DrawMatrix obj = pool.Get();
            obj.Init(pool, matrix, length, thickness, seconds, onTop);
            return AddDebugObject(pb, obj);
        }

        int API_GPS(IMyProgrammableBlock pb, string name, Vector3D origin, Color? color, float seconds)
        {
            MyConcurrentPool<DrawGPS> pool = GetPool<DrawGPS>();
            DrawGPS obj = pool.Get();
            obj.Init(pool, name, origin, color, seconds);
            return AddDebugObject(pb, obj);
        }

        int API_PrintHUD(IMyProgrammableBlock pb, string message, string font, float seconds)
        {
            if(UnitTesting)
                return -1;

            if(seconds <= 0)
                throw new Exception($"PrintHUD() cannot be shown for {seconds} seconds.");

            MyConcurrentPool<PrintHUD> pool = GetPool<PrintHUD>();
            PrintHUD obj = pool.Get();
            obj.Init(pool, message, font, seconds);
            return AddDebugObject(pb, obj);
        }

        void API_PrintChat(IMyProgrammableBlock pb, string message, string sender, Color? senderColor, string font)
        {
            if(UnitTesting)
                return;

            MyDefinitionBase fontDef = MyDefinitionManager.Static.GetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), font));
            if(fontDef == null)
            {
                //Log.Error($"Font '{font}' does not exist, reverting to 'Debug'.");
                font = "Debug";
            }

            if(string.IsNullOrEmpty(sender))
                sender = pb.CustomName;

            if(!senderColor.HasValue)
                senderColor = Color.White;

            // NOTE: 0 shows to everyone, if this mod were allowed in MP
            MyVisualScriptLogicProvider.SendChatMessageColored(message, senderColor.Value, sender, 0, font);
        }

        int API_DeclareAdjustNumber(IMyProgrammableBlock pb, double initial, double step, string keyModifier, string label)
        {
            MyConcurrentPool<AdjustNumber> pool = GetPool<AdjustNumber>();
            AdjustNumber obj = pool.Get();
            obj.Init(pool, label, initial, step, keyModifier);
            return AddDebugObject(pb, obj);
        }

        double API_GetAdjustNumber(IMyProgrammableBlock pb, int id)
        {
            if(UnitTesting)
                return 1;

            DebugObjectHost handler;
            DebugObjectBase drawObj;
            if(DrawPerPB.TryGetValue(pb, out handler) && handler.Objects.TryGetValue(id, out drawObj))
            {
                AdjustNumber adjustable = drawObj as AdjustNumber;
                if(adjustable != null)
                    return adjustable.Value;
                else
                    throw new Exception($"GetAdjustNumber(): Given id={id} is not a {nameof(AdjustNumber)}, but a {drawObj.GetType().Name}");
            }

            throw new Exception($"GetAdjustNumber(): No {nameof(AdjustNumber)} with id={id}");
            //return double.NaN;
        }

        int API_GetTick()
        {
            return MyAPIGateway.Session.GameplayFrameCounter;
        }
    }
}
