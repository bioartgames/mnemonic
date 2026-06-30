class_name HookLiveRecIndicator
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookClipThumbnailGd = preload("res://addons/mnemonic_hook/clips/hook_clip_thumbnail.gd")
const HookSignificanceTierGd = preload("res://addons/mnemonic_hook/clips/hook_significance_tier.gd")
const HookDockThemeGd = preload("res://addons/mnemonic_hook/ui/hook_dock_theme.gd")
const PulseShader = preload("res://addons/mnemonic_hook/ui/hook_live_save_pulse.gdshader")


static func build(thumb_size: Vector2, theme: HookDockTheme) -> Dictionary:
	var root := Control.new()
	root.custom_minimum_size = thumb_size
	root.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	root.size_flags_vertical = Control.SIZE_SHRINK_CENTER

	var thumb_rect := TextureRect.new()
	HookClipThumbnailGd.apply_placeholder(thumb_rect)
	thumb_rect.custom_minimum_size = thumb_size
	thumb_rect.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.add_child(thumb_rect)

	var dot_wrap := CenterContainer.new()
	dot_wrap.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	dot_wrap.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(dot_wrap)

	var dot_px := Mc.LIVE_REC_DOT_PX
	var dot_panel := PanelContainer.new()
	dot_panel.custom_minimum_size = Vector2(dot_px, dot_px)
	dot_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var dot_style := StyleBoxFlat.new()
	dot_style.bg_color = Color(0, 0, 0, 0)
	dot_style.border_width_left = 0
	dot_style.border_width_top = 0
	dot_style.border_width_right = 0
	dot_style.border_width_bottom = 0
	var radius := int(dot_px / 2)
	dot_style.corner_radius_top_left = radius
	dot_style.corner_radius_top_right = radius
	dot_style.corner_radius_bottom_left = radius
	dot_style.corner_radius_bottom_right = radius
	dot_panel.add_theme_stylebox_override("panel", dot_style)
	dot_panel.clip_contents = true
	dot_wrap.add_child(dot_panel)

	var dot_fill := ColorRect.new()
	dot_fill.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	dot_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	dot_fill.color = HookSignificanceTierGd.neutral_rec_dot_color(theme)
	var shader_mat := ShaderMaterial.new()
	shader_mat.shader = PulseShader
	shader_mat.set_shader_parameter("pulse_min", Mc.LIVE_REC_PULSE_MIN)
	shader_mat.set_shader_parameter("pulse_max", Mc.LIVE_REC_PULSE_MAX)
	shader_mat.set_shader_parameter("pulse_speed", Mc.LIVE_REC_PULSE_SPEED)
	dot_fill.material = shader_mat
	dot_panel.add_child(dot_fill)

	return {
		"root": root,
		"thumb_rect": thumb_rect,
		"dot_fill": dot_fill,
		"shader_mat": shader_mat,
		"tier_id": "",
		"close_phase": "",
	}


static func apply_tier(
	state: Dictionary,
	tier_id: String,
	theme: HookDockTheme,
	close_phase: String = "",
) -> void:
	if state.is_empty():
		return
	var dot_fill: ColorRect = state.get("dot_fill")
	var shader_mat: ShaderMaterial = state.get("shader_mat")
	if not is_instance_valid(dot_fill) or shader_mat == null:
		return

	var normalized_tier := tier_id.strip_edges()
	var normalized_phase := close_phase.strip_edges()
	var tier_changed := str(state.get("tier_id", "")) != normalized_tier
	var phase_changed := str(state.get("close_phase", "")) != normalized_phase

	if tier_changed:
		if normalized_tier.is_empty():
			dot_fill.color = HookSignificanceTierGd.neutral_rec_dot_color(theme)
		elif normalized_tier == Mc.LIVE_REC_TIER_QUEUED_SAVE:
			dot_fill.color = HookSignificanceTierGd.queued_save_rec_dot_color(theme)
		else:
			dot_fill.color = HookSignificanceTierGd.tier_border_color(normalized_tier, theme)
		state["tier_id"] = normalized_tier

	if phase_changed or tier_changed:
		var speed := float(Mc.LIVE_REC_PULSE_SPEED)
		if not normalized_phase.is_empty():
			speed *= float(Mc.LIVE_REC_CLOSE_PHASE_PULSE_SPEED_MULT)
		shader_mat.set_shader_parameter("pulse_speed", speed)
		state["close_phase"] = normalized_phase


static func resize_thumb(state: Dictionary, thumb_size: Vector2) -> void:
	if state.is_empty():
		return
	var root: Control = state.get("root")
	var thumb_rect: TextureRect = state.get("thumb_rect")
	if is_instance_valid(root):
		root.custom_minimum_size = thumb_size
	if is_instance_valid(thumb_rect):
		HookClipThumbnailGd.apply_placeholder(thumb_rect)
		thumb_rect.custom_minimum_size = thumb_size


static func abandon(state: Dictionary) -> void:
	if state.is_empty():
		return
	var dot_fill: ColorRect = state.get("dot_fill")
	if is_instance_valid(dot_fill):
		dot_fill.material = null
	state.clear()
