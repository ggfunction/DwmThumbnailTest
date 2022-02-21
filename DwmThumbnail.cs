namespace DwmThumbnailTest
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    public class DwmThumbnail : IDisposable
    {
        private bool disposedValue;

        public DwmThumbnail(IntPtr destinationHwnd, IntPtr sourceHwnd)
        {
            this.Register(destinationHwnd, sourceHwnd);
        }

        ~DwmThumbnail()
        {
            this.Dispose(false);
        }

        public Rectangle DestinationBounds { get; private set; }

        public IntPtr DestinationHwnd { get; private set; }

        public int Flags { get; private set; }

        public bool Failed { get; private set; }

        public IntPtr Id { get; private set; }

        public Rectangle SourceBounds { get; private set; }

        public IntPtr SourceHwnd { get; private set; }

        public bool SourceClientAreaOnly { get; private set; }

        public byte Opacity { get; private set; }

        public object Tag { get; set; }

        public bool Visible { get; private set; }

        public virtual void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Size QuerySourceSize()
        {
            Size size;
            NativeMethods.DwmQueryThumbnailSourceSize(this.Id, out size);
            return size;
        }

        public bool Update(Rectangle destinationBounds, Rectangle sourceBounds, bool visible)
        {
            var flags = NativeMethods.DwmThumbnailPropertyFlags.RectDestination |
                NativeMethods.DwmThumbnailPropertyFlags.RectSource |
                NativeMethods.DwmThumbnailPropertyFlags.Visible;

            if (destinationBounds.IsEmpty)
            {
                flags &= ~NativeMethods.DwmThumbnailPropertyFlags.RectDestination;
            }

            if (sourceBounds.IsEmpty)
            {
                flags &= ~NativeMethods.DwmThumbnailPropertyFlags.RectSource;
            }

            return this.Update(
                (int)flags,
                destinationBounds,
                sourceBounds,
                0,
                visible,
                false);
        }

        public bool Update(Rectangle destinationBounds, Rectangle sourceBounds, byte opacity, bool visible, bool sourceClientAreaOnly)
        {
            var flags = NativeMethods.DwmThumbnailPropertyFlags.RectDestination |
                NativeMethods.DwmThumbnailPropertyFlags.RectSource |
                NativeMethods.DwmThumbnailPropertyFlags.Opacity |
                NativeMethods.DwmThumbnailPropertyFlags.Visible |
                NativeMethods.DwmThumbnailPropertyFlags.SourceClientAreaOnly;

            if (destinationBounds.IsEmpty)
            {
                flags &= ~NativeMethods.DwmThumbnailPropertyFlags.RectDestination;
            }

            if (sourceBounds.IsEmpty)
            {
                flags &= ~NativeMethods.DwmThumbnailPropertyFlags.RectSource;
            }

            return this.Update(
                (int)flags,
                destinationBounds,
                sourceBounds,
                opacity,
                visible,
                sourceClientAreaOnly);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                this.Unregister();

                this.disposedValue = true;
            }
        }

        protected void Register(IntPtr destinationHwnd, IntPtr sourceHwnd)
        {
            IntPtr thumbnailId;
            this.Failed = NativeMethods.DwmRegisterThumbnail(destinationHwnd, sourceHwnd, out thumbnailId) != 0;
            this.Id = thumbnailId;
            this.DestinationHwnd = destinationHwnd;
            this.SourceHwnd = sourceHwnd;
        }

        protected void Unregister()
        {
            if (this.Id != IntPtr.Zero)
            {
                this.Failed = NativeMethods.DwmUnregisterThumbnail(this.Id) != 0;
            }
        }

        protected bool Update(
            int flags,
            Rectangle destinationBounds,
            Rectangle sourceBounds,
            byte opacity,
            bool visible,
            bool sourceClientAreaOnly)
        {
            var properties = new NativeMethods.DwmThumbnailProperties
            {
                Flags = flags,
                RectDestination = new NativeMethods.Rect(destinationBounds),
                RectSource = new NativeMethods.Rect(sourceBounds),
                Opacity = opacity,
                Visible = visible,
                SourceClientAreaOnly = sourceClientAreaOnly,
            };

            this.Flags = flags;
            this.Failed = NativeMethods.DwmUpdateThumbnailProperties(this.Id, ref properties) != 0;

            if ((properties.Flags & (int)NativeMethods.DwmThumbnailPropertyFlags.RectDestination) != 0)
            {
                this.DestinationBounds = destinationBounds;
            }

            if ((properties.Flags & (int)NativeMethods.DwmThumbnailPropertyFlags.RectSource) != 0)
            {
                this.SourceBounds = sourceBounds;
            }

            if ((properties.Flags & (int)NativeMethods.DwmThumbnailPropertyFlags.Opacity) != 0)
            {
                this.Opacity = opacity;
            }

            if ((properties.Flags & (int)NativeMethods.DwmThumbnailPropertyFlags.Visible) != 0)
            {
                this.Visible = visible;
            }

            if ((properties.Flags & (int)NativeMethods.DwmThumbnailPropertyFlags.SourceClientAreaOnly) != 0)
            {
                this.SourceClientAreaOnly = sourceClientAreaOnly;
            }

            return !this.Failed;
        }

        private static class NativeMethods
        {
            [Flags]
            public enum DwmThumbnailPropertyFlags : int
            {
                None = 0x00000000,

                RectDestination = 0x00000001,

                RectSource = 0x00000002,

                Opacity = 0x00000004,

                Visible = 0x00000008,

                SourceClientAreaOnly = 0x00000010,
            }

            [DllImport("dwmapi.dll")]
            public static extern int DwmRegisterThumbnail(IntPtr hwndDestination, IntPtr hwndSource, out IntPtr thumbnailId);

            [DllImport("dwmapi.dll")]
            public static extern int DwmUnregisterThumbnail(IntPtr thumbnailId);

            [DllImport("dwmapi.dll")]
            public static extern int DwmQueryThumbnailSourceSize(IntPtr thumbnailId, out Size size);

            [DllImport("dwmapi.dll")]
            public static extern int DwmUpdateThumbnailProperties(IntPtr thumbnailId, ref DwmThumbnailProperties properties);

            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public Rect(int left, int top, int right, int bottom)
                    : this()
                {
                    this.Left = left;
                    this.Top = top;
                    this.Right = right;
                    this.Bottom = bottom;
                }

                public Rect(Rectangle bounds)
                    : this(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)
                {
                }

                public int Left { get; set; }

                public int Top { get; set; }

                public int Right { get; set; }

                public int Bottom { get; set; }

                public int Width
                {
                    get { return this.Right - this.Left; }
                }

                public int Height
                {
                    get { return this.Bottom - this.Top; }
                }

                public Rectangle ToRectangle()
                {
                    return new Rectangle(this.Left, this.Top, this.Width, this.Height);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DwmThumbnailProperties
            {
                public int Flags { get; set; }

                public Rect RectDestination { get; set; }

                public Rect RectSource { get; set; }

                public byte Opacity { get; set; }

                public bool Visible { get; set; }

                public bool SourceClientAreaOnly { get; set; }
            }
        }
    }
}