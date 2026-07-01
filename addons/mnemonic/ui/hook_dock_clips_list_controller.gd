class_name HookDockClipsListController
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookClipsSourceGd = preload("res://addons/mnemonic/clips/hook_clips_source.gd")
const HookClipsIndexReaderGd = preload("res://addons/mnemonic/clips/hook_clips_index_reader.gd")
const HookClipDeleteGd = preload("res://addons/mnemonic/clips/hook_clip_delete.gd")
const HookClipDisplayGd = preload("res://addons/mnemonic/clips/hook_clip_display.gd")
const HookDevlogOutlineGd = preload("res://addons/mnemonic/clips/hook_devlog_outline.gd")
const HookClipGroupLabelGd = preload("res://addons/mnemonic/clips/hook_clip_group_label.gd")
const HookLiveRecIndicatorGd = preload("res://addons/mnemonic/ui/hook_live_rec_indicator.gd")
const HookClipsRowBuilderGd = preload("res://addons/mnemonic/clips/hook_clips_row_builder.gd")
const HookDockClipRowGd = preload("res://addons/mnemonic/ui/hook_dock_clip_row.gd")
const HookDockClipListReloadGd = preload(
	"res://addons/mnemonic/ui/hook_dock_clip_list_reload.gd"
)
const HookDockClipListContextGd = preload(
	"res://addons/mnemonic/ui/hook_dock_clip_list_context.gd"
)
const HookDockEmptyStateGd = preload("res://addons/mnemonic/ui/hook_dock_empty_state.gd")
const HookDockVerticalLayoutGd = preload("res://addons/mnemonic/ui/hook_dock_vertical_layout.gd")
const HookDockToolbarStyleGd = preload("res://addons/mnemonic/ui/hook_dock_toolbar_style.gd")
const HookDockStatusToastGd = preload("res://addons/mnemonic/ui/hook_dock_status_toast.gd")
const HookLiveSaveOverlayGd = preload("res://addons/mnemonic/ui/hook_live_save_overlay.gd")
const HookClipThumbnailQueueGd = preload(
	"res://addons/mnemonic/clips/hook_clip_thumbnail_queue.gd"
)
const HookDockHostGd = preload("res://addons/mnemonic/ui/hook_dock_host.gd")
const HookDockFilterControllerGd = preload(
	"res://addons/mnemonic/ui/hook_dock_filter_controller.gd"
)
const HookDockLiveSessionControllerGd = preload(
	"res://addons/mnemonic/ui/hook_dock_live_session_controller.gd"
)
const HookDockLiveSaveControllerGd = preload(
	"res://addons/mnemonic/ui/hook_dock_live_save_controller.gd"
)

var host = null
var filter = null
var live_session = null
var live_save = null
var clips_box: VBoxContainer = null
var delete_confirm: ConfirmationDialog = null
var live_save_poll_timer: Timer = null
var on_reload_segment_log: Callable = Callable()
var on_apply_drawer_offset: Callable = Callable()
var _dock_node: Node = null

var last_reload_signature: String = ""
var reload_pending := false
var thumb_generation := 0
var thumb_queue: HookClipThumbnailQueue = HookClipThumbnailQueueGd.new()
var preserve_threshold: int = -1
var notable_min: int = -1
var highlight_min: int = -1
var clip_rows_compact := false
var pending_delete_folder: String = ""
var pending_delete_clips_dir: String = ""


