class_name HookDockSegmentLogContext
extends RefCounted

var plugin: EditorPlugin = null
var segment_log_list: VBoxContainer
var segment_log_search: LineEdit
var theme: HookDockTheme
var clear_segment_log_list: Callable = Callable()
var add_segment_log_empty_state: Callable = Callable()
var update_segment_log_clear_enabled: Callable = Callable()
var sync_segment_log_layout: Callable = Callable()
