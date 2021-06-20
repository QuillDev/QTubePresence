using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace QTubePresence.Core
{
    public class BrowserTitleGrabber
    {
        private static readonly Condition TabControlCondition =
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab);

        private static readonly Condition TabItemCondition =
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem);

        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        public async Task<IEnumerable<string>> GetTabTitles(EBrowser browser)
        {
            return await Task.Run(() =>
            {
                switch (browser)
                {
                    case EBrowser.Chrome:
                        return GetTabTitles("chrome");
                    case EBrowser.Firefox:
                        return GetTabTitles("firefox");
                    default:
                        throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
                }
            });
        }

        private static IEnumerable<string> GetTabTitles(string procName)
        {
            _cBt = new List<string>();
            foreach (var process in Process.GetProcessesByName(procName))
            {
                foreach (ProcessThread processThread in process.Threads)
                {
                    EnumThreadWindows((uint) processThread.Id, EnumThreadWindowsCb, IntPtr.Zero);
                }
            }

            return _cBt;
        }

        private static List<string> _cBt;

        private static bool EnumThreadWindowsCb(IntPtr hWnd, IntPtr lParam)
        {
            try
            {
                _cBt.AddRange(GetTabTitles(hWnd));
            }
            catch (Exception)
            {
                // ignored
            }

            return true;
        }

        private static IEnumerable<string> GetTabTitles(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return new List<string>();
            var rootElement = AutomationElement.FromHandle(handle);
            if (rootElement == null) return new List<string>();
            var tabControl = rootElement.FindFirst(TreeScope.Descendants, TabControlCondition);
            
            return tabControl == null
                ? new List<string>()
                : (from AutomationElement tab in tabControl.FindAll(TreeScope.Children, TabItemCondition)
                    select tab.Current.Name).ToList();
            
            
        }

        public enum EBrowser
        {
            Chrome,
            Firefox
        }
    }
}