﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRageMath;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDebugAPI
{
    public partial class PBDebugAPIMod
    {
        // API declaration must match the keys and types in ForPB.cs & ForPB_Lite.cs
        void SetupAPI()
        {
            PBInterface.AddMethod("RemoveAll", new Action<IMyProgrammableBlock>(API_RemoveAll));
            PBInterface.AddMethod("RemoveDraw", new Action<IMyProgrammableBlock>(API_RemoveDraw));
            PBInterface.AddMethod("Remove", new Action<IMyProgrammableBlock, int>(API_Remove));
            PBInterface.AddMethod("Point", new Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int>(API_Point));
            PBInterface.AddMethod("Line", new Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int>(API_Line));
            PBInterface.AddMethod("AABB", new Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int>(API_AABB));
            PBInterface.AddMethod("OBB", new Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int>(API_OBB));
            PBInterface.AddMethod("Sphere", new Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int>(API_Sphere));
            PBInterface.AddMethod("Matrix", new Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int>(API_Matrix));
            PBInterface.AddMethod("GPS", new Func<IMyProgrammableBlock, string, Vector3D, Color?, float, int>(API_GPS));
            PBInterface.AddMethod("HUDNotification", new Func<IMyProgrammableBlock, string, string, float, int>(API_PrintHUD));
            PBInterface.AddMethod("Chat", new Action<IMyProgrammableBlock, string, string, Color?, string>(API_PrintChat));
            PBInterface.AddMethod("DeclareAdjustNumber", new Func<IMyProgrammableBlock, double, double, string, string, int>(API_DeclareAdjustNumber));
            PBInterface.AddMethod("GetAdjustNumber", new Func<IMyProgrammableBlock, int, double>(API_GetAdjustNumber));
            PBInterface.AddMethod("Tick", new Func<int>(API_GetTick));
            PBInterface.AddMethod("Timestamp", new Func<TimeSpan>(API_GetTimestamp));

            // Edit the user-side API in ForPB.cs, ForPB_Lite.cs and ForPB_Dummy.cs
            // Also enable code from Testing.cs to validate, then disable before release!
        }

        #region API backend
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
            if(seconds <= 0)
                throw new Exception($"PrintHUD() cannot be shown for {seconds} seconds.");

            MyConcurrentPool<PrintHUD> pool = GetPool<PrintHUD>();
            PrintHUD obj = pool.Get();
            obj.Init(pool, message, font, seconds);
            return AddDebugObject(pb, obj);
        }

        void API_PrintChat(IMyProgrammableBlock pb, string message, string sender, Color? senderColor, string font)
        {
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

            if(ValidationProgram.IsTesting)
                return; // cannot undo chat messages so this one needs to be skipped

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

            throw new Exception($"GetAdjustNumber(): No {nameof(AdjustNumber)} with id={id}. (Did you RemoveAll() instead of RemoveDraw() ?)");
            //return double.NaN;
        }

        int API_GetTick()
        {
            return MyAPIGateway.Session.GameplayFrameCounter;
        }

        TimeSpan API_GetTimestamp()
        {
            long swTicks = Stopwatch.GetTimestamp();
            double tickMod = (double)TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency; // stopwatch ticks to timespan ticks
            return TimeSpan.FromTicks((long)(swTicks * tickMod));
        }
        #endregion
    }
}
