class_name HookDockTransportBar
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookDockVerticalLayoutGd = preload(
	"res://addons/mnemonic/ui/hook_dock_vertical_layout.gd"
)
const HookDockToolbarStyleGd = preload("res://addons/mnemonic/ui/hook_dock_toolbar_style.gd")
const HookDockStatusToastGd = preload("res://addons/mnemonic/ui/hook_dock_status_toast.gd")
const HookDockHostGd = preload("res://addons/mnemonic/ui/hook_dock_host.gd")

var host
var header_leading_spacer: Control
var transport_row: HBoxContainer
var btn_stop: Button
var btn_resume: Button
var on_sync_header_settings: Callable = Callable()
var on_sync_toolbar_heights: Callable = Callable()


func setup(p_host) -> void:
	host = p_host


func build(header: HBoxContainer, leading_spacer: Control) -> void:
	header_leading_spacer = leading_spacer
	transport_row = HBoxContainer.new()
	transport_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(transport_row)
	if host != null and host.theme != null:
		transport_row.add_theme_constant_override("separation", host.theme.row_separation_px())
	btn_stop = Button.new()
	btn_stop.text = Mc.UI_STOP_RECORDING
	btn_stop.tooltip_text = Mc.TOOLTIP_STOP_RECORDING
	btn_stop.pressed.connect(on_stop_pressed)
	HookDockToolbarStyleGd.apply_header_transport_button(host.theme if host != null else null, btn_stop)
	transport_row.add_child(btn_stop)
	btn_resume = Button.new()
	btn_resume.text = Mc.UI_START_RECORDING
	btn_resume.tooltip_text = Mc.TOOLTIP_START_RECORDING
	btn_resume.pressed.connect(on_resume_pressed)
	HookDockToolbarStyleGd.apply_header_transport_button(host.theme if host != null else null, btn_resume)
	transport_row.add_child(btn_resume)
	header.add_child(transport_row)


func update_buttons() -> void:
	if not is_instance_valid(btn_stop) or not is_instance_valid(btn_resume):
		return
	var paths_ok: bool = (
		host != null
		and host.plugin != null
		and host.plugin.get_data_root_paths() != null
		and host.plugin.get_data_root_paths().is_valid()
	)
	var core_running: bool = host != null and host.plugin != null and host.plugin.is_core_running()
	var show_stop := false
	var show_start := false
	if paths_ok:
		if core_running:
			var snap: HookStatusSnapshot = host.plugin.get_status_snapshot()
			if snap != null:
				var st: String = snap.state.strip_edges().to_lower()
				show_stop = st == Mc.CAPTURE_STATE_RECORDING and snap.recording
				show_start = not show_stop
			else:
				show_start = true
		else:
			show_start = true
	btn_stop.visible = show_stop
	btn_resume.visible = show_start
	var show_transport := paths_ok and (show_stop or show_start)
	if is_instance_valid(transport_row):
		transport_row.visible = show_transport
	if is_instance_valid(header_leading_spacer):
		header_leading_spacer.visible = not show_transport
	if on_sync_header_settings.is_valid():
		on_sync_header_settings.call_deferred()
	if on_sync_toolbar_heights.is_valid():
		on_sync_toolbar_heights.call_deferred()


func on_stop_pressed() -> void:
	if host == null or host.plugin == null:
		return
	var result: Dictionary = host.plugin.stop_recording_session()
	HookDockStatusToastGd.show(
		host,
		str(result.get("message", Mc.UI_TOAST_STOP_FAILED)),
	)


func on_resume_pressed() -> void:
	if host == null or host.plugin == null:
		return
	var result: Dictionary = host.plugin.start_recording_session()
	HookDockStatusToastGd.show(
		host,
		str(result.get("message", Mc.UI_TOAST_START_FAILED)),
	)


func apply_theme() -> void:
	if host == null or host.theme == null:
		return
	if is_instance_valid(btn_stop):
		host.theme.apply_body_font(btn_stop)
	if is_instance_valid(btn_resume):
		host.theme.apply_body_font(btn_resume)
