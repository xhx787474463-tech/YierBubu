using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace YierBubu.Windows;

public sealed class StatusWindow:Window
{
    private readonly PetWindow window;
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,FontSize=15,Margin=new Thickness(0,10,0,10)};
    private readonly TextBlock counts=new(){TextWrapping=TextWrapping.Wrap,FontSize=15,Margin=new Thickness(0,6,0,12)};
    private readonly TextBlock typing=new(){TextWrapping=TextWrapping.Wrap,FontSize=15,Margin=new Thickness(0,0,0,4)};
    private readonly Slider scale=new(){Minimum=PetWindow.MinScale*100,Maximum=PetWindow.MaxScale*100,LargeChange=10,SmallChange=1,TickFrequency=10};
    private readonly TextBlock scaleCaption=new(){FontSize=13,Foreground=Brushes.DimGray,Margin=new Thickness(0,2,0,12)};
    public StatusWindow(PetWindow window,Action recheck,Action scaleCommitted)
    {
        this.window=window;
        Title="一二布布 · 连接状态";Width=440;Height=520;ResizeMode=ResizeMode.CanMinimize;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Brushes.FloralWhite;
        var panel=new StackPanel{Margin=new Thickness(24)};
        panel.Children.Add(new TextBlock{Text="一二布布 · 键鼠连接",FontSize=22,FontWeight=FontWeights.SemiBold});
        panel.Children.Add(status);panel.Children.Add(counts);
        panel.Children.Add(new TextBlock{Text="打字统计（真实按键，本周从周一起算）",FontSize=16,FontWeight=FontWeights.SemiBold});
        panel.Children.Add(typing);
        panel.Children.Add(new TextBlock{Text="小熊大小",FontSize=16,FontWeight=FontWeights.SemiBold});
        scale.Value=window.Scale*100;
        scale.ValueChanged+=(_,_)=>{window.SetScale(scale.Value/100);SyncScaleCaption();};
        scale.AddHandler(Thumb.DragCompletedEvent,new RoutedEventHandler((_,_)=>scaleCommitted()));
        panel.Children.Add(scale);panel.Children.Add(scaleCaption);
        panel.Children.Add(new TextBlock{Text="也可以按住 Ctrl 拖动小熊，或按住 Ctrl 滚动鼠标滚轮调整大小。",TextWrapping=TextWrapping.Wrap,FontSize=12,Foreground=Brushes.DimGray,Margin=new Thickness(0,0,0,12)});
        panel.Children.Add(new TextBlock{Text="请切换到其他软件，按字母、数字、空格或回车，点击和滚动，观察小熊动作与真实计数。动作演示不会增加这些计数。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        var button=new Button{Content="重新连接",Padding=new Thickness(12,6,12,6),HorizontalAlignment=HorizontalAlignment.Left};button.Click+=(_,_)=>recheck();panel.Children.Add(button);
        panel.Children.Add(new TextBlock{Text="仅用物理键位驱动动画，不读取输入文字、不联网。\n打字统计只保存每日汇总次数，不记录按键内容、不保存输入历史。\n普通权限运行；锁屏、安全桌面等场景不会尝试绕过系统限制。",TextWrapping=TextWrapping.Wrap,FontSize=12,Foreground=Brushes.DimGray,Margin=new Thickness(0,18,0,0)});
        SyncScaleCaption();Content=panel;
    }
    public void Update(string value,InputActivity activity,TypingStats stats)
    {
        status.Text=value;
        counts.Text=$"键盘：{activity.KeyPresses}\n左键：{activity.LeftClicks}　右键：{activity.RightClicks}　中键：{activity.MiddleClicks}\n滚动：{activity.ScrollEvents}";
        var today=DateOnly.FromDateTime(DateTime.Now);
        typing.Text=$"今日：{stats.On(today)}　本周：{stats.Since(TypingStats.WeekStart(today))}　本月：{stats.Since(TypingStats.MonthStart(today))}\n累计：{stats.Total}";
        if(Math.Abs(scale.Value/100-window.Scale)>0.0005)scale.Value=window.Scale*100;
        SyncScaleCaption();
    }
    private void SyncScaleCaption()=>scaleCaption.Text=$"当前 {Math.Round(window.Scale*100)}%（50%–200% 自由调整）";
}