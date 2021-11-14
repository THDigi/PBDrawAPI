using System;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Digi.PBDrawAPI
{
    public interface IDrawObject
    {
        void Draw();
        void Dispose();
    }

    public abstract class DrawObject : IDrawObject
    {
        public int TicksToLive;

        protected BlendTypeEnum BlendType;

        protected static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");

        protected void Init(float seconds, bool onTop)
        {
            BlendType = (onTop ? BlendTypeEnum.AdditiveTop : BlendTypeEnum.Standard);

            if(seconds < 0)
                TicksToLive = -1;
            else
                TicksToLive = (int)(seconds * 60);
        }

        public abstract void Draw();

        public abstract void Dispose();
    }

    public class DrawPoint : DrawObject
    {
        Vector3D Origin;
        Color Color;
        float Radius;
        MyConcurrentPool<DrawPoint> Pool;

        public void Init(MyConcurrentPool<DrawPoint> pool, Vector3D origin, Color color, float radius, float seconds, bool onTop)
        {
            base.Init(seconds, onTop);
            Pool = pool;

            Origin = origin;
            Color = color;
            Radius = radius;
        }

        public override void Draw()
        {
            MyTransparentGeometry.AddPointBillboard(MaterialDot, Color, Origin, Radius, 0, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawLine : DrawObject
    {
        Vector3D From;
        Vector3 Direction;
        Color Color;
        float Thickness;
        MyConcurrentPool<DrawLine> Pool;

        public void Init(MyConcurrentPool<DrawLine> pool, Vector3D from, Vector3D to, Color color, float thickness, float seconds, bool onTop)
        {
            base.Init(seconds, onTop);
            Pool = pool;

            From = from;
            Direction = (to - from);
            Color = color;
            Thickness = thickness;
        }

        public override void Draw()
        {
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color, From, Direction, 1f, Thickness, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawAABB : DrawObject
    {
        MatrixD WorldMatrix;
        BoundingBoxD LocalBox;
        Color Color;
        float Thickness;
        MySimpleObjectRasterizer Style;
        MyConcurrentPool<DrawAABB> Pool;

        public void Init(MyConcurrentPool<DrawAABB> pool, BoundingBoxD bb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            base.Init(seconds, onTop);
            Pool = pool;

            LocalBox = new BoundingBoxD(-bb.HalfExtents, bb.HalfExtents);
            WorldMatrix = MatrixD.CreateTranslation(bb.Center);
            Color = color;
            Style = (MySimpleObjectRasterizer)style;
            Thickness = thickness;
        }

        public override void Draw()
        {
            MySimpleObjectDraw.DrawTransparentBox(ref WorldMatrix, ref LocalBox, ref Color, Style, 1, Thickness, MaterialSquare, MaterialSquare, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawOBB : DrawObject
    {
        MatrixD WorldMatrix;
        BoundingBoxD LocalBox;
        Color Color;
        float Thickness;
        MySimpleObjectRasterizer Style;
        MyConcurrentPool<DrawOBB> Pool;

        public void Init(MyConcurrentPool<DrawOBB> pool, MyOrientedBoundingBoxD obb, Color color, int style, float thickness, float seconds, bool onTop)
        {
            base.Init(seconds, onTop);
            Pool = pool;

            LocalBox = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            WorldMatrix = MatrixD.CreateFromQuaternion(obb.Orientation);
            WorldMatrix.Translation = obb.Center;
            Color = color;
            Style = (MySimpleObjectRasterizer)style;
            Thickness = thickness;
        }

        public override void Draw()
        {
            MySimpleObjectDraw.DrawTransparentBox(ref WorldMatrix, ref LocalBox, ref Color, Style, 1, Thickness, MaterialSquare, MaterialSquare, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawSphere : DrawObject
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
            base.Init(seconds, onTop);
            Pool = pool;

            Matrix = MatrixD.CreateTranslation(sphere.Center);
            Radius = (float)sphere.Radius;
            Color = color;
            Thickness = thickness;
            Style = (MySimpleObjectRasterizer)style;
            WireDivide = (int)MathHelper.Clamp(360 / lineEveryDegrees, 1, 360 * 10); // capped to 10 lines per degree
        }

        public override void Draw()
        {
            MySimpleObjectDraw.DrawTransparentSphere(ref Matrix, Radius, ref Color, Style, WireDivide, MaterialSquare, MaterialSquare, Thickness, blendType: BlendType);
        }

        public override void Dispose()
        {
            Pool.Return(this);
            Pool = null;
        }
    }

    public class DrawMatrix : DrawObject
    {
        MatrixD Matrix;
        float Length;
        float Thickness;
        MyConcurrentPool<DrawMatrix> Pool;

        public void Init(MyConcurrentPool<DrawMatrix> pool, MatrixD matrix, float length, float thickness, float seconds, bool onTop)
        {
            base.Init(seconds, onTop);
            Pool = pool;

            Matrix = matrix;
            Length = length;
            Thickness = thickness;
        }

        public override void Draw()
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

    public class DrawHUDMarker : DrawObject
    {
        IMyGps GPS;
        MyConcurrentPool<DrawHUDMarker> Pool;

        public void Init(MyConcurrentPool<DrawHUDMarker> pool, string name, Vector3D origin, Color color, float seconds)
        {
            base.Init(seconds, false);
            Pool = pool;

            GPS = MyAPIGateway.Session.GPS.Create(name, string.Empty, origin, showOnHud: true, temporary: false);
            GPS.GPSColor = color;

            if(seconds > -1)
                GPS.DiscardAt = MyAPIGateway.Session.ElapsedPlayTime + TimeSpan.FromSeconds(seconds);

            GPS.UpdateHash();

            MyAPIGateway.Session.GPS.AddLocalGps(GPS);
        }

        public override void Draw()
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
}