using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Interfaces;
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
        /// <summary>
        /// API unit testing for checking if API interaction works without errors.
        /// </summary>
        internal class ValidationProgram : Sandbox.ModAPI.Ingame.MyGridProgram
        {
            public static bool APIVerified = false;
            public static bool IsTesting = false;

#if false // enable only locally to validate API
            public static void VerifyAPI(IMyProgrammableBlock pb)
            {
                if(!APIVerified)
                    new ValidationProgram(pb);
            }

            private ValidationProgram(IMyProgrammableBlock realPB)
            {
                APIVerified = true;
                IsTesting = true;

                try
                {
                    Me = realPB;
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
        }

        /// <summary>
        /// A fake PB that never does anything (property getter for API will not work) resulting in mod not being detected, which is intended to test the wrapper for no-mod-presence.
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
            public bool HasNobodyPlayerAccessToBlock() => false;
            public bool HasPlayerAccess(long playerId, MyRelationsBetweenPlayerAndBlock defaultNoUser = MyRelationsBetweenPlayerAndBlock.NoOwnership) => false;
            public bool HasPlayerAccessWithNobodyCheck(long playerId, bool isForPB = false) => false;
            public bool IsSameConstructAs(Sandbox.ModAPI.Ingame.IMyTerminalBlock other) => false;
            public void RequestEnable(bool enable) { }
            public void SearchActionsOfName(string name, List<ITerminalAction> resultList, Func<ITerminalAction, bool> collect = null) { }
            public void SetCustomName(string text) { }
            public void SetCustomName(StringBuilder text) { }
            public bool TryRun(string argument) => false;
            public void UpdateIsWorking() { }
            public void UpdateVisual() { }
        }
#else
            public static void VerifyAPI(IMyProgrammableBlock pb)
            {
            }
        }
#endif
    }
}
