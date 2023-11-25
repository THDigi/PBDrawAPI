using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
// avoiding ambiguity errors with mod compiler adding mod namespaces
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
using UpdateFrequency = Sandbox.ModAPI.Ingame.UpdateFrequency;
using UpdateType = Sandbox.ModAPI.Ingame.UpdateType;
using MyGridProgram = Sandbox.ModAPI.Ingame.MyGridProgram;

namespace PB.Lite
{
    // This would be the comment-less version of the DebugAPI class you can copy to your PB script along with a quick overview of how to use it.
    // See ForPB.cs for the commented+examples version
    public class Program : MyGridProgram
    {
        DebugAPI Debug;

        public Program()
        {
            Debug = new DebugAPI(this);
        }

        public void Main(string argument, UpdateType updateType)
        {
            Debug.RemoveDraw();
        }

        public class DebugAPI
        {
            public readonly bool ModDetected;

            public void RemoveDraw() => _rmd?.Invoke(_pb);
            Action<IMyProgrammableBlock> _rmd;

            public void RemoveAll() => _rma?.Invoke(_pb);
            Action<IMyProgrammableBlock> _rma;

            public void Remove(int id) => _rm?.Invoke(_pb, id);
            Action<IMyProgrammableBlock, int> _rm;

            public int DrawPoint(Vector3D origin, Color color, float radius = 0.2f, float seconds = DefaultSeconds, bool? onTop = null) => _p?.Invoke(_pb, origin, color, radius, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, Vector3D, Color, float, float, bool, int> _p;

            public int DrawLine(Vector3D start, Vector3D end, Color color, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _ln?.Invoke(_pb, start, end, color, thickness, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, Vector3D, Vector3D, Color, float, float, bool, int> _ln;

            public int DrawAABB(BoundingBoxD bb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _bb?.Invoke(_pb, bb, color, (int)style, thickness, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, BoundingBoxD, Color, int, float, float, bool, int> _bb;

            public int DrawOBB(MyOrientedBoundingBoxD obb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _obb?.Invoke(_pb, obb, color, (int)style, thickness, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, MyOrientedBoundingBoxD, Color, int, float, float, bool, int> _obb;

            public int DrawSphere(BoundingSphereD sphere, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, int lineEveryDegrees = 15, float seconds = DefaultSeconds, bool? onTop = null) => _sph?.Invoke(_pb, sphere, color, (int)style, thickness, lineEveryDegrees, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, BoundingSphereD, Color, int, float, int, float, bool, int> _sph;

            public int DrawMatrix(MatrixD matrix, float length = 1f, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => _m?.Invoke(_pb, matrix, length, thickness, seconds, onTop ?? _defTop) ?? -1;
            Func<IMyProgrammableBlock, MatrixD, float, float, float, bool, int> _m;

            public int DrawGPS(string name, Vector3D origin, Color? color = null, float seconds = DefaultSeconds) => _gps?.Invoke(_pb, name, origin, color, seconds) ?? -1;
            Func<IMyProgrammableBlock, string, Vector3D, Color?, float, int> _gps;

            public int PrintHUD(string message, Font font = Font.Debug, float seconds = 2) => _hud?.Invoke(_pb, message, font.ToString(), seconds) ?? -1;
            Func<IMyProgrammableBlock, string, string, float, int> _hud;

            public void PrintChat(string message, string sender = null, Color? senderColor = null, Font font = Font.Debug) => _chat?.Invoke(_pb, message, sender, senderColor, font.ToString());
            Action<IMyProgrammableBlock, string, string, Color?, string> _chat;

            public void DeclareAdjustNumber(out int id, double initial, double step = 0.05, Input modifier = Input.Control, string label = null) => id = _adj?.Invoke(_pb, initial, step, modifier.ToString(), label) ?? -1;
            Func<IMyProgrammableBlock, double, double, string, string, int> _adj;

            public double GetAdjustNumber(int id, double noModDefault = 1) => _getAdj?.Invoke(_pb, id) ?? noModDefault;
            Func<IMyProgrammableBlock, int, double> _getAdj;

            public int GetTick() => _tk?.Invoke() ?? -1;
            Func<int> _tk;

            public enum Style { Solid, Wireframe, SolidAndWireframe }
            public enum Input { MouseLeftButton, MouseRightButton, MouseMiddleButton, MouseExtraButton1, MouseExtraButton2, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Tab, Shift, Control, Alt, Space, PageUp, PageDown, End, Home, Insert, Delete, Left, Up, Right, Down, D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9, Multiply, Add, Separator, Subtract, Decimal, Divide, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }
            public enum Font { Debug, White, Red, Green, Blue, DarkBlue }
            const float DefaultThickness = 0.02f;
            const float DefaultSeconds = -1;
            IMyProgrammableBlock _pb;
            bool _defTop;
            public DebugAPI(MyGridProgram program, bool drawOnTopDefault = false)
            {
                if(program == null) throw new Exception("Pass `this` into the API, not null.");
                _defTop = drawOnTopDefault;
                _pb = program.Me;
                var methods = _pb.GetProperty("DebugAPI")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(_pb);
                if(methods != null)
                {
                    Assign(out _rma, methods["RemoveAll"]);
                    Assign(out _rmd, methods["RemoveDraw"]);
                    Assign(out _rm, methods["Remove"]);
                    Assign(out _p, methods["Point"]);
                    Assign(out _ln, methods["Line"]);
                    Assign(out _bb, methods["AABB"]);
                    Assign(out _obb, methods["OBB"]);
                    Assign(out _sph, methods["Sphere"]);
                    Assign(out _m, methods["Matrix"]);
                    Assign(out _gps, methods["GPS"]);
                    Assign(out _hud, methods["HUDNotification"]);
                    Assign(out _chat, methods["Chat"]);
                    Assign(out _adj, methods["DeclareAdjustNumber"]);
                    Assign(out _getAdj, methods["GetAdjustNumber"]);
                    Assign(out _tk, methods["Tick"]);
                    RemoveAll();
                    ModDetected = true;
                }
            }
            void Assign<T>(out T field, object method) => field = (T)method;
        }
    }
}