func setup(
	p_host,
	p_filter,
	p_live_session,
	p_live_save,
	p_clips_box: VBoxContainer,
	p_delete_confirm: ConfirmationDialog,
	p_live_save_poll_timer: Timer = null,
	p_on_reload_segment_log: Callable = Callable(),
	p_on_apply_drawer_offset: Callable = Callable(),
	p_dock_node: Node = null,
) -> void:
	host = p_host
	filter = p_filter
	live_session = p_live_session
	live_save = p_live_save
	clips_box = p_clips_box
	delete_confirm = p_delete_confirm
	live_save_poll_timer = p_live_save_poll_timer
	on_reload_segment_log = p_on_reload_segment_log
	on_apply_drawer_offset = p_on_apply_drawer_offset
	_dock_node = p_dock_node
	if live_session != null:
		live_session.build_live_clip_row_fn = Callable(self, "build_live_clip_row")
		live_session.clip_row_hbox_fn = Callable(self, "clip_row_hbox")
		live_session.apply_clip_row_tooltip_fn = Callable(self, "_apply_clip_row_tooltip")
	if delete_confirm != null and not delete_confirm.confirmed.is_connected(on_delete_confirmed):
		delete_confirm.confirmed.connect(on_delete_confirmed)


func reload_if_needed(mode_fn: Callable = Callable()) -> void:
	var signature := reload_signature()
	if signature == last_reload_signature and not reload_pending:
		return
	if any_clip_row_popup_visible():
		reload_pending = true
		return
	reload_pending = false
	last_reload_signature = signature
	var mode: int = list_source_mode()
	if mode_fn.is_valid():
		mode = int(mode_fn.call())
	reload(mode)


func reload(mode: int) -> void:
	var ctx := _make_clip_list_context()
	HookDockClipListReloadGd.execute(ctx, mode)
	thumb_generation = ctx.clips_thumb_generation
	preserve_threshold = ctx.clip_row_preserve_threshold
	notable_min = ctx.clip_row_notable_score_min
	highlight_min = ctx.clip_row_highlight_score_min
	if live_session != null:
		live_session.set_thresholds(preserve_threshold, highlight_min, notable_min)
	if on_apply_drawer_offset.is_valid():
		on_apply_drawer_offset.call(false)


func reload_signature() -> String:
	var parts: PackedStringArray = []
	parts.append(str(list_source_mode()))
	if host == null or host.plugin == null:
		return "|".join(parts)
	var paths: MnemonicDataRootPaths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return "|".join(parts)
	var index_path := paths.get_clips_index_file()
	var index_mtime := 0
	if FileAccess.file_exists(index_path):
		index_mtime = int(FileAccess.get_modified_time(index_path))
	parts.append(str(index_mtime))
	var lifecycle: HookLifecycleState = host.plugin.get_lifecycle_state()
	parts.append("1" if lifecycle.core_running else "0")
	var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
	if snap != null:
		parts.append("1" if snap.recording else "0")
		parts.append(str(snap.state))
		parts.append(str(snap.capture_prefix))
		parts.append(str(snap.current_segment_index))
		parts.append(str(snap.last_segment_score))
		var live: Dictionary = snap.live_clip_preview
		if typeof(live) == TYPE_DICTIONARY and not live.is_empty():
			parts.append(str(live.get("segment_id", "")))
			parts.append(str(live.get("score_preview", 0)))
			var signals: Variant = live.get("signal_types", [])
			if typeof(signals) == TYPE_ARRAY:
				parts.append(",".join(signals))
			parts.append("live:1")
		else:
			parts.append("live:0")
	return "|".join(parts)


func list_source_mode() -> int:
	if host == null or host.plugin == null:
		return HookClipsSourceGd.SourceMode.SCAN_ONLY
	var paths = host.plugin.get_data_root_paths()
	if paths != null and paths.is_valid() and FileAccess.file_exists(paths.get_clips_index_file()):
		return HookClipsSourceGd.SourceMode.AUTO
	if host.plugin.is_core_running():
		return HookClipsSourceGd.SourceMode.AUTO
	return HookClipsSourceGd.SourceMode.SCAN_ONLY


func schedule_thumb_pump(_dock_node: Node) -> void:
	if thumb_queue != null and thumb_queue.has_pending():
		pump_thumb_queue.call_deferred()


func pump_thumb_queue() -> void:
	if thumb_queue == null:
		return
	if thumb_queue.process_batch(thumb_generation, Mc.CLIP_THUMB_BATCH_SIZE):
		pump_thumb_queue.call_deferred()


