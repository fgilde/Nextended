using System;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Nextended.UI.Helper
{
    /// <summary>
    /// DispatcherHelper
    /// </summary>
    public static class DispatcherHelper
    {

        /// <summary>
        /// DispatcherHelper class.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents(this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Background)
        {
            var frame = new DispatcherFrame();
            dispatcher.BeginInvoke(priority, new DispatcherOperationCallback(ExitFrames), frame);
            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            { }
        }

        /// <summary>
        /// DispatcherHelper class.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand,Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents(DispatcherPriority priority = DispatcherPriority.Background)
        {
            Dispatcher.CurrentDispatcher.DoEvents(priority);
        }

        /// <summary>
        /// ExitFrames
        /// </summary>
        private static object ExitFrames ( object frame )
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }

        /// <summary>
        /// Um Threadsicher etwas an der GUI zu machen
        /// </summary>
        public static TResult EnsureAccess<TResult>(this Dispatcher dispatcher, Func<TResult> action)
        {
            if (dispatcher == null || dispatcher.CheckAccess())
                return action();

            TResult result = default(TResult);
            Action procAction = () => result = action();
            dispatcher.Invoke(procAction);
            return result;
        }

        /// <summary>
        /// Um Threadsicher etwas an der GUI zu machen
        /// </summary>
        public static void EnsureAccess(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(new MethodInvoker(action));
        }

        /// <summary>
        /// Prüft ob auf dem übergebenem Control Invoke benötigt wird, und führt sie aus
        /// </summary>
        public static void EnsureAccess(this DependencyObject dependencyObject, Action action)
        {
            dependencyObject.Dispatcher.EnsureAccess(action);
        }

        /// <summary>
        /// Prüft ob auf dem übergebenem Control Invoke benötigt wird, und führt sie aus
        /// </summary>
        public static void EnsureAccess<T>(this T dependencyObject, Action<T> action) where T: DependencyObject
        {
            if (dependencyObject == null || dependencyObject.Dispatcher.CheckAccess())
                action(dependencyObject);
            else
            {
                Action a = () => action(dependencyObject);
                dependencyObject.Dispatcher.EnsureAccess(a);
            }
        }

        /// <summary>
        /// Prüft ob auf dem übergebenem Control Invoke benötigt wird, und führt sie aus
        /// </summary>
        public static TResult EnsureAccess<TResult>(this DependencyObject dependencyObject, Func<TResult> action)
        {
            if (dependencyObject == null || dependencyObject.Dispatcher.CheckAccess())
                return action();

            TResult result = default(TResult);
            Action procAction = () => result = action();
            dependencyObject.Dispatcher.Invoke(new MethodInvoker(procAction));
            return result;
        }

    }
}
