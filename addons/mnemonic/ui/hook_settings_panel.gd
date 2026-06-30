class_name EditorHookSettingsPanel
extends PopupPanel

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookSettingsIoGd = preload("res://addons/mnemonic_hook/ipc/hook_settings_io.gd")
const SegmentHistoryIoGd = preload("res://addons/mnemonic_hook/ipc/segment_history_io.gd")
const HookSettingsRetentionSectionGd = preload(
	"res://addons/mnemonic_hook/ui/hook_settings_retention_section.gd"
)
const HookSettingsHeuristicsSectionGd = preload(
	"res://addons/mnemonic_hook/ui/hook_settings_heuristics_section.gd"
)

signal settings_toast(message: String)

var _plugin: EditorPlugin = null
var _theme: HookDockTheme = null
var _ui_built := false
var _margin_root: MarginContainer
var _scroll: ScrollContainer
var _content: VBoxContainer

var _retention_panel: PanelContainer
var _segment_duration_spin: SpinBox
var _preserve_threshold_spin: SpinBox
var _notable_score_min_spin: SpinBox
var _highlight_score_min_spin: SpinBox
var _segment_history_max_spin: SpinBox
var _draw_mouse_check: CheckBox
var _signals_section: VBoxContainer
var _signals_scroll: ScrollContainer
var _signals_grid: GridContainer
var _heuristic_rows: Array = []
var _last_panel_width: int = Mc.SETTINGS_PANEL_MIN_WIDTH_PX


func setup(plugin: EditorPlugin, dock_theme: HookDockTheme) -> void:
	_plugin = plugin
	_theme = dock_theme
	if not _ui_built:
		_build_ui()
	_ui_built = true
	apply_panel_theme()


func apply_panel_theme() -> void:
	if _theme != null:
		_theme.apply_editor_theme_to_popup(self)
	remove_theme_stylebox_override(&"panel")
	if not _ui_built:
		return
	_apply_control_themes()


func popup_below_anchor(anchor: Control) -> void:
	if anchor == null:
		return
	apply_panel_theme()
	refresh_from_settings()
	var screen_pos := anchor.get_screen_position()
	var anchor_size := anchor.size
	_last_panel_width = maxi(Mc.SETTINGS_PANEL_MIN_WIDTH_PX, int(anchor_size.x))
	var pos_x := int(screen_pos.x + anchor_size.x - _last_panel_width)
	var pos_y := int(screen_pos.y + anchor_size.y)
	position = Vector2i(maxi(0, pos_x), pos_y)
	popup()
	call_deferred("_finalize_popup_layout")


func refresh_from_settings() -> void:
	if not _ui_built:
		return
	var paths: MnemonicDataRootPaths = null
	if _plugin != null:
		paths = _plugin.get_data_root_paths()
	var enabled := paths != null and paths.is_valid()
	if is_instance_valid(_segment_duration_spin):
		_segment_duration_spin.editable = enabled
	if is_instance_valid(_preserve_threshold_spin):
		_preserve_threshold_spin.editable = enabled
	if is_instance_valid(_notable_score_min_spin):
		_notable_score_min_spin.editable = enabled
	if is_instance_valid(_highlight_score_min_spin):
		_highlight_score_min_spin.editable = enabled
	if is_instance_valid(_segment_history_max_spin):
		_segment_history_max_spin.editable = enabled
	if is_instance_valid(_draw_mouse_check):
		_draw_mouse_check.disabled = not enabled
	if not enabled:
		return
	if is_instance_valid(_segment_duration_spin):
		_segment_duration_spin.set_block_signals(true)
		_segment_duration_spin.value = HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_SEGMENT_DURATION_SECONDS,
			Mc.SETTINGS_DEFAULT_SEGMENT_DURATION_SECONDS,
		)
		_segment_duration_spin.set_block_signals(false)
	if is_instance_valid(_preserve_threshold_spin):
		_preserve_threshold_spin.set_block_signals(true)
		_preserve_threshold_spin.value = HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_PRESERVE_THRESHOLD,
			Mc.SETTINGS_DEFAULT_PRESERVE_THRESHOLD,
		)
		_preserve_threshold_spin.set_block_signals(false)
	if is_instance_valid(_notable_score_min_spin):
		_notable_score_min_spin.set_block_signals(true)
		_notable_score_min_spin.value = HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_NOTABLE_SCORE_MIN,
			Mc.SETTINGS_DEFAULT_NOTABLE_SCORE_MIN,
		)
		_notable_score_min_spin.set_block_signals(false)
	if is_instance_valid(_highlight_score_min_spin):
		_highlight_score_min_spin.set_block_signals(true)
		_highlight_score_min_spin.value = HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_HIGHLIGHT_SCORE_MIN,
			Mc.SETTINGS_DEFAULT_HIGHLIGHT_SCORE_MIN,
		)
		_highlight_score_min_spin.set_block_signals(false)
	if is_instance_valid(_segment_history_max_spin):
		_segment_history_max_spin.set_block_signals(true)
		_segment_history_max_spin.value = HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_SEGMENT_HISTORY_MAX_ENTRIES,
			Mc.SETTINGS_DEFAULT_SEGMENT_HISTORY_MAX_ENTRIES,
		)
		_segment_history_max_spin.set_block_signals(false)
	if is_instance_valid(_draw_mouse_check):
		_draw_mouse_check.set_block_signals(true)
		_draw_mouse_check.button_pressed = HookSettingsIoGd.read_draw_mouse(paths)
		_draw_mouse_check.set_block_signals(false)
	_rebuild_heuristic_signal_rows(paths)