func show_loading() -> void:
	if live_session != null:
		live_session.stop_countdown()
	if clips_box == null:
		return
	for child in clips_box.get_children():
		child.queue_free()
	add_message(Mc.CLIPS_LOADING_MESSAGE)


func add_clip_row(info: Dictionary) -> void:
	var is_live_row := bool(info.get("is_live_row", false))
	var tooltip := str(info.get("tooltip", ""))
	var thumb_size := _thumb_size_for_layout()
	var ctx := _make_clip_row_context(thumb_size)
	var hb: HBoxContainer
	var live_row_band: HBoxContainer = null
	var lab: Label = null

	if is_live_row:
		if live_session != null:
			HookLiveRecIndicatorGd.abandon(live_session.live_rec_indicator)
		var built := HookDockClipRow.build_live_hbox(info, ctx)
		hb = built.hb
		live_row_band = built.live_row_band
		lab = built.lab
		if live_session != null:
			live_session.live_rec_indicator = built.live_rec_indicator
		_apply_clip_row_tooltip(hb, tooltip)
		if live_session != null and lab != null:
			live_session.start_countdown(
				lab,
				float(info.get("live_t_close_unix", 0.0)),
				float(info.get("live_t_open_unix", 0.0)),
				int(info.get("live_duration_seconds", 0)),
				int(info.get("live_segment_index", -1)),
			)
	else:
		hb = HookDockClipRow.build_archived_hbox(info, ctx)
		if not tooltip.is_empty():
			_apply_clip_row_tooltip(hb, tooltip)

	wire_clip_row_menu(hb, live_row_band, info, thumb_size, is_live_row)

	var row_root: Control = hb
	if is_live_row and host != null and host.theme != null:
		row_root = HookDockClipRow.wrap_live_panel(hb, host.theme)
	row_root.gui_input.connect(on_clip_row_gui_input.bind(info))
	clips_box.add_child(row_root)


func add_group_header(group: Dictionary, compact: bool) -> void:
	var sep := HSeparator.new()
	clips_box.add_child(sep)

	var hb := HBoxContainer.new()
	hb.custom_minimum_size.x = 0
	hb.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(hb)
	if host != null and host.theme != null:
		hb.add_theme_constant_override("separation", host.theme.row_separation_px())

	var tip := HookClipGroupLabelGd.format_header_tooltip(group)
	if not tip.is_empty():
		hb.tooltip_text = tip

	var label := Label.new()
	label.text = HookClipGroupLabelGd.format_header(group, compact)
	label.mouse_filter = Control.MOUSE_FILTER_PASS
	_style_dock_label(label)
	if host != null and host.theme != null:
		host.theme.apply_shrink_width_label(label, false)
		label.add_theme_font_size_override("font_size", host.theme.heading_font_size() - 1)
	hb.add_child(label)

	var btn_copy := Button.new()
	btn_copy.tooltip_text = Mc.UI_COPY_OUTLINE_TOOLTIP
	btn_copy.visible = not compact
	if host != null and host.theme != null:
		host.theme.apply_flat_icon_button(btn_copy)
		var copy_icon: Texture2D = host.theme.icon(&"ActionCopy")
		if copy_icon != null:
			btn_copy.icon = copy_icon
	btn_copy.pressed.connect(func() -> void: on_copy_outline_pressed(group))
	hb.add_child(btn_copy)

	clips_box.add_child(hb)


func add_empty_state(reason: String) -> void:
	var message := ""
	var icon_name: StringName = &""
	match reason:
		"no_clips":
			message = Mc.UI_CLIPS_NO_CLIPS
			icon_name = &"Animation"
		"no_match":
			message = Mc.UI_CLIPS_NO_MATCH
			icon_name = &"Search"
		"index_unavailable":
			message = Mc.UI_CLIPS_INDEX_UNAVAILABLE
		_:
			add_message(message if not message.is_empty() else reason)
			return
	var theme_ref: HookDockTheme = host.theme if host != null else null
	var col := HookDockEmptyStateGd.build_centered_message(theme_ref, message, icon_name)
	for child in col.get_children():
		if child is Label:
			_style_dock_label(child)
	clips_box.add_child(col)


