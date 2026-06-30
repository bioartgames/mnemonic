@tool
class_name HookDockTheme
extends RefCounted

var _editor_interface: EditorInterface = null


func setup(editor_interface: EditorInterface) -> void:
	_editor_interface = editor_interface


func get_scale() -> float:
	if _editor_interface == null:
		return 1.0
	return _editor_interface.get_editor_scale()


func _base_control() -> Control:
	if _editor_interface == null:
		return null
	return _editor_interface.get_base_control()


func body_font_size() -> int:
	var base := _base_control()
	if base == null:
		return 13
	var size := base.get_theme_font_size("font_size", "Label")
	return size if size > 0 else 13


func heading_font_size() -> int:
	return body_font_size() + 2


func margin_px() -> int:
	return maxi(4, int(4.0 * get_scale()))


func row_separation_px() -> int:
	return maxi(4, int(4.0 * get_scale()))


func section_separation_px() -> int:
	return maxi(6, int(8.0 * get_scale()))


func action_separation_px() -> int:
	return maxi(2, int(2.0 * get_scale()))


func color_editor(color_name: StringName) -> Color:
	var base := _base_control()
	if base == null:
		return Color.WHITE
	return base.get_theme_color(color_name, "Editor")


func icon(icon_name: StringName) -> Texture2D:
	var base := _base_control()
	if base == null:
		return null
	return base.get_theme_icon(icon_name, "EditorIcons")


func icon_or_null(icon_name: StringName) -> Texture2D:
	var base := _base_control()
	if base == null:
		return null
	if not base.has_theme_icon(icon_name, &"EditorIcons"):
		return null
	return base.get_theme_icon(icon_name, &"EditorIcons")


func apply_body_font(control: Control) -> void:
	if (
		control is Label
		or control is Button
		or control is CheckBox
		or control is LineEdit
		or control is MenuButton
	):
		control.add_theme_font_size_override("font_size", body_font_size())


func apply_section_label(label: Label) -> void:
	var base := _base_control()
	label.add_theme_font_size_override("font_size", heading_font_size())
	if base == null:
		return
	var title_font := base.get_theme_font("title", "EditorFonts")
	if title_font != null:
		label.add_theme_font_override("font", title_font)
	var tint := base.get_theme_color("property_color_z", "Editor")
	if tint.a > 0.0:
		label.add_theme_color_override("font_color", tint)


func apply_popup_section_label(label: Label) -> void:
	apply_body_font(label)
	label.add_theme_font_size_override("font_size", heading_font_size())
	var base := _base_control()
	if base == null:
		return
	var title_font := base.get_theme_font("title", "EditorFonts")
	if title_font != null:
		label.add_theme_font_override("font", title_font)
	label.add_theme_color_override("font_color", color_editor("font_color"))


func apply_popup_subsection_label(label: Label) -> void:
	apply_body_font(label)
	label.add_theme_color_override("font_color", color_editor("readonly_color"))


func apply_popup_column_header(label: Label) -> void:
	apply_body_font(label)
	label.add_theme_color_override("font_color", color_editor("readonly_color"))


func apply_settings_signal_label(label: Label) -> void:
	apply_body_font(label)
	label.clip_text = false
	label.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
	label.autowrap_mode = TextServer.AUTOWRAP_OFF
	label.custom_minimum_size.x = 0
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.size_flags_stretch_ratio = 1.0


func spin_box_min_width_px() -> int:
	return maxi(64, int(64.0 * get_scale()))


func heuristic_checkbox_column_width_px() -> int:
	return maxi(28, int(28.0 * get_scale()))


func apply_editor_spin_box(spin: SpinBox) -> void:
	spin.size_flags_horizontal = Control.SIZE_SHRINK_END
	spin.custom_minimum_size.x = spin_box_min_width_px()
	apply_body_font(spin)


func apply_editor_checkbox(check: CheckBox) -> void:
	apply_body_font(check)


func apply_editor_theme_to_popup(root: Window) -> void:
	if _editor_interface == null:
		return
	var ed_theme := _editor_interface.get_editor_theme()
	if ed_theme != null:
		root.theme = ed_theme


func apply_status_tint(label: Label, state_key: String) -> void:
	label.remove_theme_color_override("font_color")
	var tint: Color
	match state_key:
		"recording":
			tint = color_editor("accent_color")
		"paused":
			tint = color_editor("warning_color")
		"error":
			tint = color_editor("error_color")
		"idle", "unavailable":
			tint = color_editor("readonly_color")
		_:
			return
	label.add_theme_color_override("font_color", tint)


func filter_control_height_px() -> int:
	return maxi(28, int(28.0 * get_scale()))


func clips_scrollbar_width_px() -> int:
	return maxi(10, int(12.0 * get_scale()))


func clips_scroll_edge_inset_px() -> int:
	return maxi(2, int(4.0 * get_scale()))


func clips_scroll_content_inset_px() -> int:
	return maxi(2, int(4.0 * get_scale()))


func apply_clips_scroll(
	scroll: ScrollContainer,
	content_margin: MarginContainer,
	edge_margin: MarginContainer,
) -> void:
	var vscroll := scroll.get_v_scroll_bar()
	if vscroll != null:
		vscroll.custom_minimum_size.x = clips_scrollbar_width_px()
	content_margin.add_theme_constant_override(
		"margin_right",
		clips_scroll_content_inset_px(),
	)
	edge_margin.add_theme_constant_override("margin_right", clips_scroll_edge_inset_px())


