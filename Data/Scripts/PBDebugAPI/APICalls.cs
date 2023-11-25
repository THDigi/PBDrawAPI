using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components.Interfaces;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static PB.Program;
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
        }

        /// <summary>
        /// API unit testing for checking if API interaction works without errors.
        /// </summary>
        class ValidationProgram : Sandbox.ModAPI.Ingame.MyGridProgram
        {
            public static bool APIVerified = false;
            public static bool IsTesting = false;

            void TestMethods(DebugAPI api, bool testWithModPresent)
            {
                api.DrawPoint(Vector3D.Zero, Color.White);
                api.DrawLine(Vector3D.Zero, Vector3D.One, Color.White);
                api.DrawAABB(new BoundingBoxD(-Vector3D.Half, Vector3D.Half), Color.White);
                api.DrawOBB(new MyOrientedBoundingBoxD(MatrixD.Identity), Color.White);
                api.DrawSphere(new BoundingSphereD(Vector3D.Zero, 1), Color.White);
                api.DrawMatrix(MatrixD.Identity);
                api.DrawGPS("Validation", Vector3D.Zero, Color.Lime);
                api.PrintHUD("Validation");
                api.PrintChat("Validation");
                api.GetTick();
                api.GetTimestamp();
                using(api.Measure((t) => { })) { }
                using(api.Measure("")) { }
                api.Remove(0);
                api.RemoveDraw();
                api.RemoveAll();

                if(testWithModPresent)
                {
                    var inputs = (DebugAPI.Input[])Enum.GetValues(typeof(DebugAPI.Input));
                    foreach(var input in inputs)
                    {
                        TestAdjustedNumber(api, input, testWithModPresent);
                    }
                }
                else // no need to test all inputs again
                {
                    TestAdjustedNumber(api, DebugAPI.Input.A, testWithModPresent);
                }
            }

            void TestAdjustedNumber(DebugAPI api, DebugAPI.Input input, bool testWithModPresent)
            {
                int random = MyUtils.GetRandomInt(-1000, 1000); // using it just to get more reliable comparison

                int id;
                api.DeclareAdjustNumber(out id, random, 1, input);

                const int noModDefault = -5;
                int value = (int)api.GetAdjustNumber(id, noModDefault);

                if(testWithModPresent)
                {
                    if(value != random)
                        Log.Error($"API with mod validation: Mismatched result on adjust number, created with: {random}, got back: {value}");
                }
                else
                {
                    if(value != noModDefault)
                        Log.Error($"API no-mod validation: Mismatched result on adjust number, expected noModDefault: {noModDefault}, got: {value}");
                }

                api.RemoveAll(); // avoid filling the pool with unnecessary stuff
            }

            public ValidationProgram(IMyProgrammableBlock pb)
            {
                APIVerified = true;
                IsTesting = true;

                try
                {
                    Me = pb;
                    DebugAPI api = new DebugAPI(this);
                    TestMethods(api, testWithModPresent: true);

                    // simulate when mod is not present
                    Me = new DummyProgrammableBlock();
                    api = new DebugAPI(this);
                    TestMethods(api, testWithModPresent: false);

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
                    IsTesting = false;
                }
            }
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

        static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        TimeSpan API_GetTimestamp()
        {
            return TimeSpan.FromTicks((long)(Stopwatch.GetTimestamp() * TickFrequency));
        }
        #endregion

        /// <summary>
        /// A fake PB that never does anything causing the mod to not be detected (which is the intention).
        /// </summary>
        private class DummyProgrammableBlock : IMyProgrammableBlock
        {
            public bool IsRunning { get; }
            public string TerminalRunArgument { get; }
            public bool Enabled { get; set; }
            public string CustomName { get; set; }
            public string CustomNameWithFaction { get; }
            public string DetailedInfo { get; }
            public string CustomInfo { get; }
            public string CustomData { get; set; }
            public bool ShowOnHUD { get; set; }
            public bool ShowInTerminal { get; set; }
            public bool ShowInToolbarConfig { get; set; }
            public bool ShowInInventory { get; set; }
            public SerializableDefinitionId BlockDefinition { get; }
            public VRage.Game.ModAPI.Ingame.IMyCubeGrid CubeGrid { get; }
            public string DefinitionDisplayNameText { get; }
            public float DisassembleRatio { get; }
            public string DisplayNameText { get; }
            public bool IsBeingHacked { get; }
            public bool IsFunctional { get; }
            public bool IsWorking { get; }
            public Vector3I Max { get; }
            public float Mass { get; }
            public Vector3I Min { get; }
            public int NumberInGrid { get; }
            public MyBlockOrientation Orientation { get; }
            public long OwnerId { get; }
            public Vector3I Position { get; }
            public IMyEntityComponentContainer Components { get; }
            public long EntityId { get; }
            public string Name { get; }
            public string DisplayName { get; }
            public bool HasInventory { get; }
            public int InventoryCount { get; }
            public bool Closed { get; }
            public BoundingBoxD WorldAABB { get; }
            public BoundingBoxD WorldAABBHr { get; }
            public MatrixD WorldMatrix { get; }
            public BoundingSphereD WorldVolume { get; }
            public BoundingSphereD WorldVolumeHr { get; }
            public bool UseGenericLcd { get; }
            public int SurfaceCount { get; }
            public void GetActions(List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null) { }
            public ITerminalAction GetActionWithName(string name) => null;
            public VRage.Game.ModAPI.Ingame.IMyInventory GetInventory() => null;
            public VRage.Game.ModAPI.Ingame.IMyInventory GetInventory(int index) => null;
            public string GetOwnerFactionTag() => null;
            public MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner() => MyRelationsBetweenPlayerAndBlock.NoOwnership;
            public Vector3D GetPosition() => Vector3D.Zero;
            public void GetProperties(List<ITerminalProperty> resultList, Func<ITerminalProperty, bool> collect = null) { }
            public ITerminalProperty GetProperty(string id) => null;
            public Sandbox.ModAPI.Ingame.IMyTextSurface GetSurface(int index) => null;
            public MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser = MyRelationsBetweenPlayerAndBlock.NoOwnership) => MyRelationsBetweenPlayerAndBlock.NoOwnership;
            public bool HasLocalPlayerAccess() => false;
            public bool HasPlayerAccess(long playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser = MyRelationsBetweenPlayerAndBlock.NoOwnership) => false;
            public bool IsSameConstructAs(Sandbox.ModAPI.Ingame.IMyTerminalBlock other) => false;
            public void RequestEnable(bool enable) { }
            public void SearchActionsOfName(string name, List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null) { }
            public void SetCustomName(string text) { }
            public void SetCustomName(StringBuilder text) { }
            public bool TryRun(string argument) => false;
            public void UpdateIsWorking() { }
            public void UpdateVisual() { }
        }
    }
}