func add_message(text: String) -> void:
	var lab := Label.new()
	lab.text = text
	_style_dock_label(lab)
	if host != null and host.theme != null:
		host.theme.apply_message_label(lab)
	clips_box.add_child(lab)


func wait_for_index_rebuild(dock_node: Node) -> void:
	if host == null or host.plugin == null:
		return
	var paths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	if not FileAccess.file_exists(paths.get_rebuild_clips_index_file()):
		return
	var built_before := HookClipsIndexReaderGd.read_built_at_unix(paths)
	var deadline_msec := Time.get_ticks_msec() + Mc.DOCK_REBUILD_WAIT_MS
	while Time.get_ticks_msec() < deadline_msec:
		if dock_node == null or not dock_node.is_inside_tree():
			return
		if not FileAccess.file_exists(paths.get_rebuild_clips_index_file()):
			var built_after := HookClipsIndexReaderGd.read_built_at_unix(paths)
			if built_after != built_before:
				return
		await dock_node.get_tree().create_timer(Mc.DOCK_REBUILD_POLL_SEC).timeout


func on_refresh_pressed(dock_node: Node) -> void:
	if host != null and host.plugin != null and host.plugin.is_core_running():
		host.plugin.request_rebuild_clips_index()
		await wait_for_index_rebuild(dock_node)
	reload(list_source_mode())


func on_clip_row_popup_hidden() -> void:
	if reload_pending:
		reload_if_needed()


func on_clip_row_gui_input(event: InputEvent, info: Dictionary) -> void:
	if not event is InputEventMouseButton:
		return
	var mb := event as InputEventMouseButton
	if not mb.pressed or mb.button_index != MOUSE_BUTTON_LEFT or not mb.double_click:
		return
	if bool(info.get("is_live_row", false)):
		if _can_save_live_segment():
			save_live_segment()
		return
	var video_abs := str(info.get("video_abs", ""))
	if FileAccess.file_exists(video_abs):
		OS.shell_open(video_abs)


func on_clip_menu_pressed(
	menu_id: int,
	folder_abs: String,
	video_abs: String,
	display_title: String,
	is_live_row: bool,
	tooltip_text: String = "",
) -> void:
	match menu_id:
		HookDockClipRow.MENU_REVEAL:
			_reveal_folder(folder_abs)
		HookDockClipRow.MENU_SAVE_SEGMENT:
			if is_live_row:
				save_live_segment()
		HookDockClipRow.MENU_PLAY:
			if FileAccess.file_exists(video_abs):
				OS.shell_open(video_abs)
		HookDockClipRow.MENU_DELETE:
			prompt_delete_clip(display_title, folder_abs)
		HookDockClipRow.MENU_COPY_METADATA:
			on_copy_clip_metadata_pressed(tooltip_text)


func on_delete_confirmed() -> void:
	var folder := pending_delete_folder
	var clips_dir := pending_delete_clips_dir
	pending_delete_folder = ""
	pending_delete_clips_dir = ""
	if folder.is_empty():
		return
	var ok := HookClipDeleteGd.delete_clip_folder(folder, clips_dir)
	if ok:
		HookDockStatusToastGd.show(host, Mc.UI_TOAST_CLIP_DELETED)
		var dock_node := _dock_node
		if dock_node != null:
			on_refresh_pressed(dock_node)
	else:
		HookDockStatusToastGd.show(host, Mc.UI_TOAST_DELETE_FAILED)


func prompt_delete_clip(display_title: String, folder_abs: String) -> void:
	if host == null or host.plugin == null or delete_confirm == null:
		return
	var paths: MnemonicDataRootPaths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		HookDockStatusToastGd.show(host, Mc.UI_TOAST_DELETE_FAILED)
		return
	pending_delete_folder = folder_abs
	pending_delete_clips_dir = paths.get_clips_dir()
	delete_confirm.dialog_text = "%s\n\n%s" % [display_title, folder_abs]
	delete_confirm.popup_centered()
	var cancel_btn := delete_confirm.get_cancel_button()
	if cancel_btn != null:
		cancel_btn.grab_focus.call_deferred()


