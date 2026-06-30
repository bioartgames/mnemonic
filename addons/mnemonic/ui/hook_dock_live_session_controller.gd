class_name HookDockLiveSessionController
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookSignificanceTierGd = preload("res://addons/mnemonic_hook/clips/hook_significance_tier.gd")
const HookLiveRecIndicatorGd = preload("res://addons/mnemonic_hook/ui/hook_live_rec_indicator.gd")
const HookLiveDisplayGd = preload("res://addons/mnemonic_hook/clips/hook_live_display.gd")
const HookDockHostGd = preload("res://addons/mnemonic_hook/ui/hook_dock_host.gd")
const HookDockLiveSaveControllerGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_live_save_controller.gd"
)

var host = null
var segment_timer: Timer = null
var live_save: HookDockLiveSaveController = null

var clip_row_preserve_threshold: int = -1
var clip_row_highlight_score_min: int = -1
var clip_row_notable_score_min: int = -1

var build_live_clip_row_fn: Callable
var clip_row_hbox_fn: Callable
var apply_clip_row_tooltip_fn: Callable

var countdown_label: Label = null
var t_close_unix := 0.0
var t_open_unix := 0.0
var duration_seconds := 0
var segment_index := -1
var live_rec_indicator: Dictionary = {}


func setup(
	p_host,
	p_segment_timer: Timer,
	p_live_save,
) -> void:
	host = p_host
	segment_timer = p_segment_timer
	live_save = p_live_save


func set_thresholds(preserve: int, highlight: int, notable: int) -> void:
	clip_row_preserve_threshold = preserve
	clip_row_highlight_score_min = highlight
	clip_row_notable_score_min = notable


func should_refresh_from_poll() -> bool:
	if live_save != null and live_save.pending:
		return true
	if segment_index >= 0:
		return true
	return false


func stop_countdown() -> void:
	countdown_label = null
	t_close_unix = 0.0
	t_open_unix = 0.0
	duration_seconds = 0
	segment_index = -1
	HookLiveRecIndicatorGd.abandon(live_rec_indicator)
	if is_instance_valid(segment_timer):
		segment_timer.stop()


func start_countdown(
	label: Label,
	p_t_close_unix: float,
	p_t_open_unix: float = 0.0,
	p_duration_seconds: int = 0,
	p_segment_index: int = -1,
) -> void:
	countdown_label = label
	t_close_unix = p_t_close_unix
	t_open_unix = p_t_open_unix
	duration_seconds = p_duration_seconds
	segment_index = p_segment_index
	_update_live_countdown_label()
	if is_instance_valid(segment_timer):
		segment_timer.start()


func on_segment_timer_tick() -> void:
	if not is_instance_valid(countdown_label):
		stop_countdown()
		return
	_update_live_countdown_label()
	update_rec_indicator_tier()


func update_rec_indicator_tier() -> void:
	if live_rec_indicator.is_empty() or host == null or host.plugin == null:
		return
	var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
	if snap == null:
		return
	var score := 0
	var live_segment_index := segment_index
	var live: Dictionary = snap.live_clip_preview
	if typeof(live) == TYPE_DICTIONARY:
		score = int(live.get("score_preview", 0))
		if live_segment_index < 0:
			live_segment_index = int(live.get("segment_index", -1))
	var save_pending: bool = live_save != null and live_save.pending
	var save_segment_index: int = live_save.segment_index if live_save != null else -1
	var tier_id := HookSignificanceTierGd.resolve_live_rec_tier(
		score,
		clip_row_preserve_threshold,
		clip_row_highlight_score_min,
		clip_row_notable_score_min,
		live_segment_index,
		snap.pending_manual_preserve_segment_index,
		save_pending,
		save_segment_index,
	)
	var close_phase := ""
	var now_unix := float(Time.get_unix_time_from_system())
	if t_close_unix > 0.0 and int(t_close_unix) - int(now_unix) <= 0:
		close_phase = live_close_phase_from_snap(snap, segment_index)
	HookLiveRecIndicatorGd.apply_tier(live_rec_indicator, tier_id, host.theme, close_phase)


func live_close_phase_from_snap(snap: HookStatusSnapshot, live_segment_index: int = -1) -> String:
	if live_save != null and live_save.pending:
		return "saving"
	if snap == null:
		return ""
	if live_segment_index >= 0 and snap.pending_manual_preserve_segment_index == live_segment_index:
		return "saving"
	var live: Dictionary = snap.live_clip_preview
	if typeof(live) == TYPE_DICTIONARY and int(live.get("segment_index", -1)) == live_segment_index:
		var threshold := snap.preserve_threshold
		if threshold < 0:
			threshold = clip_row_preserve_threshold
		var phase := HookLiveDisplayGd.close_phase_at_segment_end(
			int(live.get("score_preview", 0)),
			threshold,
			snap.pending_manual_preserve_segment_index,
			live_segment_index,
		)
		if not phase.is_empty():
			return phase
	if not snap.has_last_segment_preserved:
		return ""
	if snap.last_segment_preserved:
		return "saving"
	return "discarding"


func live_segment_index_from_snap(snap: HookStatusSnapshot) -> int:
	if snap == null:
		return -1
	if live_save != null and live_save.pending:
		return live_save.segment_index
	var live: Dictionary = snap.live_clip_preview
	if typeof(live) == TYPE_DICTIONARY:
		return int(live.get("segment_index", snap.current_segment_index))
	return snap.current_segment_index


func _update_live_countdown_label() -> void:
	if not is_instance_valid(countdown_label):
		return
	var now_unix := float(Time.get_unix_time_from_system())
	var close_phase := ""
	if (
		host != null
		and host.plugin != null
		and t_close_unix > 0.0
		and int(t_close_unix) - int(now_unix) <= 0
	):
		var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
		var idx := segment_index
		if idx < 0:
			idx = live_segment_index_from_snap(snap)
		close_phase = live_close_phase_from_snap(snap, idx)
	countdown_label.text = HookLiveDisplayGd.format_countdown_timer(
		t_close_unix,
		t_open_unix,
		duration_seconds,
		now_unix,
		close_phase,
	)


func sync_live_row_tooltips() -> void:
	if host == null or host.plugin == null:
		return
	var paths: MnemonicDataRootPaths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	if not build_live_clip_row_fn.is_valid():
		return
	var row: Dictionary = build_live_clip_row_fn.call(
		paths,
		clip_row_preserve_threshold,
		clip_row_highlight_score_min,
		clip_row_notable_score_min,
	)
	if row.is_empty():
		return
	var tip := str(row.get("tooltip", ""))
	var panel := find_live_row_panel()
	if panel == null:
		return
	if not clip_row_hbox_fn.is_valid():
		return
	var hb: HBoxContainer = clip_row_hbox_fn.call(panel)
	if hb == null:
		return
	for child in hb.get_children():
		if child is MenuButton:
			continue
		if child is Control:
			if apply_clip_row_tooltip_fn.is_valid():
				apply_clip_row_tooltip_fn.call(child, tip)
			for sub in child.get_children():
				if sub is Control and apply_clip_row_tooltip_fn.is_valid():
					apply_clip_row_tooltip_fn.call(sub, tip)


func find_live_row_panel() -> PanelContainer:
	if host == null or not is_instance_valid(host.clips_box):
		return null
	for child in host.clips_box.get_children():
		if child is PanelContainer and child.get_meta(&"mnemonic_live_row", false):
			return child
	return null
