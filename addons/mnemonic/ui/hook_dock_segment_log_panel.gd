class_name HookDockSegmentLogPanel
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const SegmentHistoryIoGd = preload("res://addons/mnemonic_hook/ipc/segment_history_io.gd")
const HookDockVerticalLayoutGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_vertical_layout.gd"
)
const HookDockEmptyStateGd = preload("res://addons/mnemonic_hook/ui/hook_dock_empty_state.gd")
const HookDockToolbarStyleGd = preload("res://addons/mnemonic_hook/ui/hook_dock_toolbar_style.gd")
const HookDockSegmentLogReloadGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_segment_log_reload.gd"
)
const HookDockSegmentLogContextGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_segment_log_context.gd"
)
const HookDockHostGd = preload("res://addons/mnemonic_hook/ui/hook_dock_host.gd")
const HookDockClipsDrawerGd = preload("res://addons/mnemonic_hook/ui/hook_dock_clips_drawer.gd")

var host
var toggle_btn: Button
var clear_btn: Button
var outer: PanelContainer
var inner: VBoxContainer
var scroll: ScrollContainer
var list: VBoxContainer
var search: LineEdit
var clear_confirm: ConfirmationDialog
var drawer
var on_layout_changed: Callable = Callable()


func mount(dock_top: VBoxContainer, dock_node: Node) -> void:
	var segment_log_row := HBoxContainer.new()
	segment_log_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(segment_log_row)
	if host != null and host.theme != null:
		segment_log_row.add_theme_constant_override("separation", host.theme.row_separation_px())

	toggle_btn = Button.new()
	toggle_btn.text = Mc.UI_SEGMENT_LOG
	toggle_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	toggle_btn.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	toggle_btn.pressed.connect(on_toggle_pressed)
	HookDockToolbarStyleGd.apply_disclosure_button(host.theme if host != null else null, toggle_btn)
	segment_log_row.add_child(toggle_btn)

	clear_btn = Button.new()
	clear_btn.text = ""
	clear_btn.tooltip_text = Mc.UI_CLEAR_SEGMENT_LOG_TOOLTIP
	clear_btn.size_flags_horizontal = Control.SIZE_SHRINK_END
	clear_btn.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	HookDockToolbarStyleGd.apply_toolbar_icon_button(host.theme if host != null else null, clear_btn)
	clear_btn.pressed.connect(on_clear_pressed)
	segment_log_row.add_child(clear_btn)
	dock_top.add_child(segment_log_row)

	outer = PanelContainer.new()
	outer.visible = false
	outer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	outer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	if host != null and host.theme != null:
		outer.add_theme_stylebox_override("panel", host.theme.make_filter_panel())

	inner = VBoxContainer.new()
	inner.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inner.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if host != null and host.theme != null:
		inner.add_theme_constant_override("separation", host.theme.row_separation_px())
	outer.add_child(inner)

	search = LineEdit.new()
	search.placeholder_text = Mc.UI_SEARCH_SEGMENTS_PLACEHOLDER
	search.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	search.text_changed.connect(on_search_changed)
	if host != null and host.theme != null:
		host.theme.apply_body_font(search)
	inner.add_child(search)

	scroll = ScrollContainer.new()
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	inner.add_child(scroll)

	list = VBoxContainer.new()
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if host != null and host.theme != null:
		list.add_theme_constant_override("separation", host.theme.row_separation_px())
	scroll.add_child(list)

	clear_confirm = ConfirmationDialog.new()
	clear_confirm.title = Mc.UI_DIALOG_CLEAR_SEGMENT_LOG_TITLE
	clear_confirm.dialog_text = Mc.UI_DIALOG_CLEAR_SEGMENT_LOG_TEXT
	clear_confirm.ok_button_text = "Clear log"
	clear_confirm.confirmed.connect(on_clear_confirmed)
	dock_node.add_child(clear_confirm)

	dock_top.add_child(outer)

	if host != null:
		host.segment_log_panel_outer = outer


func on_toggle_pressed() -> void:
	if is_instance_valid(outer):
		outer.visible = not outer.visible
		if outer.visible:
			outer.size_flags_vertical = Control.SIZE_EXPAND_FILL
			reload()
	if on_layout_changed.is_valid():
		on_layout_changed.call_deferred()


func on_clear_pressed() -> void:
	if is_instance_valid(clear_confirm):
		clear_confirm.popup_centered()


func on_search_changed(_new_text: String) -> void:
	reload()


