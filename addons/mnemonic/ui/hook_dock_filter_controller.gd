class_name HookDockFilterController
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookClipFilterCriteriaGd = preload(
	"res://addons/mnemonic/clips/hook_clip_filter_criteria.gd"
)
const HookDockToolbarStyleGd = preload("res://addons/mnemonic/ui/hook_dock_toolbar_style.gd")
const HookDockHostGd = preload("res://addons/mnemonic/ui/hook_dock_host.gd")
const HookDockFilterPanelGd = preload("res://addons/mnemonic/ui/hook_dock_filter_panel.gd")

var host
var panel
var criteria
var on_request_reload: Callable = Callable()
var on_refresh_pressed: Callable = Callable()
var on_layout_changed: Callable = Callable()
var any_clip_row_popup_visible: Callable = Callable()
var on_set_reload_pending: Callable = Callable()


func setup(p_host, p_panel) -> void:
	host = p_host
	panel = p_panel
	criteria = HookClipFilterCriteriaGd.new()


func connect_signals() -> void:
	if not is_instance_valid(panel):
		return
	panel.btn_toggle.pressed.connect(_on_toggle_pressed)
	panel.btn_refresh.pressed.connect(_on_refresh_pressed)
	panel.search.text_changed.connect(_on_text_changed)
	panel.date_from.date_changed.connect(_on_date_changed)
	panel.date_to.date_changed.connect(_on_date_changed)
	panel.tier_option.item_selected.connect(_on_tier_selected)
	panel.apply_timer.timeout.connect(apply_filters_from_form)
	panel.btn_clear.pressed.connect(on_clear_pressed)


func _on_toggle_pressed() -> void:
	if is_instance_valid(panel.outer):
		panel.outer.visible = not panel.outer.visible
	if on_layout_changed.is_valid():
		on_layout_changed.call_deferred()


func _on_refresh_pressed() -> void:
	if on_refresh_pressed.is_valid():
		on_refresh_pressed.call()
	elif on_request_reload.is_valid():
		on_request_reload.call()


func _on_text_changed(_unused = null) -> void:
	if not is_instance_valid(panel.apply_timer):
		apply_filters_from_form()
		return
	panel.apply_timer.start()


func _on_date_changed(_unused = null) -> void:
	if is_instance_valid(panel.apply_timer):
		panel.apply_timer.stop()
	apply_filters_from_form()


func _on_tier_selected(_index: int) -> void:
	if is_instance_valid(panel.apply_timer):
		panel.apply_timer.stop()
	apply_filters_from_form()


func apply_filters_from_form() -> void:
	if not read_fields_into_criteria():
		return
	update_clear_button()
	_request_reload_with_popup_deferral()


func _request_reload_with_popup_deferral() -> void:
	if any_clip_row_popup_visible.is_valid() and any_clip_row_popup_visible.call():
		if on_set_reload_pending.is_valid():
			on_set_reload_pending.call()
		return
	if on_request_reload.is_valid():
		on_request_reload.call()


func read_fields_into_criteria() -> bool:
	criteria.search_query = panel.search.text.strip_edges()
	criteria.from_unix = -1
	criteria.to_unix = -1

	var from_text: String = panel.date_from.get_date_iso()
	if not from_text.is_empty():
		criteria.from_unix = HookClipFilterCriteriaGd.parse_date_start_utc(from_text)

	var to_text: String = panel.date_to.get_date_iso()
	if not to_text.is_empty():
		criteria.to_unix = HookClipFilterCriteriaGd.parse_date_end_utc(to_text)

	if is_instance_valid(panel.tier_option):
		criteria.significance_tier_filter = significance_tier_filter_from_option_index(
			panel.tier_option.selected
		)
	else:
		criteria.significance_tier_filter = ""

	return true


func filters_are_active() -> bool:
	return criteria != null and not criteria.is_empty()


func sync_button_state() -> void:
	if host == null or host.theme == null or not is_instance_valid(panel.btn_toggle):
		return
	host.theme.apply_filter_button_active(panel.btn_toggle, filters_are_active())


func update_clear_button() -> void:
	if not is_instance_valid(panel.btn_clear):
		return
	panel.btn_clear.disabled = not filters_are_active()
	sync_button_state()


func on_clear_pressed() -> void:
	if is_instance_valid(panel.apply_timer):
		panel.apply_timer.stop()
	panel.search.set_block_signals(true)
	panel.search.text = ""
	panel.search.set_block_signals(false)
	panel.date_from.clear()
	panel.date_to.clear()
	criteria.search_query = ""
	criteria.from_unix = -1
	criteria.to_unix = -1
	criteria.significance_tier_filter = ""
	if is_instance_valid(panel.tier_option):
		panel.tier_option.select(0)
	_request_reload_with_popup_deferral()
	update_clear_button()


func apply_theme() -> void:
	if host == null or host.theme == null or not is_instance_valid(panel):
		return
	HookDockToolbarStyleGd.apply_disclosure_button(host.theme, panel.btn_toggle)
	HookDockToolbarStyleGd.apply_toolbar_icon_button(host.theme, panel.btn_refresh)
	HookDockToolbarStyleGd.apply_full_width_button(host.theme, panel.btn_clear)
	if is_instance_valid(panel.outer):
		panel.outer.add_theme_stylebox_override("panel", host.theme.make_filter_panel())
	var reload_icon: Texture2D = host.theme.icon(&"Reload")
	if reload_icon != null and is_instance_valid(panel.btn_refresh):
		panel.btn_refresh.icon = reload_icon
	if is_instance_valid(panel.date_from):
		panel.date_from.apply_dock_theme(host.theme)
	if is_instance_valid(panel.date_to):
		panel.date_to.apply_dock_theme(host.theme)
	sync_button_state()


func significance_tier_filter_from_option_index(index: int) -> String:
	match index:
		1:
			return "highlight"
		2:
			return "notable"
		3:
			return "manual"
		_:
			return ""
