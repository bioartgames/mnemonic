class_name HookDockClipsDrawer
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookDockLayoutGd = preload("res://addons/mnemonic_hook/ui/hook_dock_layout.gd")
const HookDockHostGd = preload("res://addons/mnemonic_hook/ui/hook_dock_host.gd")

var host
var grip: Control
var clips_list_panel: PanelContainer
var drawer_top_px: int = Mc.DOCK_SPLIT_UNSET
var drag_active := false
var drag_pending := false
var drag_start_y := 0.0
var drag_start_offset := 0
var transport_row: HBoxContainer
var btn_stop: Button
var btn_resume: Button
var segment_log_toggle: Button


func setup(
	p_host,
	p_grip: Control,
	p_clips_list_panel: PanelContainer,
) -> void:
	host = p_host
	grip = p_grip
	clips_list_panel = p_clips_list_panel
	if is_instance_valid(grip):
		grip.gui_input.connect(on_grip_gui_input)


func set_transport_refs(row: HBoxContainer, stop_btn: Button, resume_btn: Button) -> void:
	transport_row = row
	btn_stop = stop_btn
	btn_resume = resume_btn


func set_segment_log_toggle(btn: Button) -> void:
	segment_log_toggle = btn


func layout_dock_top() -> void:
	if host == null or not is_instance_valid(host.dock_body) or not is_instance_valid(host.dock_top):
		return
	var width := maxi(1, int(host.dock_body.size.x))
	var height := int(host.dock_top.get_combined_minimum_size().y)
	host.dock_top.position = Vector2.ZERO
	host.dock_top.size = Vector2(width, height)


func apply_offset(persist: bool) -> void:
	if host == null or not is_instance_valid(host.dock_body) or not is_instance_valid(clips_list_panel):
		return
	var total := int(host.dock_body.size.y)
	if total <= 0:
		return
	var top_px := drawer_top_px
	if top_px == Mc.DOCK_SPLIT_UNSET:
		top_px = HookDockLayoutGd.read_split_offset()
		if top_px == Mc.DOCK_SPLIT_UNSET:
			top_px = _default_clips_drawer_top_px(total)
	top_px = HookDockLayoutGd.clamp_split_offset(
		top_px,
		total,
		Mc.DOCK_SPLIT_TOP_MIN_PX,
		clips_drawer_min_height_px(),
	)
	drawer_top_px = top_px
	clips_list_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	clips_list_panel.offset_top = top_px
	clips_list_panel.offset_bottom = 0
	clips_list_panel.offset_left = 0
	clips_list_panel.offset_right = 0
	clips_list_panel.visible = true
	clips_list_panel.move_to_front()
	if persist:
		HookDockLayoutGd.write_split_offset(top_px)


func on_body_resized() -> void:
	layout_dock_top()
	apply_offset(false)


func on_top_minimum_size_changed() -> void:
	layout_dock_top()
	apply_offset(false)


func on_grip_gui_input(event: InputEvent) -> void:
	if host == null or not is_instance_valid(host.dock_body):
		return
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index != MOUSE_BUTTON_LEFT:
			return
		if mb.pressed:
			if mb.double_click:
				drag_pending = false
				drag_active = false
				on_double_click_toggle()
				if is_instance_valid(grip):
					grip.accept_event()
				return
			drag_pending = true
			drag_active = false
			drag_start_y = mb.global_position.y
			drag_start_offset = drawer_top_px
			if drag_start_offset == Mc.DOCK_SPLIT_UNSET:
				drag_start_offset = clips_list_panel.offset_top
		else:
			drag_pending = false
			if drag_active:
				drag_active = false
				apply_offset(true)
	elif event is InputEventMouseMotion:
		var motion := event as InputEventMouseMotion
		if drag_pending:
			if absf(motion.global_position.y - drag_start_y) >= float(
				Mc.DOCK_SPLIT_COLLAPSED_TOLERANCE_PX
			):
				drag_active = true
				drag_pending = false
		if drag_active:
			var delta := int(motion.global_position.y - drag_start_y)
			drawer_top_px = drag_start_offset + delta
			apply_offset(false)