func _build_ui() -> void:
	_margin_root = MarginContainer.new()
	_margin_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(_margin_root)

	var outer := VBoxContainer.new()
	outer.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	outer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	outer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	if _theme != null:
		outer.add_theme_constant_override("separation", _theme.section_separation_px())
	_margin_root.add_child(outer)

	_scroll = ScrollContainer.new()
	_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	outer.add_child(_scroll)

	_content = VBoxContainer.new()
	_content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_content.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if _theme != null:
		_content.add_theme_constant_override("separation", _theme.section_separation_px())
	_scroll.add_child(_content)
	_build_content(_content)
	_apply_margin_overrides()
	_apply_control_themes()


func _apply_margin_overrides() -> void:
	if not is_instance_valid(_margin_root) or _theme == null:
		return
	var m: int = _theme.margin_px()
	_margin_root.add_theme_constant_override("margin_left", m)
	_margin_root.add_theme_constant_override("margin_right", m)
	_margin_root.add_theme_constant_override("margin_top", m)
	_margin_root.add_theme_constant_override("margin_bottom", m)


func _apply_control_themes() -> void:
	if _theme == null:
		return
	_apply_margin_overrides()
	if is_instance_valid(_retention_panel):
		_retention_panel.add_theme_stylebox_override("panel", _theme.make_subtle_panel())
	if is_instance_valid(_draw_mouse_check) and _theme != null:
		_theme.apply_editor_checkbox(_draw_mouse_check)
	for row in _heuristic_rows:
		if row is HookHeuristicRow:
			row.apply_theme(_theme)


func _finalize_popup_layout() -> void:
	_update_popup_size(_last_panel_width)


func _update_popup_size(panel_w: int) -> void:
	if not is_instance_valid(_content) or not is_instance_valid(_scroll):
		return
	_last_panel_width = panel_w
	_apply_signals_scroll_height()
	var content_h := int(_content.get_combined_minimum_size().y)
	var margin_h := 0
	if is_instance_valid(_margin_root):
		margin_h = (
			_margin_root.get_theme_constant("margin_top", "MarginContainer")
			+ _margin_root.get_theme_constant("margin_bottom", "MarginContainer")
		)
	var total_h := content_h + margin_h
	var panel_h := mini(total_h, Mc.SETTINGS_PANEL_MAX_HEIGHT_PX)
	size = Vector2i(panel_w, panel_h)
	var height_clamped := total_h > Mc.SETTINGS_PANEL_MAX_HEIGHT_PX
	if height_clamped or panel_h >= Mc.SETTINGS_PANEL_MAX_HEIGHT_PX:
		_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	else:
		_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED


func _apply_signals_scroll_height() -> void:
	if not is_instance_valid(_signals_scroll) or not is_instance_valid(_signals_grid):
		return
	var rows_h := int(_signals_grid.get_combined_minimum_size().y)
	var scroll_h := mini(rows_h, Mc.SETTINGS_PANEL_SIGNALS_SCROLL_MAX_PX)
	_signals_scroll.custom_minimum_size.y = scroll_h
	if rows_h > Mc.SETTINGS_PANEL_SIGNALS_SCROLL_MAX_PX:
		_signals_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	else:
		_signals_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED


func _add_section_separator(panel: VBoxContainer) -> void:
	var sep := HSeparator.new()
	sep.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_child(sep)


func _add_setting_row(
	panel: VBoxContainer,
	label_text: String,
	control: Control,
	tooltip_text: String = "",
) -> void:
	var row := HBoxContainer.new()
	row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if _theme != null:
		row.add_theme_constant_override("separation", _theme.row_separation_px())
	var lbl := Label.new()
	lbl.text = label_text
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if _theme != null:
		_theme.apply_body_font(lbl)
	row.add_child(lbl)
	control.size_flags_horizontal = Control.SIZE_SHRINK_END
	row.add_child(control)
	panel.add_child(row)
	if not tooltip_text.is_empty():
		lbl.tooltip_text = tooltip_text
		control.tooltip_text = tooltip_text
		row.tooltip_text = tooltip_text


func _build_content(panel: VBoxContainer) -> void:
	var retention := HookSettingsRetentionSectionGd.build(
		_theme,
		panel,
		Callable(self, "_add_setting_row"),
		{
			"segment_duration": _on_segment_duration_changed,
			"preserve_threshold": _on_preserve_threshold_changed,
			"notable_score_min": _on_notable_score_min_changed,
			"highlight_score_min": _on_highlight_score_min_changed,
			"segment_history_max": _on_segment_history_max_changed,
			"draw_mouse": _on_draw_mouse_toggled,
		},
	)
	_retention_panel = retention["retention_panel"]
	_segment_duration_spin = retention["segment_duration_spin"]
	_preserve_threshold_spin = retention["preserve_threshold_spin"]
	_notable_score_min_spin = retention["notable_score_min_spin"]
	_highlight_score_min_spin = retention["highlight_score_min_spin"]
	_segment_history_max_spin = retention["segment_history_max_spin"]
	_draw_mouse_check = retention["draw_mouse_check"]

	_add_section_separator(panel)

	var heuristics := HookSettingsHeuristicsSectionGd.build(_theme, panel)
	_signals_section = heuristics["signals_section"]
	_signals_scroll = heuristics["signals_scroll"]
	_signals_grid = heuristics["signals_grid"]


func _on_segment_duration_changed(_value: float) -> void:
	if not is_instance_valid(_segment_duration_spin):
		return
	_write_setting_int(
		Mc.SETTINGS_KEY_SEGMENT_DURATION_SECONDS,
		int(_segment_duration_spin.value),
		true,
	)


func _on_preserve_threshold_changed(_value: float) -> void:
	if not is_instance_valid(_preserve_threshold_spin):
		return
	var preserve := int(_preserve_threshold_spin.value)
	_clamp_notable_and_highlight_to_preserve(preserve)
	_write_setting_int(
		Mc.SETTINGS_KEY_PRESERVE_THRESHOLD,
		preserve,
		false,
	)


func _on_notable_score_min_changed(_value: float) -> void:
	if not is_instance_valid(_notable_score_min_spin):
		return
	var preserve := int(_preserve_threshold_spin.value) if is_instance_valid(_preserve_threshold_spin) else Mc.SETTINGS_DEFAULT_PRESERVE_THRESHOLD
	var notable := int(_notable_score_min_spin.value)
	if notable < preserve:
		notable = preserve
		_notable_score_min_spin.set_block_signals(true)
		_notable_score_min_spin.value = notable
		_notable_score_min_spin.set_block_signals(false)
	if is_instance_valid(_highlight_score_min_spin) and int(_highlight_score_min_spin.value) <= notable:
		_highlight_score_min_spin.set_block_signals(true)
		_highlight_score_min_spin.value = mini(
			Mc.PRESERVE_THRESHOLD_MAX,
			notable + 1,
		)
		_highlight_score_min_spin.set_block_signals(false)
		_write_setting_int(
			Mc.SETTINGS_KEY_HIGHLIGHT_SCORE_MIN,
			int(_highlight_score_min_spin.value),
			false,
		)
	_write_setting_int(
		Mc.SETTINGS_KEY_NOTABLE_SCORE_MIN,
		notable,
		false,
	)