func on_clear_confirmed() -> void:
	if host == null or host.plugin == null:
		return
	var paths: MnemonicDataRootPaths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	SegmentHistoryIoGd.clear(paths)
	if is_instance_valid(search):
		search.set_block_signals(true)
		search.text = ""
		search.set_block_signals(false)
	reload()


func reload() -> void:
	var ctx := _make_reload_context()
	HookDockSegmentLogReloadGd.execute(ctx)


func apply_theme() -> void:
	if host == null or host.theme == null:
		return
	if is_instance_valid(outer):
		outer.add_theme_stylebox_override("panel", host.theme.make_filter_panel())
	if is_instance_valid(search):
		host.theme.apply_body_font(search)
	apply_clear_button_icon()


func apply_clear_button_icon() -> void:
	if not is_instance_valid(clear_btn) or host == null or host.theme == null:
		return
	for icon_name in [&"Clear", &"Remove", &"QueueDelete"]:
		var icon_tex: Texture2D = host.theme.icon_or_null(icon_name)
		if icon_tex != null:
			clear_btn.icon = icon_tex
			return
	clear_btn.icon = null


func update_clear_enabled() -> void:
	if not is_instance_valid(clear_btn):
		return
	clear_btn.disabled = not _segment_history_has_entries()


func _make_reload_context() -> HookDockSegmentLogContext:
	var ctx := HookDockSegmentLogContextGd.new()
	ctx.plugin = host.plugin if host != null else null
	ctx.segment_log_list = list
	ctx.segment_log_search = search
	ctx.theme = host.theme if host != null else null
	ctx.clear_segment_log_list = Callable(self, "_clear_list")
	ctx.add_segment_log_empty_state = Callable(self, "_add_empty_state")
	ctx.update_segment_log_clear_enabled = Callable(self, "update_clear_enabled")
	ctx.sync_segment_log_layout = Callable(self, "_sync_layout")
	return ctx


func _clear_list() -> void:
	if not is_instance_valid(list):
		return
	for child in list.get_children():
		list.remove_child(child)
		child.free()


func _sync_layout() -> void:
	if drawer != null:
		drawer.layout_dock_top()
	_update_scroll_height()
	if drawer != null:
		drawer.apply_offset(false)
	elif on_layout_changed.is_valid():
		on_layout_changed.call_deferred()


func _update_scroll_height() -> void:
	if not is_instance_valid(scroll) or not is_instance_valid(list):
		return
	if not is_instance_valid(outer) or not outer.visible:
		scroll.custom_minimum_size = Vector2.ZERO
		return
	var content_h := int(list.get_combined_minimum_size().y)
	if content_h <= 0:
		scroll.custom_minimum_size = Vector2.ZERO
		return
	var chrome_h := 0
	if host != null and is_instance_valid(host.dock_top):
		for child in host.dock_top.get_children():
			if child == outer:
				break
			chrome_h += int(child.get_combined_minimum_size().y)
			if host.theme != null:
				chrome_h += host.theme.row_separation_px()
	var split_top_px := Mc.DOCK_SPLIT_UNSET
	if drawer != null:
		split_top_px = drawer.drawer_top_px
	if split_top_px == Mc.DOCK_SPLIT_UNSET and host != null and is_instance_valid(host.clips_list_panel):
		split_top_px = host.clips_list_panel.offset_top
	if split_top_px == Mc.DOCK_SPLIT_UNSET:
		split_top_px = 0
	var inner_pad := 8
	if host != null and host.theme != null:
		inner_pad = host.theme.row_separation_px() * 2
	var available_h := maxi(96, split_top_px - chrome_h - inner_pad)
	scroll.custom_minimum_size.y = mini(content_h, available_h)


func _segment_history_has_entries() -> bool:
	if host == null or host.plugin == null:
		return false
	var paths: MnemonicDataRootPaths = host.plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return false
	return not SegmentHistoryIoGd.read_records(paths, 1).is_empty()


func _add_empty_state(reason: String) -> void:
	var message := ""
	match reason:
		"data_root_unavailable":
			message = Mc.UI_SEGMENT_LOG_UNAVAILABLE
		"no_file":
			message = Mc.UI_SEGMENT_LOG_NO_FILE
		"empty":
			message = Mc.UI_SEGMENT_LOG_NO_SEGMENTS
		"no_match":
			message = Mc.UI_SEGMENT_LOG_NO_MATCH
		_:
			message = reason
	var col := HookDockEmptyStateGd.build_centered_message(
		host.theme if host != null else null,
		message,
		&"",
		true,
	)
	list.add_child(col)
