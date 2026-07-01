class_name HookLiveSaveOverlay
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const PulseShader = preload("res://addons/mnemonic/ui/hook_live_save_pulse.gdshader")

const _LAYOUT_RETRY_MAX := 10


static func attach(panel: PanelContainer, theme: HookDockTheme) -> Dictionary:
	if panel == null or theme == null or not is_instance_valid(panel):
		return {}
	var existing := panel.get_node_or_null(^"LiveSaveOverlay")
	if existing != null:
		existing.queue_free()
	var wrapper := Control.new()
	wrapper.name = &"LiveSaveOverlay"
	wrapper.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	wrapper.mouse_filter = Control.MOUSE_FILTER_IGNORE
	wrapper.z_index = 1
	var fill := ColorRect.new()
	fill.name = &"Fill"
	fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var accent := theme.color_editor(&"accent_color")
	accent.a = 0.35
	fill.color = accent
	fill.modulate.a = 0.0
	var mat := ShaderMaterial.new()
	mat.shader = PulseShader
	mat.set_shader_parameter("pulse_min", Mc.LIVE_SAVE_PULSE_MIN)
	mat.set_shader_parameter("pulse_max", Mc.LIVE_SAVE_PULSE_MAX)
	mat.set_shader_parameter("pulse_speed", Mc.LIVE_SAVE_PULSE_SPEED)
	fill.material = mat
	wrapper.add_child(fill)
	_layout_fill_full_rect(fill)
	panel.add_child(wrapper)
	panel.move_child(wrapper, panel.get_child_count() - 1)
	return {
		"root": wrapper,
		"fill": fill,
		"shader_mat": mat,
		"tween": null,
		"panel": panel,
		"peak_modulate_a": 1.0,
		"_pulse_started": false,
	}


static func start_indeterminate(overlay: Dictionary) -> void:
	start_pending_pulse(overlay)


static func start_pending_pulse(overlay: Dictionary) -> void:
	if not _overlay_refs_valid(overlay):
		return
	if overlay.get("_pulse_started", false):
		return
	overlay["_pulse_started"] = true
	overlay["_layout_wait_frames"] = 0
	_kill_overlay_tween(overlay)
	_begin_pulse(overlay)


## Drop overlay without fade; safe when clip rows are rebuilt (nodes may already be freed).
static func abandon(overlay: Dictionary) -> void:
	_kill_overlay_tween(overlay)
	_free_overlay(overlay)


static func stop(overlay: Dictionary) -> void:
	if overlay.is_empty():
		return
	_kill_overlay_tween(overlay)
	if not _overlay_refs_valid(overlay):
		_free_overlay(overlay)
		return
	var fill: ColorRect = overlay.get("fill")
	var panel: PanelContainer = overlay.get("panel")
	if fill.modulate.a <= 0.01:
		_free_overlay(overlay)
		return
	var tween := panel.create_tween()
	overlay["tween"] = tween
	var step := tween.tween_property(fill, "modulate:a", 0.0, Mc.LIVE_SAVE_FADE_OUT_SEC)
	if step != null:
		step.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
	tween.tween_callback(func() -> void:
		_free_overlay(overlay)
	)


static func flash_error(overlay: Dictionary, theme: HookDockTheme) -> void:
	if not _overlay_refs_valid(overlay) or theme == null:
		abandon(overlay)
		return
	var fill: ColorRect = overlay.get("fill")
	var panel: PanelContainer = overlay.get("panel")
	_kill_overlay_tween(overlay)
	overlay["_pulse_started"] = true
	var err_color := theme.color_editor(&"error_color")
	err_color.a = 0.45
	fill.color = err_color
	fill.modulate.a = 1.0
	var tween := panel.create_tween()
	overlay["tween"] = tween
	tween.tween_interval(0.35)
	tween.tween_callback(func() -> void:
		stop(overlay)
	)


static func _overlay_refs_valid(overlay: Dictionary) -> bool:
	var fill = overlay.get("fill")
	var panel = overlay.get("panel")
	if fill == null or panel == null:
		return false
	if not is_instance_valid(fill) or not is_instance_valid(panel):
		return false
	return true


static func _free_overlay(overlay: Dictionary) -> void:
	var root: Control = overlay.get("root")
	if root != null and is_instance_valid(root):
		root.queue_free()
	overlay.clear()


static func _kill_overlay_tween(overlay: Dictionary) -> void:
	var tween: Tween = overlay.get("tween")
	if tween != null and tween.is_valid():
		tween.kill()
	overlay["tween"] = null


static func _layout_fill_full_rect(fill: ColorRect) -> void:
	fill.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


static func _panel_has_layout(panel: PanelContainer) -> bool:
	var w := panel.size.x
	if w < 2.0:
		w = maxf(panel.get_combined_minimum_size().x, panel.custom_minimum_size.x)
	if w >= 2.0:
		return true
	var node: Node = panel.get_parent()
	while node is Control:
		var c := node as Control
		w = maxf(c.size.x, maxf(c.get_combined_minimum_size().x, c.custom_minimum_size.x))
		if w >= 2.0:
			return true
		node = node.get_parent()
	return false


static func _begin_pulse(overlay: Dictionary) -> void:
	if not _overlay_refs_valid(overlay):
		return
	var fill: ColorRect = overlay.get("fill")
	var panel: PanelContainer = overlay.get("panel")
	if not _panel_has_layout(panel):
		var waits := int(overlay.get("_layout_wait_frames", 0))
		if waits < _LAYOUT_RETRY_MAX:
			overlay["_layout_wait_frames"] = waits + 1
			var tree := panel.get_tree()
			if tree != null:
				tree.process_frame.connect(
					func() -> void:
						if overlay.is_empty():
							return
						_begin_pulse(overlay),
					CONNECT_ONE_SHOT,
				)
			return
	var peak_a: float = float(overlay.get("peak_modulate_a", 1.0))
	_layout_fill_full_rect(fill)
	fill.modulate.a = 0.0
	var tween := panel.create_tween()
	overlay["tween"] = tween
	var fade_in := tween.tween_property(fill, "modulate:a", peak_a, Mc.LIVE_SAVE_FADE_IN_SEC)
	if fade_in != null:
		fade_in.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	tween.tween_callback(func() -> void:
		if overlay.is_empty():
			return
		overlay["tween"] = null
	)
