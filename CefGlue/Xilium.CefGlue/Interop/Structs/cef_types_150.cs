//
// Interop enums + plain structs introduced in CEF 150 that the generated *.g.cs
// reference by their raw C name. CefGlue hand-maintains enums and non-ref-counted
// structs (the generator emits neither), so each CEF bump adds the new ones here.
// Manually transcribed from cef/include/internal/cef_types*.h for CEF 150.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    // --- cef_types_runtime.h ---
    internal enum cef_runtime_style_t
    {
        CEF_RUNTIME_STYLE_DEFAULT = 0,
        CEF_RUNTIME_STYLE_CHROME,
        CEF_RUNTIME_STYLE_ALLOY,
    }

    // --- cef_types.h ---
    internal enum cef_log_items_t
    {
        LOG_ITEMS_DEFAULT = 0,
        LOG_ITEMS_NONE = 1,
        LOG_ITEMS_FLAG_PROCESS_ID = 1 << 1,
        LOG_ITEMS_FLAG_THREAD_ID = 1 << 2,
        LOG_ITEMS_FLAG_TIME_STAMP = 1 << 3,
        LOG_ITEMS_FLAG_TICK_COUNT = 1 << 4,
    }

    internal enum cef_color_variant_t
    {
        CEF_COLOR_VARIANT_SYSTEM = 0,
        CEF_COLOR_VARIANT_LIGHT,
        CEF_COLOR_VARIANT_DARK,
        CEF_COLOR_VARIANT_TONAL_SPOT,
        CEF_COLOR_VARIANT_NEUTRAL,
        CEF_COLOR_VARIANT_VIBRANT,
        CEF_COLOR_VARIANT_EXPRESSIVE,
        CEF_COLOR_VARIANT_NUM_VALUES,
    }

    internal enum cef_task_type_t
    {
        CEF_TASK_TYPE_UNKNOWN = 0,
        CEF_TASK_TYPE_BROWSER,
        CEF_TASK_TYPE_GPU,
        CEF_TASK_TYPE_ZYGOTE,
        CEF_TASK_TYPE_UTILITY,
        CEF_TASK_TYPE_RENDERER,
        CEF_TASK_TYPE_EXTENSION,
        CEF_TASK_TYPE_GUEST,
        CEF_TASK_TYPE_PLUGIN_DEPRECATED,
        CEF_TASK_TYPE_PLUGIN,
        CEF_TASK_TYPE_SANDBOX_HELPER,
        CEF_TASK_TYPE_DEDICATED_WORKER,
        CEF_TASK_TYPE_SHARED_WORKER,
        CEF_TASK_TYPE_SERVICE_WORKER,
        CEF_TASK_TYPE_NUM_VALUES,
    }

    // --- cef_types_component.h ---
    internal enum cef_component_state_t
    {
        CEF_COMPONENT_STATE_NEW = 0,
        CEF_COMPONENT_STATE_CHECKING = 1,
        CEF_COMPONENT_STATE_CAN_UPDATE = 2,
        CEF_COMPONENT_STATE_DOWNLOADING = 3,
        CEF_COMPONENT_STATE_DECOMPRESSING = 4,
        CEF_COMPONENT_STATE_PATCHING = 5,
        CEF_COMPONENT_STATE_UPDATING = 6,
        CEF_COMPONENT_STATE_UPDATED = 7,
        CEF_COMPONENT_STATE_UP_TO_DATE = 8,
        CEF_COMPONENT_STATE_UPDATE_ERROR = 9,
        CEF_COMPONENT_STATE_RUN = 10,
    }

    internal enum cef_component_update_priority_t
    {
        CEF_COMPONENT_UPDATE_PRIORITY_BACKGROUND = 0,
        CEF_COMPONENT_UPDATE_PRIORITY_FOREGROUND = 1,
    }

    internal enum cef_component_update_error_t
    {
        CEF_COMPONENT_UPDATE_ERROR_NONE = 0,
        CEF_COMPONENT_UPDATE_ERROR_UPDATE_IN_PROGRESS = 1,
        CEF_COMPONENT_UPDATE_ERROR_UPDATE_CANCELED = 2,
        CEF_COMPONENT_UPDATE_ERROR_RETRY_LATER = 3,
        CEF_COMPONENT_UPDATE_ERROR_SERVICE_ERROR = 4,
        CEF_COMPONENT_UPDATE_ERROR_UPDATE_CHECK_ERROR = 5,
        CEF_COMPONENT_UPDATE_ERROR_CRX_NOT_FOUND = 6,
        CEF_COMPONENT_UPDATE_ERROR_INVALID_ARGUMENT = 7,
        CEF_COMPONENT_UPDATE_ERROR_BAD_CRX_DATA_CALLBACK = 8,
    }

    // --- cef_types.h: cef_task_info_t (CefTaskManager::GetTaskInfo) ---
    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_task_info_t
    {
        public UIntPtr size;
        public long id;
        public cef_task_type_t type;
        public int is_killable;
        public cef_string_t title;
        public double cpu_usage;
        public int number_of_processors;
        public long memory;
        public long gpu_memory;
        public int is_gpu_memory_inflated;
    }

    // --- cef_types_osr.h: shared-texture accelerated-paint metadata (not used by
    //     the software OnPaint path; defined for ABI completeness of the vtable). ---
    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_accelerated_paint_info_common_t
    {
        public UIntPtr size;
        public ulong timestamp;
        public cef_size_t coded_size;
        public cef_rect_t visible_rect;
        public cef_rect_t content_rect;
        public cef_size_t source_size;
        public cef_rect_t capture_update_rect;
        public cef_rect_t region_capture_rect;
        public ulong capture_counter;
        public byte has_capture_update_rect;
        public byte has_region_capture_rect;
        public byte has_source_size;
        public byte has_capture_counter;
    }

    // --- cef_types_win.h: cef_accelerated_paint_info_t (Windows) ---
    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_accelerated_paint_info_t
    {
        public UIntPtr size;
        // cef_shared_texture_handle_t == HANDLE (void*) on Windows.
        public IntPtr shared_texture_handle;
        public CefColorType format;
        public cef_accelerated_paint_info_common_t extra;
    }
}
