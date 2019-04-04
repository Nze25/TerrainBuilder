﻿using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using NanoVGDotNet.NanoVG;
using OpenTK;
using OpenTK.Input;

namespace Kuat
{
    public class KuatWindow
    {
        /// <summary>
        /// Gets the time, in milliseconds, that the user has configured to be the minimum time between clicks to be considered a double click
        /// </summary>
        /// <returns>The time in milliseconds</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetDoubleClickTime();
        
        /// <summary>
        /// The index of the control that's tab-focused. This isn't the index throughout the list but rather, the control whose ZIndex property is equal to this is considered focused.
        /// </summary>
        private int _tabIndex;
        /// <summary>
        /// The control which is currently considered focused
        /// </summary>
        private KuatControl _focusedControl;
        /// <summary>
        /// The control which is temporarily stored to check for double clicks
        /// </summary>
        private KuatControl _clickedControl;
        /// <summary>
        /// The time at which a click event occurs to check for double clicks
        /// </summary>
        private DateTime _clickTime;
        
        /// <summary>
        /// The list of controls contained within the window
        /// </summary>
        public ConcurrentBag<KuatControl> Controls { get; }
        /// <summary>
        /// True if the window contains invalid controls and requires a re-render
        /// </summary>
        public bool Invalid => Controls.Any(control => control.Invalid);

        /// <summary>
        /// Creates a <see cref="KuatWindow"/> that contains the functionality to hold controls and pump events
        /// </summary>
        /// <param name="window">The <see cref="INativeWindow"/> parent from which to subscribe events</param>
        public KuatWindow(INativeWindow window)
        {
            Controls = new ConcurrentBag<KuatControl>();
            SubscribeEvents(window);
        }

        /// <summary>
        /// Wires all of the events 
        /// </summary>
        /// <param name="window"></param>
        private void SubscribeEvents(INativeWindow window)
        {
            window.MouseMove += WindowOnMouseEvent;
            window.MouseWheel += WindowOnMouseEvent;
            window.MouseDown += WindowOnMouseEvent;
            window.MouseUp += WindowOnMouseEvent;
            window.KeyDown += WindowOnKeyboardEvent;
            window.KeyUp += WindowOnKeyboardEvent;
            window.KeyPress += WindowOnKeyboardEvent;
        }

        /// <summary>
        /// Renders the GUI into the NanoVG context
        /// </summary>
        /// <param name="sender">The object which initiates the event</param>
        /// <param name="context">The context of the event</param>
        public void Paint(object sender, NvgContext context)
        {
            foreach (var control in Controls) control.ProcessRenderEvent(sender, context);
        }

        /// <summary>
        /// Process keyboard key press events from the parent window
        /// </summary>
        /// <param name="sender">The object which initiates the event</param>
        /// <param name="args">The context of the event</param>
        private void WindowOnKeyboardEvent(object sender, KeyPressEventArgs args)
        {
            if (args.KeyChar == '\t' && (_focusedControl == null || _focusedControl.CanTabOut))
            {
                _tabIndex = (_tabIndex + 1) % Controls.Count;
                var tabbedControl = Controls.First(control => control.TabStop && control.TabIndex == _tabIndex);
                SetFocus(tabbedControl);
            }

            _focusedControl?.ProcessKeyboardEvent(sender, args);
        }
        
        /// <summary>
        /// Process keyboard state change events from the parent window
        /// </summary>
        /// <param name="sender">The object which initiates the event</param>
        /// <param name="args">The context of the event</param>
        private void WindowOnKeyboardEvent(object sender, KeyboardKeyEventArgs args)
        {
            _focusedControl?.ProcessKeyboardEvent(sender, args);
        }
        
        /// <summary>
        /// Process mouse events from the parent window
        /// </summary>
        /// <param name="sender">The object which initiates the event</param>
        /// <param name="args">The context of the event</param>
        private void WindowOnMouseEvent(object sender, MouseEventArgs args)
        {
            var isClickEvent = args is MouseButtonEventArgs;
            var focused = false;
            var prevFocusedControl = _focusedControl;

            // process the mouse events from the top-down, focusing on the topmost control if it's clicked on
            foreach (var control in Controls.OrderByDescending(control => control.ZIndex))
            {
                var stage = control.ProcessMouseEvent(sender, args);
                if (stage != EventStage.Handled || !control.CanFocus || focused) continue;
                SetFocus(control);
                focused = true;
            }

            // if nothing was clicked on, blur all controls
            if (!focused && isClickEvent)
                SetFocus(null);

            // return if we don't need to process click or double click events
            if (_focusedControl is null || _focusedControl != prevFocusedControl || !(args is MouseButtonEventArgs buttonEvent) ||
                buttonEvent.IsPressed)
                return;

            var now = DateTime.Now;

            // check eligibility of click events
            var doubleClick = _focusedControl == _clickedControl &&
                              (now - _clickTime).TotalMilliseconds < GetDoubleClickTime();
            _focusedControl.ProcessMouseEvent(sender, new MouseClickEventArgs(args, doubleClick));

            // reset the clicked control after double click events
            _clickTime = now;
            _clickedControl = doubleClick ? null : _focusedControl;
        }

        /// <summary>
        /// Sets the focus to the specified control
        /// </summary>
        /// <param name="needle">The control to focus on. Pass null to blur all elements.</param>
        private void SetFocus(KuatControl needle)
        {
            _focusedControl = needle;
            foreach (var control in Controls)
                control.HasFocus = control == needle;
        }
    }
}
