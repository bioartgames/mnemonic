class_name HookDockEmptyState
extends RefCounted


static func build_centered_message(
	theme: HookDockTheme,
	message: String,
	icon_name: StringName = &"",
	use_placeholder_label: bool = false,
) -> VBoxContainer:
	var col := VBoxContainer.new()
	col.alignment = BoxContainer.ALIGNMENT_CENTER
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	col.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		col.add_theme_constant_override("separation", theme.row_separation_px())

	if not icon_name.is_empty() and theme != null:
		var icon_tex: Texture2D = theme.icon_or_null(icon_name)
		if icon_tex != null:
			var icon_rect := TextureRect.new()
			icon_rect.texture = icon_tex
			icon_rect.custom_minimum_size = Vector2(32, 32)
			icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			icon_rect.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
			col.add_child(icon_rect)

	var lab := Label.new()
	lab.text = message
	lab.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if theme != null:
		if use_placeholder_label:
			theme.apply_placeholder_label(lab)
		else:
			theme.apply_message_label(lab)
	col.add_child(lab)
	return col
