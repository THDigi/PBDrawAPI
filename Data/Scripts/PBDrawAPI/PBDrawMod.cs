using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static Program;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDrawAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class PBDrawMod : MySessionComponentBase
    {
        const int LimitObjectsPerPB = 1000;

        bool Requirements => MyAPIGateway.Session.IsServer && MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        Dictionary<IMyProgrammableBlock, PBDrawHandler> DrawPerPB;
        List<IMyProgrammableBlock> RemovePBs;

        APIHandler APIHandler;

        Dictionary<Type, object> DrawObjectPools;

        public override void LoadData()
        {
            try
            {
                if(!Requirements)
                    return;

                DrawPerPB = new Dictionary<IMyProgrammableBlock, PBDrawHandler>();
                RemovePBs = new List<IMyProgrammableBlock>();

                DrawObjectPools = new Dictionary<Type, object>();

                APIHandler = new APIHandler("DebugDrawAPI");
                SetupAPI();

                MyEntities.OnEntityCreate += EntityCreated;
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
            }
        }

        public override void BeforeStart()
        {
            if(!Requirements)
            {
                string msg = $"Can't use PB draw mod in any online mode (currently={MyAPIGateway.Session.OnlineMode.ToString()})";
                MyLog.Default.WriteLine($"ERROR: {msg}");
                MyAPIGateway.Utilities.ShowMessage("ERROR", msg);
            }
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityCreate -= EntityCreated;
        }

        void EntityCreated(MyEntity ent)
        {
            IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
            if(pb != null)
            {
                MyEntities.OnEntityCreate -= EntityCreated;

                if(!APIHandler.Created)
                {
                    APIHandler.SubmitAndCreate();
                    VerifyAPI(pb);
                }
            }
        }

        public override void Draw()
        {
            try
            {
                if(DrawPerPB == null || DrawPerPB.Count <= 0)
                    return;

                foreach(KeyValuePair<IMyProgrammableBlock, PBDrawHandler> kv in DrawPerPB)
                {
                    PBDrawHandler handler = kv.Value;
                    if(!handler.Draw())
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

        void PBMarkForClose(IMyEntity ent)
        {
            try
            {
                IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
                if(pb != null)
                    DrawPerPB.Remove(pb);
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

        void VerifyAPI(IMyProgrammableBlock pb)
        {
            bool success = false;
            try
            {
                ValidationProgram program = new ValidationProgram(pb);
                DrawAPI api = new DrawAPI(program);
                api.AddPoint(Vector3D.Zero, Color.White);
                api.AddLine(Vector3D.Zero, Vector3D.One, Color.White);
                api.AddAABB(new BoundingBoxD(-Vector3D.Half, Vector3D.Half), Color.White);
                api.AddOBB(new MyOrientedBoundingBoxD(MatrixD.Identity), Color.White);
                api.AddSphere(new BoundingSphereD(Vector3D.Zero, 1), Color.White);
                api.AddMatrix(MatrixD.Identity);
                api.AddHudMarker("o hi", Vector3D.Zero, Color.Lime);
                api.Remove(0);
                api.RemoveAll();
                DrawPerPB.Remove(pb);
                success = true;
            }
            catch(Exception e)
            {
                Log.Error($"Failed to validate {nameof(DrawAPI)}!");
                Log.Error(e);
            }

            if(success)
            {
                Log.Info($"{nameof(DrawAPI)} creation succesful!");
                MyLog.Default.WriteLineAndConsole($"{GetType().FullName}: {nameof(DrawAPI)} creation succesful!");
            }
        }

        int AddDrawObject(IMyProgrammableBlock pb, DrawObject drawObject)
        {
            PBDrawHandler handler;
            if(!DrawPerPB.TryGetValue(pb, out handler))
            {
                DrawPerPB[pb] = handler = new PBDrawHandler(pb);

                IMyTerminalBlock tb = (IMyTerminalBlock)pb;
                tb.OnMarkForClose += PBMarkForClose;
            }

            if(handler.DrawObjects.Count > LimitObjectsPerPB)
                throw new Exception($"More than {LimitObjectsPerPB} active DrawObjects on this PB.");

            int id = handler.NextId;
            handler.DrawObjects.Add(id, drawObject);
            handler.NextId++;
            return id;
        }

        MyConcurrentPool<T> GetPool<T>() where T : DrawObject, new()
        {
            Type type = typeof(T);
            object obj;
            if(!DrawObjectPools.TryGetValue(type, out obj))
                DrawObjectPools[type] = obj = new MyConcurrentPool<T>();

            return (MyConcurrentPool<T>)obj;
        }

        void SetupAPI()
        {
            APIHandler.AddMethod("RemoveAll", new Action<IMyProgrammableBlock>(RemoveAll));
            APIHandler.AddMethod("Remove", new Action<IMyProgrammableBlock, int>(Remove));
            APIHandler.AddMethod("Point", new Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int>(AddPoint));
            APIHandler.AddMethod("Line", new Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int>(AddLine));
            APIHandler.AddMethod("AABB", new Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int>(AddAABB));
            APIHandler.AddMethod("OBB", new Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int>(AddOBB));
            APIHandler.AddMethod("Sphere", new Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int>(AddSphere));
            APIHandler.AddMethod("Matrix", new Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int>(AddMatrix));
            APIHandler.AddMethod("HUDMarker", new Func<IMyProgrammableBlock, string, Vector3D, Color, float, int>(AddHUDMarker));
        }

        void Remove(IMyProgrammableBlock pb, int id)
        {
            PBDrawHandler handler;
            DrawObject drawObject;
            if(DrawPerPB.TryGetValue(pb, out handler) && handler.DrawObjects.TryGetValue(id, out drawObject))
            {
                handler.DrawObjects.Remove(id);
                drawObject.Dispose();
            }
        }

        void RemoveAll(IMyProgrammableBlock pb)
        {
            PBDrawHandler handler;
            if(DrawPerPB.TryGetValue(pb, out handler))
            {
                try
                {
                    foreach(DrawObject drawObject in handler.DrawObjects.Values)
                    {
                        drawObject.Dispose();
                    }
                }
                finally
                {
                    handler.DrawObjects.Clear();
                }
            }
        }

        int AddPoint(IMyProgrammableBlock pb, Vector3D origin, Color color, float size, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawPoint> pool = GetPool<DrawPoint>();
            DrawPoint obj = pool.Get();
            obj.Init(pool, origin, color, size, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddLine(IMyProgrammableBlock pb, Vector3D start, Vector3D end, Color color, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawLine> pool = GetPool<DrawLine>();
            DrawLine obj = pool.Get();
            obj.Init(pool, start, end, color, thickness, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddAABB(IMyProgrammableBlock pb, BoundingBoxD bb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawAABB> pool = GetPool<DrawAABB>();
            DrawAABB obj = pool.Get();
            obj.Init(pool, bb, color, style, thickness, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddOBB(IMyProgrammableBlock pb, MyOrientedBoundingBoxD obb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawOBB> pool = GetPool<DrawOBB>();
            DrawOBB obj = pool.Get();
            obj.Init(pool, obb, color, style, thickness, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddSphere(IMyProgrammableBlock pb, BoundingSphereD sphere, Color color, int style, float thickness, int lineEveryDegrees, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawSphere> pool = GetPool<DrawSphere>();
            DrawSphere obj = pool.Get();
            obj.Init(pool, sphere, color, style, thickness, lineEveryDegrees, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddMatrix(IMyProgrammableBlock pb, MatrixD matrix, float length, float thickness, float seconds, bool onTop)
        {
            MyConcurrentPool<DrawMatrix> pool = GetPool<DrawMatrix>();
            DrawMatrix obj = pool.Get();
            obj.Init(pool, matrix, length, thickness, seconds, onTop);
            return AddDrawObject(pb, obj);
        }

        int AddHUDMarker(IMyProgrammableBlock pb, string name, Vector3D origin, Color color, float seconds)
        {
            MyConcurrentPool<DrawHUDMarker> pool = GetPool<DrawHUDMarker>();
            DrawHUDMarker obj = pool.Get();
            obj.Init(pool, name, origin, color, seconds);
            return AddDrawObject(pb, obj);
        }
    }
}