func on_copy_outline_pressed(group: Dictionary) -> void:
	var text := HookDevlogOutlineGd.format_group(group)
	DisplayServer.clipboard_set(text)
	HookDockStatusToastGd.show(host, Mc.UI_TOAST_OUTLINE_COPIED)


func on_copy_clip_metadata_pressed(tooltip_text: String) -> void:
	var text := tooltip_text.strip_edges()
	if text.is_empty():
		return
	DisplayServer.clipboard_set(text)
	HookDockStatusToastGd.show(host, Mc.UI_TOAST_METADATA_COPIED)


func preserve_threshold_for_tooltips(paths: MnemonicDataRootPaths) -> int:
	var snap_threshold := -1
	if host != null and host.plugin != null:
		var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
		if snap != null:
			snap_threshold = snap.preserve_threshold
	return HookClipDisplayGd.resolve_preserve_threshold(snap_threshold, paths)


func highlight_score_min_for_tooltips(paths: MnemonicDataRootPaths) -> int:
	var snap_highlight := -1
	if host != null and host.plugin != null:
		var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
		if snap != null:
			snap_highlight = snap.highlight_score_min
	return HookClipDisplayGd.resolve_highlight_score_min(snap_highlight, paths)


func notable_score_min_for_tooltips(
	paths: MnemonicDataRootPaths,
	preserve_threshold_arg: int = -1,
) -> int:
	var snap_notable := -1
	if host != null and host.plugin != null:
		var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
		if snap != null:
			snap_notable = snap.notable_score_min
	return HookClipDisplayGd.resolve_notable_score_min(
		snap_notable, paths, preserve_threshold_arg
	)


func build_live_clip_row(
	paths: MnemonicDataRootPaths,
	preserve_threshold_arg: int = -1,
	highlight_score_min_arg: int = -1,
	notable_score_min_arg: int = -1,
) -> Dictionary:
	if host == null or host.plugin == null:
		return {}
	var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
	var close_phase_fn := Callable()
	if live_session != null:
		close_phase_fn = Callable(live_session, "live_close_phase_from_snap")
	var save_pending: bool = live_save.pending if live_save != null else false
	var save_segment: int = live_save.segment_index if live_save != null else -1
	return HookClipsRowBuilderGd.from_live_preview(
		snap,
		paths,
		preserve_threshold_arg,
		highlight_score_min_arg,
		notable_score_min_arg,
		close_phase_fn,
		save_pending,
		save_segment,
	)


func apply_responsive_layout(dock_width: float) -> void:
	var compact := dock_width < Mc.DOCK_COMPACT_WIDTH_PX
	if compact != clip_rows_compact:
		clip_rows_compact = compact
		_update_clip_row_compact_states()


func wire_clip_row_menu(
	hb: HBoxContainer,
	live_row_band: HBoxContainer,
	info: Dictionary,
	thumb_size: Vector2,
	is_live_row: bool,
) -> void:
	var menu_ctx := HookDockClipRow.ClipRowMenuContext.new()
	menu_ctx.theme = host.theme if host != null else null
	menu_ctx.info = info
	menu_ctx.thumb_size = thumb_size
	menu_ctx.is_live_row = is_live_row
	menu_ctx.can_save_live_segment = Callable(self, "_can_save_live_segment")
	menu_ctx.on_menu_pressed = Callable(self, "on_clip_menu_pressed")
	menu_ctx.on_popup_hidden = Callable(self, "on_clip_row_popup_hidden")
	HookDockClipRow.wire_production_menu(hb, live_row_band, menu_ctx)


func any_clip_row_popup_visible() -> bool:
	if clips_box == null:
		return false
	for row in clips_box.get_children():
		var hb := clip_row_hbox(row)
		if hb == null:
			continue
		for child in hb.get_children():
			if child is MenuButton:
				var row_popup: PopupMenu = child.get_popup()
				if row_popup != null and row_popup.visible:
					return true
	return false


