namespace Xilium.CefGlue
{
    using System;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to observe preference changes. The methods of this
    /// class will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefPreferenceObserver
    {
        private void on_preference_changed(cef_preference_observer_t* self, cef_string_t* name)
        {
            CheckSelf(self);

            var mName = cef_string_t.ToString(name);

            OnPreferenceChanged(mName);
        }

        /// <summary>
        /// Called when a preference has changed. The new value can be retrieved using
        /// CefPreferenceManager::GetPreference.
        /// </summary>
        protected virtual void OnPreferenceChanged(string name)
        {
        }
    }
}
