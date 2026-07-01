class_name HookDockClipListContext
extends RefCounted

const HookClipThumbnailQueueGd = preload(
	"res://addons/mnemonic/clips/hook_clip_thumbnail_queue.gd"
)

var plugin: EditorPlugin = null
var clips_box: VBoxContainer
var filter_criteria: HookClipFilterCriteria
var thumb_queue: HookClipThumbnailQueue = null
var clips_thumb_generation: int = 0
var clip_row_preserve_threshold: int = -1
var clip_row_notable_score_min: int = -1
var clip_row_highlight_score_min: int = -1
var last_clips_reload_signature: String = ""
var clips_reload_pending: bool = false
var segment_log_panel_outer: Control
var stop_live_countdown: Callable = Callable()
var build_live_clip_row: Callable = Callable()
var add_clip_row: Callable = Callable()
var add_group_header: Callable = Callable()
var add_clips_empty_state: Callable = Callable()
var add_clips_message: Callable = Callable()
var schedule_thumb_queue_pump: Callable = Callable()
var sync_clips_thumb_generation: Callable = Callable()
var clips_reload_signature: Callable = Callable()
var preserve_threshold_for_tooltips: Callable = Callable()
var notable_score_min_for_tooltips: Callable = Callable()
var highlight_score_min_for_tooltips: Callable = Callable()
var reload_segment_log: Callable = Callable()
