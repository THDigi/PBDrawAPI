using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Digi.PBDebugAPI
{
    public interface IDrawObject { } // categorization for RemoveDraw()

    public abstract class DebugObjectBase
    {
        public float LiveSeconds;

        public abstract void Update();

        public abstract void Dispose();
    }

    public abstract class DebugDrawBillboardBase : DebugObjectBase, IDrawObject
    {
        protected bool DrawOnTop;

        protected static readonly BlendTypeEnum BlendType = BlendTypeEnum.PostPP;

        protected static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");

        protected const float OnTopColorMul = 0.5f;

        const float DepthRatioF = 0.01f;

        protected static float ToAlwaysOnTop(ref MatrixD matrix)
        {
            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D posOverlay = camMatrix.Translation + ((matrix.Translation - camMatrix.Translation) * DepthRatioF);

            MatrixD.Rescale(ref matrix, DepthRatioF);
            matrix.Translation = posOverlay;

            return DepthRatioF;
        }

        protected static float ToAlwaysOnTop(ref Vector3D position)
        {
            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            position = camMatrix.Translation + ((position - camMatrix.Translation) * DepthRatioF);

            return DepthRatioF;
        }
    }

    public class DrawPoint : DebugDrawBillboardBase
    {
        Vector3D Origin;
        Color Color;
        float Radius;
        MyConcurrentPool<DrawPoint> Pool;

        public void Init(MyConcurrentPool<DrawPoint> pool, Vector3D origin, Color color, float radius, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            Origin = origin;
            Color = color;
            Radius = radius;
        }

        public override void Update()
        {
            Color color = Color;
            Vector3D pos = Origin;
            float radius = Radius;
            MyTransparentGeometry.AddPointBillboard(MaterialDot, Color, pos, radius, 0, blendType: BlendType);

            if(DrawOnTop)
            {
                float depthScale = ToAlwaysOnTop(ref pos);
                radius *= depthScale;
                MyTransparentGeometry.AddPointBillboard(MaterialDot, Color * OnTopColorMul, pos, radius, 0, blendType: BlendType);
            }
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawLine : DebugDrawBillboardBase
    {
        Vector3D From;
        Vector3 Direction;
        Color Color;
        float Thickness;
        MyConcurrentPool<DrawLine> Pool;

        public void Init(MyConcurrentPool<DrawLine> pool, Vector3D from, Vector3D to, Color color, float thickness, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            From = from;
            Direction = (to - from);
            Color = color;
            Thickness = thickness;
        }

        public override void Update()
        {
            Vector3D from = From;
            Vector3 dir = Direction;
            float thick = Thickness;
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color, from, dir, 1f, thick, blendType: BlendType);

            if(DrawOnTop)
            {
                float depthScale = ToAlwaysOnTop(ref from);
                dir *= depthScale;
                thick *= depthScale;
                MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color * OnTopColorMul, from, dir, 1f, thick, blendType: BlendType);
            }
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawAABB : DebugDrawBillboardBase
    {
        MatrixD WorldMatrix;
        BoundingBoxD LocalBox;
        Color Color;
        float Thickness;
        MySimpleObjectRasterizer Style;
        MyConcurrentPool<DrawAABB> Pool;

        public void Init(MyConcurrentPool<DrawAABB> pool, BoundingBoxD bb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            LocalBox = new BoundingBoxD(-bb.HalfExtents, bb.HalfExtents);
            WorldMatrix = MatrixD.CreateTranslation(bb.Center);
            Color = color;
            Style = (MySimpleObjectRasterizer)style;
            Thickness = thickness;
        }

        public override void Update()
        {
            MatrixD wm = WorldMatrix;
            float thick = Thickness;
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref LocalBox, ref Color, Style, 1, thick, MaterialSquare, MaterialSquare, blendType: BlendType);

            if(DrawOnTop)
            {
                Color color = Color * OnTopColorMul;
                float depthScale = ToAlwaysOnTop(ref wm);
                thick *= depthScale;
                MySimpleObjectDraw.DrawTransparentBox(ref wm, ref LocalBox, ref color, Style, 1, thick, MaterialSquare, MaterialSquare, blendType: BlendType);
            }
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawOBB : DebugDrawBillboardBase
    {
        MatrixD WorldMatrix;
        BoundingBoxD LocalBox;
        Color Color;
        float Thickness;
        MySimpleObjectRasterizer Style;
        MyConcurrentPool<DrawOBB> Pool;

        public void Init(MyConcurrentPool<DrawOBB> pool, MyOrientedBoundingBoxD obb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            LocalBox = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            WorldMatrix = MatrixD.CreateFromQuaternion(obb.Orientation);
            WorldMatrix.Translation = obb.Center;
            Color = color;
            Style = (MySimpleObjectRasterizer)style;
            Thickness = thickness;
        }

        public override void Update()
        {
            MatrixD wm = WorldMatrix;
            float thick = Thickness;
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref LocalBox, ref Color, Style, 1, thick, MaterialSquare, MaterialSquare, blendType: BlendType);

            if(DrawOnTop)
            {
                Color color = Color * OnTopColorMul;
                float depthScale = ToAlwaysOnTop(ref wm);
                thick *= depthScale;
                MySimpleObjectDraw.DrawTransparentBox(ref wm, ref LocalBox, ref color, Style, 1, thick, MaterialSquare, MaterialSquare, blendType: BlendType);
            }
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawSphere : DebugDrawBillboardBase
    {
        MatrixD WorldMatrix;
        Color Color;
        float Radius;
        float Thickness;
        MySimpleObjectRasterizer Style;
        int WireDivide;
        MyConcurrentPool<DrawSphere> Pool;

        const int LinesPerDegreeLimit = 10;

        static List<Vector3> TempVertices = new List<Vector3>();

        public void Init(MyConcurrentPool<DrawSphere> pool, BoundingSphereD sphere, Color color, int style, float thickness, float lineEveryDegrees, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            WorldMatrix = MatrixD.CreateTranslation(sphere.Center);
            Radius = (float)sphere.Radius;
            Color = color;
            Thickness = thickness;
            Style = (MySimpleObjectRasterizer)style;
            WireDivide = (int)MathHelper.Clamp(360 / lineEveryDegrees, 1, 360 * LinesPerDegreeLimit);
        }

        public override void Update()
        {
            MatrixD wm = WorldMatrix;
            float radius = Radius;
            float thick = Thickness;

            TempVertices.Clear();
            GenerateHalfSphereLocal(WireDivide, TempVertices);

            RenderSphere(TempVertices, wm, radius, Color, thick, Style);

            if(DrawOnTop)
            {
                float depthScale = ToAlwaysOnTop(ref wm);
                thick *= depthScale;
                // radius does not need scaling because worldmatrix is already
                RenderSphere(TempVertices, wm, radius, Color * OnTopColorMul, thick, Style);
            }
        }

        static void RenderSphere(List<Vector3> localVerts, MatrixD wm, float radius, Color color, float thick, MySimpleObjectRasterizer style)
        {
            // HACK: cloned DrawTransparentSphere() to remove the length > 0.1 check on lines, making it unable to work with DrawOnTop

            Vector3D vctPos = Vector3D.Zero;
            MyQuadD quad;

            for(int i = 0; i < 2; i++) // two halves
            {
                bool flip = i != 0;

                for(int v = 0; v < localVerts.Count; v += 4)
                {
                    Vector3 v0 = localVerts[v + 1] * radius;
                    Vector3 v1 = localVerts[v + 3] * radius;
                    Vector3 v2 = localVerts[v + 2] * radius;
                    Vector3 v3 = localVerts[v] * radius;

                    if(flip)
                    {
                        v0.Y = -v0.Y;
                        v1.Y = -v1.Y;
                        v2.Y = -v2.Y;
                        v3.Y = -v3.Y;
                    }

                    quad.Point0 = Vector3D.Transform(v0, wm);
                    quad.Point1 = Vector3D.Transform(v1, wm);
                    quad.Point2 = Vector3D.Transform(v2, wm);
                    quad.Point3 = Vector3D.Transform(v3, wm);

                    if(style == MySimpleObjectRasterizer.Solid || style == MySimpleObjectRasterizer.SolidAndWireframe)
                    {
                        MyTransparentGeometry.AddQuad(MaterialSquare, ref quad, color, ref vctPos, blendType: BlendType);
                    }

                    if(style == MySimpleObjectRasterizer.Wireframe || style == MySimpleObjectRasterizer.SolidAndWireframe)
                    {
                        {
                            Vector3D point = quad.Point0;
                            Vector3 dir = quad.Point1 - point;
                            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color, point, dir, 1f, thick, blendType: BlendType);
                        }
                        {
                            Vector3D point = quad.Point1;
                            Vector3 dir = quad.Point2 - point;
                            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color, point, dir, 1f, thick, blendType: BlendType);
                        }
                    }
                }
            }
        }

        static void GenerateHalfSphereLocal(int steps, List<Vector3> vertices)
        {
            vertices.Clear();

            double angleStep = MathHelperD.ToRadians(360d / steps);
            double ang1max = MathHelperD.PiOver2 - angleStep;
            double ang2max = MathHelperD.TwoPi - angleStep;
            Vector3D vec;

            for(double ang1 = 0d; ang1 <= ang1max; ang1 += angleStep)
            {
                double ang1sin = Math.Sin(ang1);
                double ang1cos = Math.Cos(ang1);

                for(double ang2 = 0d; ang2 <= ang2max; ang2 += angleStep)
                {
                    double ang2sin = Math.Sin(ang2);
                    double ang2cos = Math.Cos(ang2);

                    double nextAng1sin = Math.Sin(ang1 + angleStep);
                    double nextAng1cos = Math.Cos(ang1 + angleStep);

                    double nextAng2sin = Math.Sin(ang2 + angleStep);
                    double nextAng2cos = Math.Cos(ang2 + angleStep);

                    vec.X = ang2sin * ang1sin;
                    vec.Y = ang1cos;
                    vec.Z = ang2cos * ang1sin;
                    vertices.Add(vec);

                    vec.X = ang2sin * nextAng1sin;
                    vec.Y = nextAng1cos;
                    vec.Z = ang2cos * nextAng1sin;
                    vertices.Add(vec);

                    vec.X = nextAng2sin * ang1sin;
                    vec.Y = ang1cos;
                    vec.Z = nextAng2cos * ang1sin;
                    vertices.Add(vec);

                    vec.X = nextAng2sin * nextAng1sin;
                    vec.Y = nextAng1cos;
                    vec.Z = nextAng2cos * nextAng1sin;
                    vertices.Add(vec);
                }
            }
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawMatrix : DebugDrawBillboardBase
    {
        MatrixD Matrix;
        float Length;
        float Thickness;
        MyConcurrentPool<DrawMatrix> Pool;

        public void Init(MyConcurrentPool<DrawMatrix> pool, MatrixD matrix, float length, float thickness, float seconds, bool onTop)
        {
            DrawOnTop = onTop;
            LiveSeconds = seconds;
            Pool = pool;

            Matrix = matrix;
            Length = length;
            Thickness = thickness;
        }

        public override void Update()
        {
            MatrixD wm = Matrix;
            float thick = Thickness;
            float len = Length;
            Render(wm, len, thick);

            if(DrawOnTop)
            {
                float depthScale = ToAlwaysOnTop(ref wm);
                thick *= depthScale;
                // len is already affected by matrix scale, no need to be rescaled
                Render(wm, len, thick, OnTopColorMul);
            }
        }

        static void Render(MatrixD wm, float len, float thick, float colorMul = 1f)
        {
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red * colorMul, wm.Translation, wm.Right, len, thick, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Green * colorMul, wm.Translation, wm.Up, len, thick, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Blue * colorMul, wm.Translation, wm.Backward, len, thick, blendType: BlendType);

            float negativeAxisOpacity = 0.2f * colorMul;
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red * negativeAxisOpacity, wm.Translation, wm.Left, len, thick, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Green * negativeAxisOpacity, wm.Translation, wm.Down, len, thick, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Blue * negativeAxisOpacity, wm.Translation, wm.Forward, len, thick, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawGPS : DebugObjectBase, IDrawObject
    {
        IMyGps GPS;
        MyConcurrentPool<DrawGPS> Pool;

        public void Init(MyConcurrentPool<DrawGPS> pool, string name, Vector3D origin, Color? color, float seconds)
        {
            LiveSeconds = seconds;
            Pool = pool;

            GPS = MyAPIGateway.Session.GPS.Create(name, string.Empty, origin, showOnHud: true, temporary: false);
            GPS.GPSColor = color ?? Color.White;

            if(seconds > 0)
                GPS.DiscardAt = MyAPIGateway.Session.ElapsedPlayTime + TimeSpan.FromSeconds(seconds);

            GPS.UpdateHash();
            MyAPIGateway.Session.GPS.AddLocalGps(GPS);
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
            MyAPIGateway.Session.GPS.RemoveLocalGps(GPS);
            GPS = null;

            Pool.Return(this);
            Pool = null;
        }
    }

    public class PrintHUD : DebugObjectBase, IDrawObject
    {
        IMyHudNotification Notification;
        MyConcurrentPool<PrintHUD> Pool;

        public void Init(MyConcurrentPool<PrintHUD> pool, string message, string font, float seconds)
        {
            LiveSeconds = seconds;
            Pool = pool;

            MyDefinitionBase fontDef = MyDefinitionManager.Static.GetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_FontDefinition), font));
            if(fontDef == null)
            {
                //Log.Error($"Font '{font}' does not exist, reverting to 'Debug'.");
                font = "Debug";
            }

            // TODO maybe escape [ and ] ?

            int ms = (int)(seconds * 1000);
            Notification = MyAPIGateway.Utilities.CreateNotification(message, ms, font);
            Notification.Show();
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
            Notification?.Hide();
            Notification = null;

            Pool.Return(this);
            Pool = null;
        }
    }

    // TODO: have a command that shows all currently monitored inputs, their name and value, for easy screenshot ref or something
    public class AdjustNumber : DebugObjectBase
    {
        public double Value;

        string Label;
        double Step;
        MyKeys KeyModifier;

        IMyHudNotification Notification;
        MyConcurrentPool<AdjustNumber> Pool;

        static bool LearnedScroll = false;

        public void Init(MyConcurrentPool<AdjustNumber> pool, string label, double initial, double step, string keyModifier)
        {
            if(keyModifier.StartsWith("Mouse"))
                keyModifier = keyModifier.Substring("Mouse".Length);

            if(!Enum.TryParse<MyKeys>(keyModifier, out KeyModifier))
            {
                pool.Return(this);
                throw new Exception($"Unknown key name: {keyModifier}");
            }

            LiveSeconds = -1;
            Pool = pool;
            Label = label;
            Value = initial;
            Step = step;
            Notification = MyAPIGateway.Utilities.CreateNotification(string.Empty, 300, "Debug");
        }

        public override void Update()
        {
            if(!MyAPIGateway.Input.IsKeyPress(KeyModifier))
                return;

            int scroll = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
            if(scroll != 0)
            {
                LearnedScroll = true;

                if(scroll > 0)
                    Value += Step;
                else
                    Value -= Step;
            }

            string scrollHint = (!LearnedScroll ? " (mouse scroll to adjust)" : "");

            Value = Math.Round(Value, 10);

            Notification.Hide();
            Notification.Text = $"{Label} = {Value.ToString("0.##########")}{scrollHint}";
            Notification.Show();
        }

        public override void Dispose()
        {
            Notification?.Hide();
            Notification = null;

            Pool.Return(this);
            Pool = null;
        }
    }
}