func clip_row_hbox(row: Node) -> HBoxContainer:
	if row is HBoxContainer:
		return row
	if row is PanelContainer and row.get_child_count() > 0:
		var inner := row.get_child(0)
		if inner is HBoxContainer:
			return inner
	return null


func save_live_segment() -> void:
	var on_started := func() -> void:
		if live_save_poll_timer != null and is_instance_valid(live_save_poll_timer):
			live_save_poll_timer.start()
		if live_session != null:
			live_session.set_thresholds(preserve_threshold, highlight_min, notable_min)
			live_session.update_rec_indicator_tier()
			live_session.sync_live_row_tooltips()
	if host == null or live_save == null or not live_save.begin_save(host.plugin, on_started):
		flash_live_save_error_on_panel()


func refresh_for_live_save_ui() -> void:
	last_reload_signature = ""
	reload_if_needed()


func find_live_row_panel() -> PanelContainer:
	if clips_box == null:
		return null
	for child in clips_box.get_children():
		if child is PanelContainer and child.get_meta(&"mnemonic_live_row", false):
			return child
	return null


func flash_live_save_error_on_panel() -> void:
	var live_panel := find_live_row_panel()
	if live_panel == null or host == null or host.theme == null:
		return
	var overlay := HookLiveSaveOverlayGd.attach(live_panel, host.theme)
	HookLiveSaveOverlayGd.flash_error(overlay, host.theme)


func _make_clip_list_context() -> HookDockClipListContext:
	var ctx := HookDockClipListContextGd.new()
	ctx.plugin = host.plugin if host != null else null
	ctx.clips_box = clips_box
	ctx.filter_criteria = filter.criteria if filter != null else null
	ctx.thumb_queue = thumb_queue
	ctx.clips_thumb_generation = thumb_generation
	ctx.clip_row_preserve_threshold = preserve_threshold
	ctx.clip_row_notable_score_min = notable_min
	ctx.clip_row_highlight_score_min = highlight_min
	ctx.last_clips_reload_signature = last_reload_signature
	ctx.clips_reload_pending = reload_pending
	ctx.segment_log_panel_outer = host.segment_log_panel_outer if host != null else null
	if live_session != null:
		ctx.stop_live_countdown = Callable(live_session, "stop_countdown")
	ctx.build_live_clip_row = Callable(self, "build_live_clip_row")
	ctx.add_clip_row = Callable(self, "add_clip_row")
	ctx.add_group_header = Callable(self, "add_group_header")
	ctx.add_clips_empty_state = Callable(self, "add_empty_state")
	ctx.add_clips_message = Callable(self, "add_message")
	ctx.schedule_thumb_queue_pump = Callable(self, "_schedule_thumb_pump_from_reload")
	ctx.sync_clips_thumb_generation = Callable(self, "_sync_thumb_generation")
	ctx.clips_reload_signature = Callable(self, "reload_signature")
	ctx.preserve_threshold_for_tooltips = Callable(self, "preserve_threshold_for_tooltips")
	ctx.notable_score_min_for_tooltips = Callable(self, "notable_score_min_for_tooltips")
	ctx.highlight_score_min_for_tooltips = Callable(self, "highlight_score_min_for_tooltips")
	ctx.reload_segment_log = on_reload_segment_log
	return ctx


func _schedule_thumb_pump_from_reload() -> void:
	schedule_thumb_pump(_dock_node)


func _sync_thumb_generation(generation: int) -> void:
	thumb_generation = generation