func apply_filter_option_button(option: OptionButton) -> void:
	apply_body_font(option)
	option.alignment = HORIZONTAL_ALIGNMENT_CENTER
	option.custom_minimum_size.y = filter_control_height_px()
	option.size_flags_horizontal = Control.SIZE_EXPAND_FILL


func apply_full_width_button(btn: Button) -> void:
	apply_body_font(btn)
	btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL


func apply_disclosure_button(btn: Button) -> void:
	apply_full_width_button(btn)


func apply_toolbar_disclosure_button(btn: Button) -> void:
	apply_body_font(btn)
	btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
	btn.theme_type_variation = &""


func apply_shrink_width_label(label: Label, wrap: bool = false) -> void:
	apply_body_font(label)
	label.clip_text = true
	label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	label.autowrap_mode = (
		TextServer.AUTOWRAP_WORD_SMART if wrap else TextServer.AUTOWRAP_OFF
	)
	label.custom_minimum_size.x = 0
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL


func apply_status_label(label: Label) -> void:
	apply_shrink_width_label(label, true)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT


func apply_toast_label(label: Label) -> void:
	apply_shrink_width_label(label, true)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_color_override("font_color", color_editor("readonly_color"))
	var base := _base_control()
	if base == null:
		return
	var italic := base.get_theme_font("doc_italic", "EditorFonts")
	if italic != null:
		label.add_theme_font_override("font", italic)


func apply_message_label(label: Label) -> void:
	apply_shrink_width_label(label, true)
	label.add_theme_color_override("font_color", color_editor("readonly_color"))


## Centered placeholder copy (segment log empty state). Avoid clip_text so layout height stays visible.
func apply_placeholder_label(label: Label) -> void:
	apply_body_font(label)
	label.clip_text = false
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size = Vector2(0, maxi(32, int(32.0 * get_scale())))
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	label.add_theme_color_override("font_color", color_editor("readonly_color"))


## Segment log history lines (must not use clip_text or rows paint with zero height).
func apply_segment_log_row_label(label: Label) -> void:
	apply_body_font(label)
	label.clip_text = false
	label.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size.x = 0
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	label.add_theme_color_override("font_color", color_editor("font_color"))


func apply_segment_log_row_rich_text(rtl: RichTextLabel) -> void:
	apply_body_font(rtl)
	rtl.bbcode_enabled = false
	rtl.fit_content = true
	rtl.scroll_active = false
	rtl.selection_enabled = true
	rtl.context_menu_enabled = true
	rtl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	rtl.custom_minimum_size = Vector2(0, maxi(32, int(32.0 * get_scale())))
	rtl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	rtl.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	rtl.add_theme_color_override("default_color", color_editor("font_color"))


func apply_flat_icon_button(btn: Button) -> void:
	btn.theme_type_variation = &"FlatButton"


func make_clips_drawer_panel() -> StyleBoxFlat:
	var panel := StyleBoxFlat.new()
	panel.bg_color = color_editor("dark_color_3")
	var radius := maxi(2, int(3.0 * get_scale()))
	panel.corner_radius_top_left = radius
	panel.corner_radius_top_right = radius
	panel.corner_radius_bottom_left = radius
	panel.corner_radius_bottom_right = radius
	var pad := maxi(4, int(4.0 * get_scale()))
	panel.content_margin_left = pad
	panel.content_margin_right = pad
	panel.content_margin_top = pad
	panel.content_margin_bottom = pad
	return panel


func make_subtle_panel() -> StyleBoxFlat:
	var panel := StyleBoxFlat.new()
	var bg := color_editor("dark_color_3")
	bg.a = 0.6
	panel.bg_color = bg
	var radius := maxi(2, int(3.0 * get_scale()))
	panel.corner_radius_top_left = radius
	panel.corner_radius_top_right = radius
	panel.corner_radius_bottom_left = radius
	panel.corner_radius_bottom_right = radius
	var pad := maxi(4, int(4.0 * get_scale()))
	panel.content_margin_left = pad
	panel.content_margin_right = pad
	panel.content_margin_top = pad
	panel.content_margin_bottom = pad
	return panel


func make_today_day_style() -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.draw_center = false
	style.border_width_bottom = 2
	style.border_color = color_editor("accent_color")
	return style


func make_filter_panel() -> StyleBoxFlat:
	return make_subtle_panel()


func make_live_row_panel() -> StyleBoxFlat:
	var panel := StyleBoxFlat.new()
	var bg := color_editor("dark_color_3")
	bg.a = 0.5
	panel.bg_color = bg
	panel.border_width_left = 0
	panel.border_width_top = 0
	panel.border_width_right = 0
	panel.border_width_bottom = 0
	var radius := maxi(2, int(3.0 * get_scale()))
	panel.corner_radius_top_left = radius
	panel.corner_radius_top_right = radius
	panel.corner_radius_bottom_left = radius
	panel.corner_radius_bottom_right = radius
	var pad := maxi(4, int(4.0 * get_scale()))
	panel.content_margin_left = pad
	panel.content_margin_right = pad
	panel.content_margin_top = 0
	panel.content_margin_bottom = 0
	return panel


func apply_filter_button_active(btn: Button, active: bool) -> void:
	if btn == null:
		return
	if active:
		btn.add_theme_color_override("font_color", color_editor("accent_color"))
		btn.add_theme_color_override("font_hover_color", color_editor("accent_color"))
		btn.add_theme_color_override("font_pressed_color", color_editor("accent_color"))
	else:
		btn.remove_theme_color_override("font_color")
		btn.remove_theme_color_override("font_hover_color")
		btn.remove_theme_color_override("font_pressed_color")
