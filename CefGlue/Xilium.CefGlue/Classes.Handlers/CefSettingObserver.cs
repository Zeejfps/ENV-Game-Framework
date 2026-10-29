namespace Xilium.CefGlue
{
    using System;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to observe content and website setting changes.
    /// The methods of this class will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefSettingObserver
    {
        private void on_setting_changed(cef_setting_observer_t* self, cef_string_t* requesting_url, cef_string_t* top_level_url, CefContentSettingType content_type)
        {
            CheckSelf(self);

            var mRequestingUrl = cef_string_t.ToString(requesting_url);
            var mTopLevelUrl = cef_string_t.ToString(top_level_url);

            OnSettingChanged(mRequestingUrl, mTopLevelUrl, content_type);
        }

        /// <summary>
        /// Called when a content or website setting has changed. The new value can be
        /// retrieved using CefRequestContext::GetContentSetting or
        /// CefRequestContext::GetWebsiteSetting.
        /// </summary>
        protected virtual void OnSettingChanged(string requestingUrl, string topLevelUrl, CefContentSettingType contentType)
        {
        }
    }
}