func _make_clip_row_context(thumb_size: Vector2) -> HookDockClipRow.ClipRowContext:
	var ctx := HookDockClipRow.ClipRowContext.new()
	ctx.theme = host.theme if host != null else null
	ctx.thumb_size = thumb_size
	ctx.scaled_px_fn = Callable(self, "_scaled_px")
	ctx.preserve_threshold = preserve_threshold
	ctx.highlight_score_min = highlight_min
	ctx.notable_score_min = notable_min
	ctx.thumb_generation = thumb_generation
	ctx.thumb_enqueue_fn = Callable(self, "_enqueue_clip_row_thumb")
	ctx.style_label_fn = Callable(self, "_style_dock_label")
	ctx.apply_tooltip_fn = Callable(self, "_apply_clip_row_tooltip")
	if host != null and host.plugin != null:
		var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
		if snap != null:
			ctx.pending_manual_preserve_segment = snap.pending_manual_preserve_segment_index
	ctx.live_save_pending = live_save.pending if live_save != null else false
	ctx.live_save_segment_index = live_save.segment_index if live_save != null else -1
	return ctx


func _enqueue_clip_row_thumb(generation: int, thumb_abs: String, thumb_rect: TextureRect) -> void:
	thumb_queue.enqueue(generation, thumb_abs, thumb_rect)


func _thumb_size_for_layout() -> Vector2:
	if clip_rows_compact:
		return Vector2(Mc.THUMB_COMPACT_WIDTH_PX, Mc.THUMB_COMPACT_HEIGHT_PX)
	return Vector2(Mc.CLIP_THUMB_WIDTH_PX, Mc.CLIP_THUMB_HEIGHT_PX)


func _update_clip_row_compact_states() -> void:
	if clips_box == null:
		return
	var thumb_size := _thumb_size_for_layout()
	for child in clips_box.get_children():
		if child is HBoxContainer:
			_apply_compact_to_clip_row(child)
		elif child is PanelContainer and child.get_meta(&"mnemonic_live_row", false):
			_apply_compact_to_live_row(child as PanelContainer, thumb_size)


func _apply_compact_to_clip_row(hb: HBoxContainer) -> void:
	var thumb_size := _thumb_size_for_layout()
	for child in hb.get_children():
		if child is TextureRect:
			child.custom_minimum_size = thumb_size


func _apply_compact_to_live_row(live_panel: PanelContainer, thumb_size: Vector2) -> void:
	if live_panel.get_child_count() == 0:
		return
	var row_hb := live_panel.get_child(0) as HBoxContainer
	if row_hb == null:
		return
	var band := row_hb.get_child(0) as HBoxContainer
	if band == null:
		return
	for child in band.get_children():
		if child.get_meta(&"mnemonic_live_indicator", false):
			HookLiveRecIndicatorGd.resize_thumb(child.get_meta(&"mnemonic_indicator_state", {}), thumb_size)
			child.custom_minimum_size = thumb_size
		elif child is CenterContainer and child.get_meta(&"mnemonic_live_menu_slot", false):
			child.custom_minimum_size.x = thumb_size.x
		elif child is CenterContainer and child.get_meta(&"mnemonic_live_timer_slot", false):
			child.custom_minimum_size.y = thumb_size.y
			if child.get_child_count() > 0 and child.get_child(0) is Label:
				var timer_lab := child.get_child(0) as Label
				timer_lab.custom_minimum_size.y = thumb_size.y
	HookDockVerticalLayoutGd.apply_thumb_band_center(band, thumb_size.y)


func _apply_clip_row_tooltip(control: Control, tooltip: String) -> void:
	if control == null or tooltip.is_empty():
		return
	control.tooltip_text = tooltip


func _style_dock_label(label: Label) -> void:
	HookDockToolbarStyleGd.apply_dock_label(label)


func _scaled_px(base_px: int) -> int:
	if host != null and host.theme != null:
		return maxi(1, int(round(float(base_px) * float(host.theme.get_scale()))))
	return base_px


func _can_save_live_segment() -> bool:
	if live_save == null or host == null:
		return false
	return live_save.can_save(host.plugin, preserve_threshold)


func _reveal_folder(folder_abs: String) -> void:
	if folder_abs.is_empty():
		return
	var err := OS.shell_show_in_file_manager(folder_abs)
	if err != OK:
		push_warning(
			"Mnemonic: shell_show_in_file_manager failed (%d), falling back to shell_open" % err
		)
		OS.shell_open(folder_abs)
