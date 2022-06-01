using System;
using System.Collections.Generic;
using System.Text;
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
    // TODO: rename file to DebugObjects

    public interface IDrawObject { } // categorization for RemoveDraw()

    public abstract class DebugObjectBase
    {
        public float LiveSeconds;

        public abstract void Update();

        public abstract void Dispose();
    }

    public abstract class DebugDrawBillboardBase : DebugObjectBase, IDrawObject
    {
        protected BlendTypeEnum BlendType;

        protected static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");
    }

    public class DrawPoint : DebugDrawBillboardBase
    {
        Vector3D Origin;
        Color Color;
        float Radius;
        MyConcurrentPool<DrawPoint> Pool;

        public void Init(MyConcurrentPool<DrawPoint> pool, Vector3D origin, Color color, float radius, float seconds, bool onTop)
        {
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
            LiveSeconds = seconds;
            Pool = pool;

            Origin = origin;
            Color = color;
            Radius = radius;
        }

        public override void Update()
        {
            MyTransparentGeometry.AddPointBillboard(MaterialDot, Color, Origin, Radius, 0, blendType: BlendType);
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
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
            LiveSeconds = seconds;
            Pool = pool;

            From = from;
            Direction = (to - from);
            Color = color;
            Thickness = thickness;
        }

        public override void Update()
        {
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color, From, Direction, 1f, Thickness, blendType: BlendType);
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
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
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
            MySimpleObjectDraw.DrawTransparentBox(ref WorldMatrix, ref LocalBox, ref Color, Style, 1, Thickness, MaterialSquare, MaterialSquare, blendType: BlendType);
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
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
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
            MySimpleObjectDraw.DrawTransparentBox(ref WorldMatrix, ref LocalBox, ref Color, Style, 1, Thickness, MaterialSquare, MaterialSquare, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawSphere : DebugDrawBillboardBase
    {
        MatrixD Matrix;
        Color Color;
        float Radius;
        float Thickness;
        MySimpleObjectRasterizer Style;
        int WireDivide;
        MyConcurrentPool<DrawSphere> Pool;

        public void Init(MyConcurrentPool<DrawSphere> pool, BoundingSphereD sphere, Color color, int style, float thickness, float lineEveryDegrees, float seconds, bool onTop)
        {
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
            LiveSeconds = seconds;
            Pool = pool;

            Matrix = MatrixD.CreateTranslation(sphere.Center);
            Radius = (float)sphere.Radius;
            Color = color;
            Thickness = thickness;
            Style = (MySimpleObjectRasterizer)style;
            WireDivide = (int)MathHelper.Clamp(360 / lineEveryDegrees, 1, 360 * 10); // capped to 10 lines per degree
        }

        public override void Update()
        {
            MySimpleObjectDraw.DrawTransparentSphere(ref Matrix, Radius, ref Color, Style, WireDivide, MaterialSquare, MaterialSquare, Thickness, blendType: BlendType);
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
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);
            LiveSeconds = seconds;
            Pool = pool;

            Matrix = matrix;
            Length = length;
            Thickness = thickness;
        }

        public override void Update()
        {
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red, Matrix.Translation, Matrix.Right, Length, Thickness, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Green, Matrix.Translation, Matrix.Up, Length, Thickness, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Blue, Matrix.Translation, Matrix.Backward, Length, Thickness, blendType: BlendType);

            float negativeAxisOpacity = 0.2f;
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red * negativeAxisOpacity, Matrix.Translation, Matrix.Left, Length, Thickness, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Green * negativeAxisOpacity, Matrix.Translation, Matrix.Down, Length, Thickness, blendType: BlendType);
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Blue * negativeAxisOpacity, Matrix.Translation, Matrix.Forward, Length, Thickness, blendType: BlendType);
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