using System;
using VRageMath;

// avoiding ambiguity errors with mod compiler adding mod namespaces.
// remove all of these if copying the entire file to a PB project.
using UpdateType = Sandbox.ModAPI.Ingame.UpdateType;
using MyGridProgram = Sandbox.ModAPI.Ingame.MyGridProgram;

namespace IngameScript.Dummy
{
    public class Program : MyGridProgram
    {
        public Program()
        {
        }

        public void Main(string argument, UpdateType updateType)
        {
        }

        // This is the dummy version if you wish to minimize how many characters the API uses without removing it from your code.
        public class DebugAPI
        {
            public readonly bool ModDetected;
            public void RemoveDraw() { }
            public void RemoveAll() { }
            public void Remove(int id) { }
            public int DrawPoint(Vector3D origin, Color color, float radius = 0.2f, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawLine(Vector3D start, Vector3D end, Color color, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawAABB(BoundingBoxD bb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawOBB(MyOrientedBoundingBoxD obb, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawSphere(BoundingSphereD sphere, Color color, Style style = Style.Wireframe, float thickness = DefaultThickness, int lineEveryDegrees = 15, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawMatrix(MatrixD matrix, float length = 1f, float thickness = DefaultThickness, float seconds = DefaultSeconds, bool? onTop = null) => -1;
            public int DrawGPS(string name, Vector3D origin, Color? color = null, float seconds = DefaultSeconds) => -1;
            public int PrintHUD(string message, Font font = Font.Debug, float seconds = 2) => -1;
            public void PrintChat(string message, string sender = null, Color? senderColor = null, Font font = Font.Debug) { }
            public void DeclareAdjustNumber(out int id, double initial, double step = 0.05, Input modifier = Input.Control, string label = null) => id = -1;
            public double GetAdjustNumber(int id, double noModDefault = 1) => noModDefault;
            public int GetTick() => -1;
            public TimeSpan GetTimestamp() => TimeSpan.Zero;
            public MeasureToken Measure(Action<TimeSpan> call) => new MeasureToken(this, call);
            public struct MeasureToken : IDisposable { public MeasureToken(DebugAPI api, Action<TimeSpan> call) { } public void Dispose() { } }
            public enum Style { Solid, Wireframe, SolidAndWireframe }
            public enum Input { MouseLeftButton, MouseRightButton, MouseMiddleButton, MouseExtraButton1, MouseExtraButton2, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Tab, Shift, Control, Alt, Space, PageUp, PageDown, End, Home, Insert, Delete, Left, Up, Right, Down, D0, D1, D2, D3, D4, D5, D6, D7, D8, D9, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9, Multiply, Add, Separator, Subtract, Decimal, Divide, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12 }
            public enum Font { Debug, White, Red, Green, Blue, DarkBlue }
            const float DefaultThickness = 0.02f;
            const float DefaultSeconds = -1;
            public DebugAPI(MyGridProgram program, bool drawOnTopDefault = false) { }
        }
    }
}