using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WV2ScalingWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<CoreWebView2Controller> ControllerList;
        Stopwatch watch = new Stopwatch();

        //TODO: Remove WV2
        //TODO: UDFs
        //TODO: Handle resize
        public MainWindow()
        {
            Environment.SetEnvironmentVariable("COREWEBVIEW2_FORCED_HOSTING_MODE", "COREWEBVIEW2_HOSTING_MODE_WINDOW_TO_VISUAL");
            InitializeComponent();
            Application.Current.MainWindow.SizeChanged += WindowResized;
            ControllerList = new List<CoreWebView2Controller>();
        }

        private void WindowResized(object sender, SizeChangedEventArgs e)
        {
            RearrangeWV2s();
        }

        async void MeasureNewWV2()
        {
            watch.Reset();
            watch.Start();
            creationList.Items.Add($"Creation starting: {watch.ElapsedMilliseconds}");
            await InitializeNewWV2Async();
            //watch.Stop();

            creationList.Items.Add($"Creation completed: {watch.ElapsedMilliseconds}");
            
            await UpdateMemoryUsage();
        }

        public async Task InitializeNewWV2Async()
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            CoreWebView2EnvironmentOptions envOpts = new CoreWebView2EnvironmentOptions();

            /*
             * 
             * Option #2 - Process per site. Better perf, worse isolation.
             * 
             */
            //envOpts.AdditionalBrowserArguments = "--process-per-site";


            /*
             * 
             * Option #3 - A UDF per WV2. Worst perf, best isolation.
             * 
             */
            //TODO: Figure out how to get where UDF default is.
            //CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, envOpts);
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, envOpts);
            
            CoreWebView2ControllerOptions controllerOpts = environment.CreateCoreWebView2ControllerOptions();

            /*
             * 
             * Option #4 - A profile per WV2. Worse perf, better isolation.
             * 
             */
            //controllerOpts.ProfileName = "Profile" + ControllerList.Count;


            CoreWebView2Controller controller = await environment.CreateCoreWebView2ControllerAsync(
                windowHandle,
                controllerOpts
            );

            controller.Bounds = GetNewWV2Location(ControllerList.Count);
            RearrangeWV2s();
            controller.IsVisible = true;
            await controller.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            const observer = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                const lastEntry = entries[entries.length - 1];
                window.chrome.webview.postMessage(""LCP: "" + lastEntry.startTime);
            });
            observer.observe({ type: ""largest-contentful-paint"", buffered: true });
            ");
            controller.CoreWebView2.WebMessageReceived += MessageReceived;
            controller.CoreWebView2.NavigationCompleted += NavigationCompleted;
            controller.CoreWebView2.Navigate(addressBar.Text);

            ControllerList.Add(controller);
        }

        private void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            creationList.Items.Add($"LCP Event: {watch.ElapsedMilliseconds}");
        }

        private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            creationList.Items.Add($"Navigation completed: {watch.ElapsedMilliseconds}");
        }

        private async Task SuspendWebViews()
        {
            ControllerList.ForEach(async controller =>
            {
                controller.IsVisible = false;
                await controller.CoreWebView2.TrySuspendAsync();
            });
        }

        private async Task ResumeWebViews()
        {
            ControllerList.ForEach(controller =>
            {
                controller.IsVisible = true;
                controller.CoreWebView2.Resume();
            });
        }

        //TODO: Handle profiles and new UDFs
        private async Task UpdateMemoryUsage()
        {
            processList.Items.Clear();
            processList.Items.Add("Loading...");
            await Task.Delay(1000);
            processList.Items.Clear();

            List<CoreWebView2ProcessInfo> _processList = new List<CoreWebView2ProcessInfo>();
            for (int wvNumber = 0; wvNumber < ControllerList.Count; wvNumber++)
            {
                CoreWebView2Environment env = ControllerList[wvNumber].CoreWebView2.Environment;
                _processList.AddRange(env.GetProcessInfos());
            }

            int processListCount = _processList.Count;
            long runningTotal = 0;

            if (processListCount == 0)
            {
                processList.Items.Add("No process found.");
            }
            else
            {
                foreach(CoreWebView2ProcessInfo procObj in _processList.DistinctBy(x=>x.ProcessId))
                {
                    var proc = Process.GetProcessById(procObj.ProcessId);
                    //TODO: This can fail
                    var memoryInBytes = proc.PrivateMemorySize64;
                    var b2mb = memoryInBytes / 1024 / 1024;
                    processList.Items.Add($"{b2mb} MB - {procObj.Kind} ({procObj.ProcessId})");
                    runningTotal += b2mb;
                }
            }
            processList.Items.Add($"{runningTotal} MB - Total");

        }

        public async Task RearrangeWV2s()
        {
            for (int i = 0; i <= ControllerList.Count+1; i++)
            {
                ControllerList[i].Bounds = GetNewWV2Location(i);
            }
        }

        private System.Drawing.Rectangle GetNewWV2Location(int wvNumber)
        {
            int totalWV2Count = ControllerList.Count+1;
            int sqrt = (int)Math.Ceiling(Math.Sqrt(totalWV2Count));

            int newWidth = (int)Math.Floor((Width-300) / sqrt);
            int newHeight = (int)Math.Floor((Height-32) / sqrt);
            int left = (wvNumber % sqrt) * newWidth;
            int top = (wvNumber / sqrt) * newHeight;
            
            return new System.Drawing.Rectangle(left+300, top, newWidth, newHeight);
        }

        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            MeasureNewWV2();
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateMemoryUsage();
        }

        private async void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            await SuspendWebViews();
            UpdateMemoryUsage();
        }

        private async void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            await ResumeWebViews();
            UpdateMemoryUsage();
        }
    }
}

/*
 * 400,400 Window
 * l, r, h, w, h
 * 1: 0,0,400,400
 * 2: 
 *      1: 0, 0,200,400 - 0,0, 
 *      2: 200, 0, 200, 400
 * 3:
 *      1: 0, 0, 200, 200
 *      2: 200, 0, 200, 200
 *      3: 0, 200, 200, 200
 * 4:
 *      1: 0, 0, 200, 200
 *      2: 200, 0, 200, 200
 *      3: 0, 200, 200, 200
 *      4: 200, 200, 200, 200
 * 5:
 *      1: 0, 0, 100, 200
 *      2: 100, 0, 100, 200
 *      3: 200, 0, 100, 200
 *      4: 


//TODO: There's a third case where width needs to be /2 when the current grid size is full.
if (LastWV.Bounds.Height > LastWV.Bounds.Width)
{
    newWidth = LastWV.Bounds.Width;
    newHeight = LastWV.Bounds.Height/2;
}
else
{
    newWidth = LastWV.Bounds.Height;
    newHeight = LastWV.Bounds.Height;
}



 * 
 * 
 *                     newWidth = LastWV.Bounds.Height;
    newHeight = LastWV.Bounds.Height;

*/