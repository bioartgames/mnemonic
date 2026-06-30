@tool
class_name EditorMnemonicHookDock
extends MarginContainer

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookDockThemeGd = preload("res://addons/mnemonic_hook/ui/hook_dock_theme.gd")
const HookSettingsPanelGd = preload("res://addons/mnemonic_hook/ui/hook_settings_panel.gd")
const HookClipsDrawerGripGd = preload("res://addons/mnemonic_hook/ui/hook_clips_drawer_grip.gd")
const HookDockVerticalLayoutGd = preload("res://addons/mnemonic_hook/ui/hook_dock_vertical_layout.gd")
const HookDockFilterPanelGd = preload("res://addons/mnemonic_hook/ui/hook_dock_filter_panel.gd")
const HookDockLiveSaveControllerGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_live_save_controller.gd"
)
const HookLiveSavePollGd = preload("res://addons/mnemonic_hook/ui/hook_live_save_poll.gd")
const HookDockHostGd = preload("res://addons/mnemonic_hook/ui/hook_dock_host.gd")
const HookDockToolbarStyleGd = preload("res://addons/mnemonic_hook/ui/hook_dock_toolbar_style.gd")
const HookDockStatusToastGd = preload("res://addons/mnemonic_hook/ui/hook_dock_status_toast.gd")
const HookDockSettingsMenuGd = preload("res://addons/mnemonic_hook/ui/hook_dock_settings_menu.gd")
const HookDockTransportBarGd = preload("res://addons/mnemonic_hook/ui/hook_dock_transport_bar.gd")
const HookDockClipsDrawerGd = preload("res://addons/mnemonic_hook/ui/hook_dock_clips_drawer.gd")
const HookDockSegmentLogPanelGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_segment_log_panel.gd"
)
const HookDockFilterControllerGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_filter_controller.gd"
)
const HookDockLiveSessionControllerGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_live_session_controller.gd"
)
const HookDockClipsListControllerGd = preload(
	"res://addons/mnemonic_hook/ui/hook_dock_clips_list_controller.gd"
)

var _plugin: EditorPlugin = null
var _theme: HookDockTheme = null
var _theme_changed_connected := false
var _host := HookDockHostGd.new()
var _transport := HookDockTransportBarGd.new()
var _settings_menu := HookDockSettingsMenuGd.new()
var _drawer := HookDockClipsDrawerGd.new()
var _segment_log := HookDockSegmentLogPanelGd.new()
var _filter_ctrl := HookDockFilterControllerGd.new()
var _live_session := HookDockLiveSessionControllerGd.new()
var _clips_list := HookDockClipsListControllerGd.new()
var _live_save := HookDockLiveSaveControllerGd.new()

var _dock_header: HBoxContainer
var _header_leading_spacer: Control
var _btn_settings: Button
var _settings_panel: EditorHookSettingsPanel
var _dock_body: Control
var _dock_top: VBoxContainer
var _clips_drawer_grip: Control
var _filter: HookDockFilterPanel
var _clips_list_panel: PanelContainer
var _clips_scroll_edge_margin: MarginContainer
var _clips_content_margin: MarginContainer
var _clips_scroll: ScrollContainer
var _clips_box: VBoxContainer
var _flag_status_label: Label
var _flag_clear_timer: Timer
var _live_save_poll_timer: Timer
var _live_segment_timer: Timer
var _delete_confirm: ConfirmationDialog
var _ui_built := false


func setup(plugin: EditorPlugin) -> void:
	_plugin = plugin
	_theme = HookDockThemeGd.new()
	_theme.setup(plugin.get_editor_interface())
	_host.plugin = plugin
	_host.theme = _theme
	name = "Mnemonic"
	custom_minimum_size = Vector2(Mc.DOCK_MIN_WIDTH_PX, 0)
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL
	clip_contents = true
	_ensure_ui_built()
	_connect_theme_changed()
	_apply_editor_theme()
	_clips_list.apply_responsive_layout(size.x)
	_refresh_ui_state()
	_clips_list.show_loading()


func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		_live_session.stop_countdown()
		_disconnect_theme_changed()
	elif what == NOTIFICATION_RESIZED:
		_clips_list.apply_responsive_layout(size.x)
		_drawer.layout_dock_top()
		_drawer.apply_offset(false)


func _connect_theme_changed() -> void:
	if _theme_changed_connected or _plugin == null:
		return
	var base := _plugin.get_editor_interface().get_base_control()
	if base == null:
		return
	if not base.theme_changed.is_connected(_on_editor_theme_changed):
		base.theme_changed.connect(_on_editor_theme_changed)
		_theme_changed_connected = true


