namespace DwmThumbnailTest
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Memorandum.Drawing;
    using Memorandum.UI;

    public class Form1 : Form
    {
        protected static readonly Func<Window, bool> DefaultCaptureTarget = w => w.Visible && !w.Rectangle.Size.IsEmpty && w.Text.Length != 0;

        protected static readonly Size DefaultThumbnailSize = new Size(150, 150);

        private readonly Dictionary<IntPtr, ListViewItem> capturedSources;

        private readonly List<ListViewItem> virtualListViewItems;

        public Form1()
        {
            this.InitializeComponent();

            this.capturedSources = new Dictionary<IntPtr, ListViewItem>();
            this.virtualListViewItems = new List<ListViewItem>();
            this.CaptureTarget = DefaultCaptureTarget;

            this.ListView1.CacheVirtualItems += (s, e) =>
            {
                this.UpdateListViewItems();
            };

            this.ListView1.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.F5)
                {
                    this.ListView1.SuspendLayout();
                    this.ClearListViewItems();
                    this.CaptureThumbnailSources(this.CaptureTarget);
                    this.ListView1.ResumeLayout();
                }
            };

            this.ListView1.RetrieveVirtualItem += (s, e) =>
            {
                e.Item = this.virtualListViewItems[e.ItemIndex];
            };
        }

        public Size ThumbnailSize { get; private set; }

        protected Func<Window, bool> CaptureTarget { get; private set; }

        protected ListView ListView1 { get; private set; }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.CaptureThumbnailSources(DefaultCaptureTarget);
        }

        protected void AddThumbnail(Window source)
        {
            var thumbnail = new DwmThumbnail(this.Handle, source.Handle);
            if (thumbnail.Update(Rectangle.Empty, Rectangle.Empty, false))
            {
                const int length = 20;
                var text = source.Text.Substring(
                    0,
                    source.Text.Length < length ? source.Text.Length : length);
                var li = new ListViewItem
                {
                    Tag = thumbnail,
                    Text = text,
                    ImageIndex = 0,
                };
                this.capturedSources.Add(source.Handle, li);
                this.virtualListViewItems.Add(li);
                this.ListView1.VirtualListSize = this.capturedSources.Count;
            }
        }

        protected void CaptureThumbnailSources(Func<Window, bool> filter)
        {
            Window.Enumerate()
                .Where(x => filter(x))
                .Where(x => !this.capturedSources.ContainsKey(x.Handle))
                .ToList()
                .ForEach(x => this.AddThumbnail(x));
        }

        protected void ClearListViewItems()
        {
            this.virtualListViewItems.Cast<ListViewItem>()
                .ToList()
                .ForEach(x => ((DwmThumbnail)x.Tag).Dispose());
            this.capturedSources.Clear();
            this.virtualListViewItems.Clear();
            this.ListView1.VirtualListSize = 0;
        }

        protected void RemoveThumbnail(DwmThumbnail thumbnail)
        {
            var item = this.capturedSources[thumbnail.SourceHwnd];
            this.virtualListViewItems.Remove(item);
            this.capturedSources.Remove(thumbnail.SourceHwnd);
            this.ListView1.VirtualListSize = this.capturedSources.Count;
            thumbnail.Dispose();
        }

        protected void UpdateListViewItems()
        {
            var items = this.virtualListViewItems.ToArray();
            foreach (var item in items)
            {
                var itemRect = item.GetBounds(ItemBoundsPortion.Icon);
                var visible = itemRect.IntersectsWith(this.ListView1.ClientRectangle);
                this.UpdateThumbnail((DwmThumbnail)item.Tag, itemRect, visible);
            }
        }

        protected void UpdateThumbnail(DwmThumbnail thumbnail, Rectangle itemRect, bool visible)
        {
            var displayRect = default(Rectangle);
            var clippedSourceRect = default(Rectangle);

            if (visible)
            {
                var sourceRect = new Rectangle(new Point(0, 0), thumbnail.QuerySourceSize());
                var destinationRect = sourceRect.Zoom(this.ThumbnailSize)
                    .Centering(itemRect);
                displayRect = Rectangle.Intersect(destinationRect, this.ListView1.ClientRectangle);
                try
                {
                    var scale = (float)sourceRect.Width / destinationRect.Width;
                    clippedSourceRect = new Rectangle(
                        (int)((displayRect.X - destinationRect.X) * scale),
                        (int)((displayRect.Y - destinationRect.Y) * scale),
                        (int)(displayRect.Width * scale),
                        (int)(displayRect.Height * scale));
                }
                catch (DivideByZeroException)
                {
                }
            }

            thumbnail.Update(displayRect, clippedSourceRect, visible);
            if (thumbnail.Failed)
            {
                this.RemoveThumbnail(thumbnail);
            }
        }

        private void InitializeComponent()
        {
            this.Text = Application.ProductName;
            this.ThumbnailSize = DefaultThumbnailSize;

            this.ListView1 = new ListView
            {
                Dock = DockStyle.Fill,
                LargeImageList = new ImageList
                {
                    ImageSize = this.ThumbnailSize,
                },
                View = View.LargeIcon,
                VirtualMode = true,
            };

            var dummyImage = new Bitmap(this.ThumbnailSize.Width, this.ThumbnailSize.Height);
            using (var g = Graphics.FromImage(dummyImage))
            {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, dummyImage.Width, dummyImage.Height));
            }

            this.ListView1.LargeImageList.Images.Add(dummyImage);

            this.Controls.AddRange(new Control[] { this.ListView1 });
        }
    }
}