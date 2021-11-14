using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

// needed like this or it can get ambiguity errors from game's mod compiler adding ModAPI usings.
// don't copy these in your PB script though.
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
using UpdateFrequency = Sandbox.ModAPI.Ingame.UpdateFrequency;
using UpdateType = Sandbox.ModAPI.Ingame.UpdateType;
using MyGridProgram = Sandbox.ModAPI.Ingame.MyGridProgram;

// Copy the DrawAPI class to your class and use it like shown here.
public class Program : MyGridProgram
{
    DrawAPI Draw;

    public Program()
    {
        Draw = new DrawAPI(this);

        // if you want to rely on the drawing or not, optional.
        if(!Draw.ModDetected)
            throw new Exception("DrawAPI mod not detected");

        Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }

    public void Main(string argument, UpdateType updateType)
    {
        Draw.RemoveAll(); // remove all previously added draw objects

        MatrixD pbm = Me.WorldMatrix;

        Vector3D somePoint = pbm.Translation + pbm.Forward * 2;

        // adds a permanent point (until removed by returned id or by RemoveAll)
        Draw.AddPoint(somePoint, Color.Red, 0.5f, onTop: true);

        Draw.AddLine(somePoint, somePoint + pbm.Up * 3, Color.Lime, onTop: true);

        Draw.AddAABB(Me.CubeGrid.WorldAABB, Color.Blue);

        BoundingBoxD gridLocalBB = new BoundingBoxD(Me.CubeGrid.Min * Me.CubeGrid.GridSize, Me.CubeGrid.Max * Me.CubeGrid.GridSize);
        MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(gridLocalBB, Me.CubeGrid.WorldMatrix);
        Draw.AddOBB(obb, new Color(255, 0, 255));

        Draw.AddSphere(Me.WorldVolume, Color.Yellow * 0.5f, DrawAPI.Style.Solid);

        Draw.AddMatrix(pbm, onTop: true);

        Draw.AddHudMarker("something is here", somePoint, Color.Blue);
    }

    public class DrawAPI
    {
        public readonly bool ModDetected;

        public void RemoveAll() => _removeAll(_program.Me);
        Action<IMyProgrammableBlock> _removeAll;

        public void Remove(int id) => _remove(_program.Me, id);
        Action<IMyProgrammableBlock, int> _remove;

        public int AddPoint(Vector3D origin, Color color, float radius = 0.2f, float seconds = DefaultSeconds, bool? onTop = null) => _point(_program.Me, origin, color, radius, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int> _point;

        public int AddLine(Vector3D start, Vector3D end, Color color, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _line(_program.Me, start, end, color, thickness, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int> _line;

        public int AddAABB(BoundingBoxD bb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _aabb(_program.Me, bb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int> _aabb;

        public int AddOBB(MyOrientedBoundingBoxD obb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _obb(_program.Me, obb, color, (int)style, thickness, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int> _obb;

        public int AddSphere(BoundingSphereD sphere, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, int lineEveryDegrees = 15, float seconds = DefaultSeconds, bool? onTop = null) => _sphere(_program.Me, sphere, color, (int)style, thickness, lineEveryDegrees, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int> _sphere;

        public int AddMatrix(MatrixD matrix, float length = 3f, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _matrix(_program.Me, matrix, length, thickness, seconds, onTop ?? _defaultOnTop);
        Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int> _matrix;

        public int AddHudMarker(string name, Vector3D origin, Color color, float seconds = DefaultSeconds) => _hudMarker(_program.Me, name, origin, color, seconds);
        Func<IMyProgrammableBlock, string, Vector3D, Color, float, int> _hudMarker;

        public enum Style { Solid, Wireframe, SolidAndWireframe }

        const float DefaultThickness = 0.02f;
        const float DefaultSeconds = -1;

        MyGridProgram _program;
        bool _defaultOnTop;

        /// <param name="program">pass `this`.</param>
        /// <param name="drawOnTopDefault">declare if all drawn objects are always on top by default.</param>
        public DrawAPI(MyGridProgram program, bool drawOnTopDefault = false)
        {
            if(program == null)
                throw new Exception("Pass `this` into the API, not null.");

            _defaultOnTop = drawOnTopDefault;
            _program = program;
            var methods = program.Me.GetProperty("DebugDrawAPI")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(program.Me);
            ModDetected = (methods != null);
            if(ModDetected)
            {
                Assign(out _removeAll, methods["RemoveAll"]);
                Assign(out _remove, methods["Remove"]);
                Assign(out _point, methods["Point"]);
                Assign(out _line, methods["Line"]);
                Assign(out _aabb, methods["AABB"]);
                Assign(out _obb, methods["OBB"]);
                Assign(out _sphere, methods["Sphere"]);
                Assign(out _matrix, methods["Matrix"]);
                Assign(out _hudMarker, methods["HUDMarker"]);
            }
        }

        void Assign<T>(out T field, object method) => field = (T)method;
    }
}