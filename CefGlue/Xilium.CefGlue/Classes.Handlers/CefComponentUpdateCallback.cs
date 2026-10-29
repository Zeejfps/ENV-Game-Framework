namespace Xilium.CefGlue
{
    using System;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface for CefRequestContext::UpdateComponent. The methods of
    /// this class will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefComponentUpdateCallback
    {
        private void on_complete(cef_component_update_callback_t* self, cef_string_t* component_id, cef_component_update_error_t error)
        {
            CheckSelf(self);

            var mComponentId = cef_string_t.ToString(component_id);

            OnComplete(mComponentId, (int)error);
        }

        /// <summary>
        /// Method that will be called on completion of a component update. |error|
        /// is 0 (CEF_COMPONENT_UPDATE_ERROR_NONE) on success.
        /// </summary>
        protected virtual void OnComplete(string componentId, int error)
        {
        }
    }
}