func on_double_click_toggle() -> void:
	if host == null or not is_instance_valid(host.dock_body):
		return
	var total := int(host.dock_body.size.y)
	if total <= 0:
		return
	var current := drawer_top_px
	if current == Mc.DOCK_SPLIT_UNSET:
		current = clips_list_panel.offset_top
	drawer_top_px = HookDockLayoutGd.clips_drawer_double_click_top_px(
		current,
		total,
		_dock_top_height_px(),
		clips_drawer_min_height_px(),
	)
	apply_offset(true)


func sync_toolbar_button_heights(
	filter_toggle: Button,
	segment_log_btn: Button,
	clear_seg_btn: Button,
	filter_refresh: Button,
) -> void:
	if not is_instance_valid(filter_toggle) or not is_instance_valid(filter_refresh):
		return
	var primary_h := _clips_toolbar_primary_button_height()
	if primary_h <= 0:
		return
	var icon_w := _toolbar_icon_button_width()
	filter_toggle.custom_minimum_size = Vector2(0, primary_h)
	if is_instance_valid(segment_log_btn):
		segment_log_btn.custom_minimum_size = Vector2(0, primary_h)
	filter_refresh.custom_minimum_size = Vector2(icon_w, primary_h)
	if is_instance_valid(clear_seg_btn):
		clear_seg_btn.custom_minimum_size = Vector2(icon_w, primary_h)


func sync_header_settings_button_size(settings_btn: Button) -> void:
	if not is_instance_valid(settings_btn):
		return
	var ref_btn: Control = btn_resume if is_instance_valid(btn_resume) and btn_resume.visible else transport_row
	if ref_btn == null:
		return
	var btn_h := int(ref_btn.get_combined_minimum_size().y)
	if btn_h <= 0 and is_instance_valid(transport_row):
		btn_h = int(transport_row.get_combined_minimum_size().y)
	if btn_h <= 0:
		return
	var btn_w := _toolbar_icon_button_width()
	settings_btn.custom_minimum_size = Vector2(btn_w, btn_h)


func clips_drawer_min_height_px() -> int:
	var min_height := Mc.DOCK_SPLIT_BOTTOM_CHROME_MIN_PX
	var live_row_height := live_row_min_height_px()
	if live_row_height > 0:
		var grip_height := _clips_drawer_grip_height_px()
		var padding := scaled_px(2)
		min_height = maxi(min_height, grip_height + live_row_height + padding)
	return min_height


func live_row_min_height_px() -> int:
	if host == null or not is_instance_valid(host.clips_box):
		return 0
	for child in host.clips_box.get_children():
		if not (child is Control):
			continue
		var row := child as Control
		if not bool(row.get_meta(&"mnemonic_live_row", false)):
			continue
		var min_h := int(row.get_combined_minimum_size().y)
		if min_h <= 0:
			min_h = int(row.size.y)
		return maxi(0, min_h)
	return 0


func scaled_px(base_px: int) -> int:
	if host != null and host.theme != null:
		return maxi(1, int(round(float(base_px) * float(host.theme.get_scale()))))
	return base_px


func _default_clips_drawer_top_px(total_height_px: int) -> int:
	if total_height_px <= 0:
		return 0
	return int(float(total_height_px) * Mc.DOCK_SPLIT_DEFAULT_RATIO)


func _dock_top_height_px() -> int:
	if host == null or not is_instance_valid(host.dock_top):
		return 0
	var height := int(host.dock_top.size.y)
	if height <= 0:
		height = int(host.dock_top.get_combined_minimum_size().y)
	return maxi(0, height)


func _clips_drawer_grip_height_px() -> int:
	if not is_instance_valid(grip):
		return Mc.DOCK_SPLIT_BOTTOM_CHROME_MIN_PX
	var grip_min := int(grip.get_combined_minimum_size().y)
	if grip_min <= 0:
		grip_min = int(grip.size.y)
	return maxi(Mc.DOCK_SPLIT_BOTTOM_CHROME_MIN_PX, grip_min)


func _clips_toolbar_primary_button_height() -> int:
	for btn in [btn_stop, btn_resume, segment_log_toggle]:
		if btn is Button and is_instance_valid(btn) and btn.visible:
			var h := int(btn.get_combined_minimum_size().y)
			if h > 0:
				return h
	if is_instance_valid(transport_row):
		var row_h := int(transport_row.get_combined_minimum_size().y)
		if row_h > 0:
			return row_h
	return 0


func _toolbar_icon_button_width() -> int:
	if host != null and host.theme != null:
		return maxi(36, int(36.0 * host.theme.get_scale()))
	return 36
