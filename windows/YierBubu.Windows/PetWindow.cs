using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
namespace YierBubu.Windows;

public sealed class PetWindow:Window
{
    private const double BaseWidth=300,BaseHeight=200;
    public const double MinScale=0.5,MaxScale=2;
    public double Scale {get;private set;}=1;
    public bool ClickThrough {get;private set;}
    public bool HasPosition=>new WindowInteropHelper(this).Handle!=0;
    public event Action? ScaleCommitted;
    private Point? resizeOrigin;
    private double resizeBase;
    private (double Left,double Top)? pendingPosition;
    public PetWindow(PetView view)
    {
        Title="一二布布";Width=BaseWidth;Height=BaseHeight;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
        AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;Focusable=false;Content=view;
        Left=SystemParameters.WorkArea.X+SystemParameters.WorkArea.Width-324;Top=SystemParameters.WorkArea.Y+SystemParameters.WorkArea.Height-224;
        SourceInitialized+=(_,_)=>{UpdateStyles();ApplyInitialPosition();};
        MouseLeftButtonDown+=OnMouseDown;MouseMove+=OnMouseMove;MouseLeftButtonUp+=OnMouseUp;MouseWheel+=OnMouseWheel;
    }
    private nint Handle=>new WindowInteropHelper(this).Handle;
    private void UpdateStyles()
    {
        if(Handle==0)return;
        long style=NativeMethods.GetWindowLongPtr(Handle,-20).ToInt64()|0x08000000|0x80; // NOACTIVATE | TOOLWINDOW
        style=ClickThrough?style|0x20:style&~0x20;
        NativeMethods.SetWindowLongPtr(Handle,-20,new nint(style));
        NativeMethods.SetWindowPos(Handle,0,0,0,0,0,0x0037); // FRAMECHANGED | NOMOVE | NOSIZE | NOZORDER | NOACTIVATE
    }
    public void SetClickThrough(bool enabled){ClickThrough=enabled;UpdateStyles();}
    public void RestorePosition(double left,double top)=>pendingPosition=(left,top);
    private void ApplyInitialPosition()
    {
        if(pendingPosition is {} pending&&IsReachable(pending.Left,pending.Top,Width,Height)){Left=pending.Left;Top=pending.Top;return;}
        ResetPosition();
    }
    private static bool IsReachable(double left,double top,double width,double height)
    {
        double right=SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth,bottom=SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight;
        return left+width>SystemParameters.VirtualScreenLeft+40&&left<right-40&&top+height>SystemParameters.VirtualScreenTop+40&&top<bottom-40;
    }
    private void OnMouseDown(object sender,MouseButtonEventArgs e)
    {
        if(ClickThrough)return;
        if((Keyboard.Modifiers&ModifierKeys.Control)!=0){resizeOrigin=PointToScreen(e.GetPosition(this));resizeBase=Scale;CaptureMouse();e.Handled=true;return;}
        if(e.ButtonState==MouseButtonState.Pressed){DragMove();e.Handled=true;}
    }
    private void OnMouseMove(object sender,MouseEventArgs e)
    {
        if(resizeOrigin is not {} origin||e.LeftButton!=MouseButtonState.Pressed)return;
        double delta=(PointToScreen(e.GetPosition(this)).X-origin.X)/VisualTreeHelper.GetDpi(this).DpiScaleX;
        SetScale(resizeBase+delta/BaseWidth);
    }
    private void OnMouseUp(object sender,MouseButtonEventArgs e)
    {
        if(resizeOrigin is null)return;
        resizeOrigin=null;ReleaseMouseCapture();ScaleCommitted?.Invoke();e.Handled=true;
    }
    private void OnMouseWheel(object sender,MouseWheelEventArgs e)
    {
        if((Keyboard.Modifiers&ModifierKeys.Control)==0)return;
        SetScale(Scale+(e.Delta>0?0.05:-0.05));ScaleCommitted?.Invoke();e.Handled=true;
    }
    /// Any scale in [MinScale, MaxScale] is accepted, including values from the slider and Ctrl+drag.
    /// Resizing anchors the bottom-right corner so a custom position is never lost.
    public void SetScale(double value)
    {
        value=Math.Clamp(Math.Round(value,3),MinScale,MaxScale);
        if(Math.Abs(value-Scale)<0.0005)return;
        double right=Left+Width,bottom=Top+Height;
        Scale=value;Width=BaseWidth*value;Height=BaseHeight*value;
        if(Handle!=0){Left=right-Width;Top=bottom-Height;}
    }
    public void ResetPosition()
    {
        if(Handle==0)return;
        var info=new NativeMethods.MonitorInfo{Size=Marshal.SizeOf<NativeMethods.MonitorInfo>()};
        if(!NativeMethods.GetMonitorInfo(NativeMethods.MonitorFromWindow(Handle,2),ref info))return;
        double factor=Math.Max(96,NativeMethods.GetDpiForWindow(Handle))/96.0;
        var point=WindowPlacement.BottomRight(new(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top),Width*factor,Height*factor,24*factor);
        NativeMethods.SetWindowPos(Handle,0,(int)point.X,(int)point.Y,0,0,0x0015); // NOSIZE | NOZORDER | NOACTIVATE
    }
}