func _clamp_notable_and_highlight_to_preserve(preserve: int) -> void:
	if is_instance_valid(_notable_score_min_spin):
		if int(_notable_score_min_spin.value) < preserve:
			_notable_score_min_spin.set_block_signals(true)
			_notable_score_min_spin.value = preserve
			_notable_score_min_spin.set_block_signals(false)
			_write_setting_int(
				Mc.SETTINGS_KEY_NOTABLE_SCORE_MIN,
				preserve,
				false,
			)
	if is_instance_valid(_highlight_score_min_spin):
		if int(_highlight_score_min_spin.value) <= preserve:
			_highlight_score_min_spin.set_block_signals(true)
			_highlight_score_min_spin.value = mini(Mc.PRESERVE_THRESHOLD_MAX, preserve + 1)
			_highlight_score_min_spin.set_block_signals(false)
			_write_setting_int(
				Mc.SETTINGS_KEY_HIGHLIGHT_SCORE_MIN,
				int(_highlight_score_min_spin.value),
				false,
			)


func _on_highlight_score_min_changed(_value: float) -> void:
	if not is_instance_valid(_highlight_score_min_spin):
		return
	var preserve := int(_preserve_threshold_spin.value) if is_instance_valid(_preserve_threshold_spin) else Mc.SETTINGS_DEFAULT_PRESERVE_THRESHOLD
	var notable := int(_notable_score_min_spin.value) if is_instance_valid(_notable_score_min_spin) else preserve
	var highlight := int(_highlight_score_min_spin.value)
	if highlight <= notable:
		highlight = mini(Mc.PRESERVE_THRESHOLD_MAX, notable + 1)
		_highlight_score_min_spin.set_block_signals(true)
		_highlight_score_min_spin.value = highlight
		_highlight_score_min_spin.set_block_signals(false)
	_write_setting_int(
		Mc.SETTINGS_KEY_HIGHLIGHT_SCORE_MIN,
		highlight,
		false,
	)


func _on_segment_history_max_changed(_value: float) -> void:
	if not is_instance_valid(_segment_history_max_spin) or _plugin == null:
		return
	var paths: MnemonicDataRootPaths = _plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	var cap := int(_segment_history_max_spin.value)
	var ok := HookSettingsIoGd.write_int(
		paths,
		Mc.SETTINGS_KEY_SEGMENT_HISTORY_MAX_ENTRIES,
		cap,
	)
	if ok:
		SegmentHistoryIoGd.trim_to_max(paths, cap)
	_emit_toast(Mc.UI_TOAST_SETTINGS_SAVED if ok else Mc.UI_TOAST_SETTING_SAVE_FAILED, false)


func _on_draw_mouse_toggled(pressed: bool) -> void:
	if _plugin == null:
		return
	var paths: MnemonicDataRootPaths = _plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	var ok := HookSettingsIoGd.write_draw_mouse(paths, pressed)
	_emit_toast_after_save(ok, true)


func _rebuild_heuristic_signal_rows(paths: MnemonicDataRootPaths) -> void:
	var finalize := Callable(self, "_finalize_popup_layout") if visible else Callable()
	HookSettingsHeuristicsSectionGd.rebuild_rows(
		paths,
		_theme,
		_signals_grid,
		_heuristic_rows,
		Callable(self, "_on_heuristic_row_changed"),
		finalize,
	)


func _on_heuristic_row_changed() -> void:
	if _plugin == null:
		return
	var paths: MnemonicDataRootPaths = _plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	var map: Dictionary = {}
	for row in _heuristic_rows:
		if row is HookHeuristicRow:
			map[row.get_type_id()] = row.to_settings_dict()
	var ok := HookSettingsIoGd.write_heuristics(paths, map)
	_emit_toast(Mc.UI_TOAST_SETTINGS_SAVED if ok else Mc.UI_TOAST_SETTING_SAVE_FAILED, false)


func _write_setting_int(key: String, value: int, needs_capture_restart: bool) -> void:
	if _plugin == null:
		return
	var paths: MnemonicDataRootPaths = _plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return
	var ok := HookSettingsIoGd.write_int(paths, key, value)
	_emit_toast_after_save(ok, needs_capture_restart)


func _emit_toast_after_save(ok: bool, needs_capture_restart: bool) -> void:
	if not ok:
		_emit_toast(Mc.UI_TOAST_SETTING_SAVE_FAILED, false)
	elif needs_capture_restart:
		_emit_toast(Mc.UI_TOAST_SETTINGS_SAVED_RESTART, false)
	else:
		_emit_toast(Mc.UI_TOAST_SETTINGS_SAVED, false)


func _emit_toast(message: String, _needs_capture_restart: bool) -> void:
	settings_toast.emit(message)
