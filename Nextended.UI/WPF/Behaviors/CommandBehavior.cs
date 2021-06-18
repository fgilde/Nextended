using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Nextended.UI.WPF.Behaviors
{
    /// <summary>
    /// Das CommandBehavior kann benutzt werden um jedem Element die möglichkeit zu geben ein Command zu binden
    /// <example>
    ///   local:CommandBehavior.RoutedEventName="MouseLeftButtonUp"
    ///   local:CommandBehavior.Command="{Binding Command}"
    /// </example>
    /// </summary>
    public static class CommandBehavior
    {
    

        /// <summary>
        /// Command : The actual ICommand to run
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command",
                                                typeof (ICommand),
                                                typeof (CommandBehavior),
                                                new FrameworkPropertyMetadata((ICommand) null));


        /// <summary>
        /// Gets the Command property.  
        /// </summary>
        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        /// <summary>
        /// Sets the Command property.  
        /// </summary>
        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }


        /// <summary>
        /// CommandParameter
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", 
                                                typeof(object), 
                                                typeof(CommandBehavior),
                                                new FrameworkPropertyMetadata(null));


        /// <summary>
        /// Gets the CommandParameterProperty property.  
        /// </summary>
        public static object GetCommandParameter(DependencyObject d)
        {
            return d.GetValue(CommandParameterProperty);
        }

        /// <summary>
        /// Sets the CommandParameterProperty property.  
        /// </summary>
        public static void SetCommandParameter(DependencyObject d, object value)
        {
            d.SetValue(CommandParameterProperty, value);
        }



     
        /// <summary>
        /// RoutedEventName : The event that should actually execute the
        /// ICommand
        /// </summary>
        public static readonly DependencyProperty RoutedEventNameProperty =
            DependencyProperty.RegisterAttached("RoutedEventName", typeof (String),
                                                typeof (CommandBehavior),
                                                new FrameworkPropertyMetadata(String.Empty, OnRoutedEventNameChanged));


        /// <summary>
        /// Gets the RoutedEventName property.  
        /// </summary>
        public static String GetRoutedEventName(DependencyObject d)
        {
            return (String) d.GetValue(RoutedEventNameProperty);
        }


        /// <summary>
        /// Sets the RoutedEventName property.  
        /// </summary>
        public static void SetRoutedEventName(DependencyObject d, String value)
        {
            d.SetValue(RoutedEventNameProperty, value);
        }


        /// <summary>
        /// Hooks up a Dynamically created EventHandler (by using the 
        /// <see cref="EventHooker">EventHooker</see> class) that when
        /// run will run the associated ICommand
        /// </summary>
        private static void OnRoutedEventNameChanged(DependencyObject d,
                                                     DependencyPropertyChangedEventArgs e)
        {
            var routedEvent = (String) e.NewValue;


            //If the RoutedEvent string is not null, create a new

            //dynamically created EventHandler that when run will execute

            //the actual bound ICommand instance (usually in the ViewModel)

            if (!String.IsNullOrEmpty(routedEvent))
            {
                var eventHooker = new EventHooker {ObjectWithAttachedCommand = d};


                EventInfo eventInfo = d.GetType().GetEvent(routedEvent,
                                                           BindingFlags.Public | BindingFlags.Instance);


                //Hook up Dynamically created event handler

                if (eventInfo != null)
                {
                    eventInfo.AddEventHandler(d,
                                              eventHooker.GetNewEventHandlerToRunCommand(eventInfo));
                }
            }
        }

    }


    /// <summary>
    /// Contains the event that is hooked into the source RoutedEvent
    /// that was specified to run the ICommand
    /// </summary>
    internal sealed class EventHooker
    {
        #region Public Methods/Properties

        /// <summary>
        /// The DependencyObject, that holds a binding to the actual
        /// ICommand to execute
        /// </summary>
        public DependencyObject ObjectWithAttachedCommand { get; set; }


        /// <summary>
        /// Creates a Dynamic EventHandler that will be run the ICommand
        /// when the user specified RoutedEvent fires
        /// </summary>
        /// <param name="eventInfo">The specified RoutedEvent EventInfo</param>
        /// <returns>An Delegate that points to a new EventHandler
        /// that will be run the ICommand</returns>
        public Delegate GetNewEventHandlerToRunCommand(EventInfo eventInfo)
        {
            if (eventInfo == null)

                throw new ArgumentNullException("eventInfo");


            if (eventInfo.EventHandlerType == null)

                throw new ArgumentException("EventHandlerType is null");


            Delegate del = Delegate.CreateDelegate(eventInfo.EventHandlerType, this,
                                                   GetType().GetMethod("OnEventRaised",
                                                                       BindingFlags.NonPublic |
                                                                       BindingFlags.Instance));


            return del;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs the ICommand when the requested RoutedEvent fires
        /// </summary>
        private void OnEventRaised(object sender, EventArgs e)
        {
            var dependencyObject = sender as DependencyObject;
            if (dependencyObject != null)
            {
                var command = (ICommand) dependencyObject.GetValue(CommandBehavior.CommandProperty);
                var commandParam = dependencyObject.GetValue(CommandBehavior.CommandParameterProperty);

                if (command != null && command.CanExecute(commandParam))
                {
                    command.Execute(commandParam);
                }
            }
        }

        #endregion
    }
}