func _disconnect_theme_changed() -> void:
	if not _theme_changed_connected or _plugin == null:
		return
	var base := _plugin.get_editor_interface().get_base_control()
	if base != null and base.theme_changed.is_connected(_on_editor_theme_changed):
		base.theme_changed.disconnect(_on_editor_theme_changed)
	_theme_changed_connected = false


func _on_editor_theme_changed() -> void:
	_apply_editor_theme()
	if is_instance_valid(_settings_panel):
		_settings_panel.apply_panel_theme()


func on_plugin_ready() -> void:
	if not _ui_built:
		return
	_refresh_ui_state()
	call_deferred("_deferred_reload_clips")


func _deferred_reload_clips() -> void:
	if not is_instance_valid(self) or not _ui_built:
		return
	_clips_list.reload(_clips_list.list_source_mode())


func refresh() -> void:
	if not _ui_built:
		return
	_refresh_ui_state()
	_clips_list.reload(_clips_list.list_source_mode())
	if is_instance_valid(_segment_log.outer) and _segment_log.outer.visible:
		_segment_log.reload()


func refresh_from_status_poll() -> void:
	if not _ui_built:
		return
	_refresh_ui_state()
	_clips_list.reload_if_needed()


func should_refresh_from_status_poll() -> bool:
	if not _ui_built:
		return false
	if _live_session.should_refresh_from_poll():
		return true
	return is_visible_in_tree()


func show_workflow_toast(message: String) -> void:
	HookDockStatusToastGd.show(_host, message)


func _refresh_ui_state() -> void:
	_transport.update_buttons()
	_settings_menu.sync_state()


func _apply_editor_theme() -> void:
	if _theme == null:
		return
	var m: int = _theme.margin_px()
	add_theme_constant_override("margin_left", m)
	add_theme_constant_override("margin_right", m)
	add_theme_constant_override("margin_top", m)
	add_theme_constant_override("margin_bottom", m)
	if is_instance_valid(_clips_list_panel):
		_clips_list_panel.add_theme_stylebox_override("panel", _theme.make_clips_drawer_panel())
	if is_instance_valid(_flag_status_label):
		_theme.apply_toast_label(_flag_status_label)
	_filter_ctrl.apply_theme()
	_segment_log.apply_theme()
	_settings_menu.apply_theme()
	_transport.apply_theme()
	if is_instance_valid(_clips_drawer_grip):
		_clips_drawer_grip.setup(_theme)
	if is_instance_valid(_clips_scroll):
		_theme.apply_clips_scroll(_clips_scroll, _clips_content_margin, _clips_scroll_edge_margin)
	call_deferred("_sync_clips_toolbar_button_heights")
	call_deferred("_sync_header_settings_button_size")


