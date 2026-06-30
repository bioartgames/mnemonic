class_name HookDockHost
extends RefCounted

var plugin: EditorPlugin = null
var theme: HookDockTheme = null
var toast_label: Label = null
var toast_timer: Timer = null
var dock_body: Control = null
var dock_top: VBoxContainer = null
var clips_list_panel: PanelContainer = null
var clips_box: VBoxContainer = null
var segment_log_panel_outer: Control = null
