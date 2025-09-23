// IUuIS_PZ2/Views/DisplayView.xaml.cs
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using IUuIS_PZ2.Models;
using IUuIS_PZ2.ViewModels;

namespace IUuIS_PZ2.Views
{
    public partial class DisplayView : UserControl
    {
        public DisplayView()
        {
            InitializeComponent();
            Loaded += (_, __) => RedrawLinks();
            SizeChanged += (_, __) => RedrawLinks();
        }

        private void Entity_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is ListBox lb && lb.SelectedItem is DerEntity ent)
            {
                DragDrop.DoDragDrop(lb, ent, DragDropEffects.Copy);
            }
        }

        private void Slot_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not DisplayViewModel vm) return;

            // prihvati i iz Tree (Copy) i iz kartice (Move)
            if (!e.Data.GetDataPresent(typeof(DerEntity))) return;

            var ent = (DerEntity)e.Data.GetData(typeof(DerEntity))!;
            int slotIndex = (int)((Border)sender).Tag;
            vm.AddToSlot(slotIndex, ent);
            RedrawLinks();
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not DisplayViewModel vm) return;
            if (sender is Border b && b.DataContext is DisplaySlot slot && slot.Occupant != null)
            {
                vm.ConnectClick(slot.Occupant.Id);
                RedrawLinks();
            }
        }

        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is Border b && b.DataContext is DisplaySlot slot && slot.Occupant != null)
            {
                // drag kartice u drugi slot
                DragDrop.DoDragDrop(b, slot.Occupant, DragDropEffects.Move);
            }
        }

        private void RedrawLinks()
        {
            if (DataContext is not DisplayViewModel vm) return;

            LinkCanvas.Children.Clear();

            var grid = FindVisualChild<UniformGrid>(SlotsItems);
            if (grid == null) return;

            double w = grid.ActualWidth;
            double h = grid.ActualHeight;
            if (w <= 0 || h <= 0) return;

            int cols = 4, rows = 3;
            double cellW = w / cols;
            double cellH = h / rows;

            Point Center(int slotIndex)
            {
                int col = (slotIndex - 1) % cols;
                int row = (slotIndex - 1) / cols;
                return new Point((col + 0.5) * cellW, (row + 0.5) * cellH);
            }

            int? FindSlotByEntityId(int id)
            {
                var s = vm.Slots.FirstOrDefault(x => x.Occupant?.Id == id);
                return s?.Index;
            }

            foreach (var link in vm.Links)
            {
                var a = FindSlotByEntityId(link.SourceId);
                var b = FindSlotByEntityId(link.TargetId);
                if (a == null || b == null) continue;

                var p1 = Center(a.Value);
                var p2 = Center(b.Value);

                // linija
                var line = new System.Windows.Shapes.Line
                {
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Opacity = 0.85
                };
                LinkCanvas.Children.Add(line);

                // strelica (poligon) usmerena ka p2
                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;
                var angle = Math.Atan2(dy, dx) * 180 / Math.PI;

                var arrow = new System.Windows.Shapes.Polygon
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0),
                        new Point(-10, -4),
                        new Point(-10,  4)
                    },
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    Opacity = 0.85,
                    RenderTransform = new TransformGroup
                    {
                        Children = new TransformCollection
                        {
                            new RotateTransform(angle),
                            new TranslateTransform(p2.X, p2.Y)
                        }
                    }
                };
                LinkCanvas.Children.Add(arrow);
            }
        }

        private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var deep = FindVisualChild<T>(child);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}