func _ensure_ui_built() -> void:
	if _ui_built:
		return

	var root_vbox := VBoxContainer.new()
	root_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root_vbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	if _theme != null:
		root_vbox.add_theme_constant_override("separation", _theme.action_separation_px())
	add_child(root_vbox)

	_dock_header = HBoxContainer.new()
	_dock_header.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(_dock_header)
	if _theme != null:
		_dock_header.add_theme_constant_override("separation", _theme.row_separation_px())

	_header_leading_spacer = Control.new()
	_header_leading_spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_header_leading_spacer.visible = false
	_dock_header.add_child(_header_leading_spacer)

	_transport.setup(_host)
	_transport.on_sync_header_settings = Callable(self, "_sync_header_settings_button_size")
	_transport.on_sync_toolbar_heights = Callable(self, "_sync_clips_toolbar_button_heights")
	_transport.build(_dock_header, _header_leading_spacer)

	_btn_settings = Button.new()
	_btn_settings.text = ""
	_btn_settings.tooltip_text = Mc.TOOLTIP_SETTINGS_GEAR
	_btn_settings.size_flags_horizontal = Control.SIZE_SHRINK_END
	_btn_settings.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_dock_header.add_child(_btn_settings)

	_dock_body = Control.new()
	_dock_body.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_dock_body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_dock_body.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_dock_body.resized.connect(_on_dock_body_resized)
	root_vbox.add_child(_dock_body)
	_host.dock_body = _dock_body

	_dock_top = VBoxContainer.new()
	_dock_top.mouse_filter = Control.MOUSE_FILTER_PASS
	if _theme != null:
		_dock_top.add_theme_constant_override("separation", _theme.action_separation_px())
	_dock_top.minimum_size_changed.connect(_on_dock_top_minimum_size_changed)
	_dock_body.add_child(_dock_top)
	_host.dock_top = _dock_top

	_dock_top.add_child(_dock_header)

	_filter = HookDockFilterPanelGd.build(_theme, self)
	_filter_ctrl.setup(_host, _filter)
	_dock_top.add_child(_filter.filter_row)
	_dock_top.add_child(_filter.outer)

	_segment_log.host = _host
	_segment_log.drawer = _drawer
	_segment_log.on_layout_changed = Callable(self, "_on_panel_layout_changed")
	_segment_log.mount(_dock_top, self)

	_settings_panel = HookSettingsPanelGd.new()
	_settings_panel.setup(_plugin, _theme)
	_settings_panel.settings_toast.connect(_on_settings_toast)
	add_child(_settings_panel)

	_settings_menu.setup(_host, _btn_settings, _settings_panel)
	_settings_menu.on_capture_panel = func() -> void:
		if is_instance_valid(_settings_panel):
			_settings_panel.popup_below_anchor(_btn_settings)
	_settings_menu.build_menus(self)

	_clips_list_panel = PanelContainer.new()
	_clips_list_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_clips_list_panel.mouse_filter = Control.MOUSE_FILTER_PASS
	_dock_body.add_child(_clips_list_panel)
	_host.clips_list_panel = _clips_list_panel

	var clips_inner := VBoxContainer.new()
	clips_inner.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	clips_inner.size_flags_vertical = Control.SIZE_EXPAND_FILL
	if _theme != null:
		clips_inner.add_theme_constant_override("separation", 0)
	_clips_list_panel.add_child(clips_inner)

	_clips_drawer_grip = HookClipsDrawerGripGd.new()
	_clips_drawer_grip.setup(_theme)
	_clips_drawer_grip.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_clips_drawer_grip.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_clips_drawer_grip.mouse_filter = Control.MOUSE_FILTER_STOP
	_clips_drawer_grip.mouse_default_cursor_shape = Control.CURSOR_VSPLIT
	clips_inner.add_child(_clips_drawer_grip)

	_clips_scroll_edge_margin = MarginContainer.new()
	_clips_scroll_edge_margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_clips_scroll_edge_margin.size_flags_vertical = Control.SIZE_EXPAND_FILL
	clips_inner.add_child(_clips_scroll_edge_margin)

	_clips_scroll = ScrollContainer.new()
	_clips_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_clips_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_clips_scroll.custom_minimum_size = Vector2(0, Mc.CLIPS_SCROLL_MIN_HEIGHT_PX)
	_clips_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_clips_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	_clips_scroll_edge_margin.add_child(_clips_scroll)

	_clips_content_margin = MarginContainer.new()
	_clips_content_margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_clips_scroll.add_child(_clips_content_margin)

	_clips_box = VBoxContainer.new()
	_clips_box.custom_minimum_size = Vector2.ZERO
	_clips_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if _theme != null:
		_clips_box.add_theme_constant_override("separation", _theme.row_separation_px())
	_clips_content_margin.add_child(_clips_box)
	_host.clips_box = _clips_box
	if _theme != null:
		_theme.apply_clips_scroll(_clips_scroll, _clips_content_margin, _clips_scroll_edge_margin)

	_flag_status_label = Label.new()
	_flag_status_label.text = ""
	HookDockVerticalLayoutGd.apply_label_center(_flag_status_label)
	root_vbox.add_child(_flag_status_label)

	_flag_clear_timer = Timer.new()
	_flag_clear_timer.one_shot = true
	_flag_clear_timer.wait_time = 3.0
	_flag_clear_timer.timeout.connect(_on_flag_clear_timeout)
	add_child(_flag_clear_timer)
	_host.toast_label = _flag_status_label
	_host.toast_timer = _flag_clear_timer

	_live_save_poll_timer = Timer.new()
	_live_save_poll_timer.wait_time = Mc.LIVE_SAVE_FLAG_POLL_SEC
	_live_save_poll_timer.one_shot = false
	_live_save_poll_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	_live_save_poll_timer.timeout.connect(_on_live_save_poll_tick)
	add_child(_live_save_poll_timer)

	_live_segment_timer = Timer.new()
	_live_segment_timer.wait_time = 1.0
	_live_segment_timer.one_shot = false
	_live_segment_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	_live_segment_timer.timeout.connect(_on_live_segment_timer_tick)
	add_child(_live_segment_timer)

	_delete_confirm = ConfirmationDialog.new()
	_delete_confirm.title = Mc.UI_DIALOG_DELETE_CLIP_TITLE
	_delete_confirm.ok_button_text = "Delete"
	_delete_confirm.cancel_button_text = "Cancel"
	add_child(_delete_confirm)

	_drawer.setup(_host, _clips_drawer_grip, _clips_list_panel)
	_drawer.set_transport_refs(_transport.transport_row, _transport.btn_stop, _transport.btn_resume)
	_drawer.set_segment_log_toggle(_segment_log.toggle_btn)

	_live_session.setup(_host, _live_segment_timer, _live_save)

	_clips_list.setup(
		_host,
		_filter_ctrl,
		_live_session,
		_live_save,
		_clips_box,
		_delete_confirm,
		_live_save_poll_timer,
		Callable(_segment_log, "reload"),
		func(persist: bool) -> void: _drawer.apply_offset(persist),
		self,
	)

	_filter_ctrl.on_request_reload = func() -> void:
		_clips_list.reload(_clips_list.list_source_mode())
	_filter_ctrl.on_refresh_pressed = func() -> void:
		_clips_list.on_refresh_pressed(self)
	_filter_ctrl.on_set_reload_pending = func() -> void:
		_clips_list.reload_pending = true
	_filter_ctrl.on_layout_changed = Callable(self, "_on_panel_layout_changed")
	_filter_ctrl.any_clip_row_popup_visible = Callable(_clips_list, "any_clip_row_popup_visible")
	_filter_ctrl.connect_signals()
	_filter_ctrl.update_clear_button()

	_apply_body_fonts_to_static_controls()
	_ui_built = true
	_clips_list.apply_responsive_layout(size.x)
	call_deferred("_layout_deferred")


