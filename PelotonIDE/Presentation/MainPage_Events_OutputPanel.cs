using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Core;

namespace PelotonIDE.Presentation
{
    public sealed partial class MainPage : Page
    {
        private void OutputPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            Telemetry.SetEnabled(true);
            Telemetry.Transmit("e.NewSize.Height=", e.NewSize.Height, "e.NewSize.Width=", e.NewSize.Width, "e.PreviousSize.Height=", e.PreviousSize.Height, "e.PreviousSize.Width=", e.PreviousSize.Width);

            string pos = Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition") ?? "Bottom";
            OutputPanelPosition outputPanelPosition = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), pos);
            switch (outputPanelPosition)
            {
                case OutputPanelPosition.Bottom:
                    outputPanel.ClearValue(WidthProperty);
                    outputPanelTabView.Width = outputPanel.ActualWidth;
                    outputPanelTabView.Height = outputPanel.ActualHeight;
                    outputThumb.Width = outputPanel.ActualWidth;
                    outputThumb.Height = 5;
                    break;
                case OutputPanelPosition.Right:
                    outputPanel.ClearValue(HeightProperty);
                    outputPanelTabView.Width = outputPanel.ActualWidth;
                    outputPanelTabView.Height = outputPanel.ActualHeight;
                    outputThumb.Width = 10;
                    outputThumb.Height = outputPanel.ActualHeight;
                    break;
                case OutputPanelPosition.Left:
                    outputPanel.ClearValue(HeightProperty);
                    outputPanelTabView.Width = outputPanel.ActualWidth;
                    outputPanelTabView.Height = outputPanel.ActualHeight;
                    outputThumb.Width = 10;
                    outputThumb.Height = outputPanel.ActualHeight;
                    Canvas.SetLeft(outputThumb, outputPanel.ActualWidth - 1);
                    break;
            }
            //oHW.Text = $"OutputPanel: {e.NewSize.Height}/{e.NewSize.Width}";

        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Telemetry.SetEnabled(false);
            Thumb me = (Thumb)sender;


            OutputPanelPosition outputPanelPosition = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition"));
            double yadjust = outputPanel.Height - e.VerticalChange;
            double xRightAdjust = outputPanel.Width - e.HorizontalChange;
            double xLeftAdjust = outputPanel.Width + e.HorizontalChange;
            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                if (yadjust >= 0)
                {
                    outputPanel.Height = yadjust;
                }
            }
            else if (outputPanelPosition == OutputPanelPosition.Left)
            {
                if (xLeftAdjust >= 0)
                {
                    outputPanel.Width = xLeftAdjust;
                }
            }
            else if (outputPanelPosition == OutputPanelPosition.Right)
            {
                if (xRightAdjust >= 0)
                {
                    outputPanel.Width = xRightAdjust;
                }
            }

            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
        }

        private void OutputThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Telemetry.SetEnabled(true);
            Thumb me = (Thumb)sender;

            Telemetry.Transmit(me.Name, "e.HorizontalChange=", e.HorizontalChange, "e.VerticalChange=", e.VerticalChange, "outputPanel.Width=", outputPanel.Width, "outputPanel.Height=", outputPanel.Height);

            OutputPanelPosition outputPanelPosition = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition"));

            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelHeight", outputPanel.Height);
            }
            else
            {
                Type_1_UpdateVirtualRegistry<double>("ideOps.OutputPanelWidth", outputPanel.Width);
            }

            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));

        }

        private async void OutputThumb_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);

            OutputPanelPosition outputPanelPosition = (OutputPanelPosition)Enum.Parse(typeof(OutputPanelPosition), Type_1_GetVirtualRegistry<string>("ideOps.OutputPanelPosition"));

            if (outputPanelPosition == OutputPanelPosition.Bottom)
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeNorthSouth, 0));
            }
            else
            {
                this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 0));
            }
            Telemetry.Transmit(this.ProtectedCursor);
        }

        private void OutputThumb_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            this.ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
            Telemetry.Transmit(this.ProtectedCursor);
        }

        private void OutputLeft_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            HandleOutputPanelChange("Left");
        }

        private void OutputBottom_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            HandleOutputPanelChange("Bottom");
        }

        private void OutputRight_Click(object sender, RoutedEventArgs e)
        {
            Telemetry.SetEnabled(false);
            HandleOutputPanelChange("Right");
        }


    }
}