func _layout_deferred() -> void:
	_drawer.layout_dock_top()
	_drawer.apply_offset(false)
	_sync_clips_toolbar_button_heights()
	_sync_header_settings_button_size()


func _on_dock_body_resized() -> void:
	_drawer.on_body_resized()


func _on_dock_top_minimum_size_changed() -> void:
	_drawer.on_top_minimum_size_changed()


func _on_panel_layout_changed() -> void:
	call_deferred("_layout_deferred")


func _sync_clips_toolbar_button_heights() -> void:
	_drawer.sync_toolbar_button_heights(
		_filter.btn_toggle,
		_segment_log.toggle_btn,
		_segment_log.clear_btn,
		_filter.btn_refresh,
	)


func _sync_header_settings_button_size() -> void:
	_drawer.sync_header_settings_button_size(_btn_settings)


func _on_settings_toast(message: String) -> void:
	HookDockStatusToastGd.show(_host, message)


func _on_flag_clear_timeout() -> void:
	HookDockStatusToastGd.clear(_host)


func _on_live_segment_timer_tick() -> void:
	_live_session.on_segment_timer_tick()


func _on_live_save_poll_tick() -> void:
	if not _live_save.pending or _plugin == null:
		_stop_live_save_poll()
		return
	var poll := _live_save.poll_tick(_plugin)
	match poll.get("outcome", HookLiveSavePollGd.Outcome.PENDING):
		HookLiveSavePollGd.Outcome.FAIL_PATHS:
			_finish_live_save(false)
		HookLiveSavePollGd.Outcome.FAIL_TIMEOUT:
			_finish_live_save(false)
		HookLiveSavePollGd.Outcome.ACKNOWLEDGED:
			_live_session.update_rec_indicator_tier()
			_live_session.sync_live_row_tooltips()
		HookLiveSavePollGd.Outcome.COMPLETE:
			_finish_live_save(true)
		_:
			pass


func _stop_live_save_poll() -> void:
	if is_instance_valid(_live_save_poll_timer):
		_live_save_poll_timer.stop()


func _finish_live_save(success: bool) -> void:
	_live_save.reset()
	_stop_live_save_poll()
	if not success:
		_clips_list.flash_live_save_error_on_panel()
	_clips_list.refresh_for_live_save_ui()


func _apply_body_fonts_to_static_controls() -> void:
	if _theme == null:
		return
	_theme.apply_toast_label(_flag_status_label)
	for node in [
		_transport.btn_stop,
		_transport.btn_resume,
		_filter.btn_toggle,
		_segment_log.toggle_btn,
		_filter.btn_clear,
		_filter.btn_refresh,
		_segment_log.clear_btn,
		_filter.search,
	]:
		if node is Button:
			if node == _filter.btn_toggle or node == _segment_log.toggle_btn:
				_theme.apply_disclosure_button(node)
			elif node == _filter.btn_clear:
				_theme.apply_full_width_button(node)
			elif (
				node == _filter.btn_refresh
				or node == _btn_settings
				or node == _segment_log.clear_btn
			):
				HookDockToolbarStyleGd.apply_toolbar_icon_button(_theme, node)
			elif node.size_flags_horizontal & Control.SIZE_EXPAND_FILL:
				_theme.apply_full_width_button(node)
			else:
				_theme.apply_body_font(node)
		elif node is Control:
			_theme.apply_body_font(node)
