@tool
class_name EditorHookDatePicker
extends HBoxContainer

signal date_changed

var _weekday_labels: PackedStringArray = PackedStringArray([
	"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa",
])
var _month_names: PackedStringArray = PackedStringArray([
	"January",
	"February",
	"March",
	"April",
	"May",
	"June",
	"July",
	"August",
	"September",
	"October",
	"November",
	"December",
])

var _caption: String = ""
var _placeholder: String = "Pick date…"
var _dock_theme = null
var _popup_root: MarginContainer
var _popup_vbox: VBoxContainer

var _caption_label: Label
var _open_btn: Button
var _popup: PopupPanel
var _month_label: Label
var _day_grid: GridContainer
var _btn_prev: Button
var _btn_next: Button
var _btn_clear: Button

var _view_year: int = 1970
var _view_month: int = 1
var _selected_year: int = -1
var _selected_month: int = -1
var _selected_day: int = -1


func _init() -> void:
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_build_ui()


func configure(caption: String, placeholder: String = "Pick date…", body_font_size: int = -1) -> void:
	_caption = caption
	_placeholder = placeholder
	if body_font_size > 0 and _dock_theme == null:
		_apply_font_size(body_font_size)
	if is_instance_valid(_caption_label):
		var show_caption := not caption.is_empty()
		_caption_label.visible = show_caption
		_caption_label.text = caption
		if show_caption:
			_caption_label.custom_minimum_size.x = 36
		else:
			_caption_label.custom_minimum_size.x = 0
		_apply_font(_caption_label)
	_update_open_button_text()
	if is_instance_valid(_open_btn):
		_apply_font(_open_btn)


func apply_dock_theme(dock_theme) -> void:
	_dock_theme = dock_theme
	if _dock_theme == null:
		return
	if is_instance_valid(_popup_root):
		var m: int = _dock_theme.margin_px()
		_popup_root.add_theme_constant_override("margin_left", m)
		_popup_root.add_theme_constant_override("margin_right", m)
		_popup_root.add_theme_constant_override("margin_top", m)
		_popup_root.add_theme_constant_override("margin_bottom", m)
	if is_instance_valid(_btn_prev):
		_dock_theme.apply_flat_icon_button(_btn_prev)
	if is_instance_valid(_btn_next):
		_dock_theme.apply_flat_icon_button(_btn_next)
	if is_instance_valid(_btn_clear):
		_dock_theme.apply_flat_icon_button(_btn_clear)
	if is_instance_valid(_open_btn):
		_open_btn.theme_type_variation = &""
		_dock_theme.apply_full_width_button(_open_btn)
	for node in [_caption_label, _open_btn, _month_label, _btn_prev, _btn_next, _btn_clear]:
		if node is Control:
			_dock_theme.apply_body_font(node)


func has_date() -> bool:
	return _selected_year > 0 and _selected_month > 0 and _selected_day > 0


func get_date_iso() -> String:
	if not has_date():
		return ""
	return "%04d-%02d-%02d" % [_selected_year, _selected_month, _selected_day]


func clear() -> void:
	_selected_year = -1
	_selected_month = -1
	_selected_day = -1
	_update_open_button_text()
	date_changed.emit()


func _build_ui() -> void:
	_caption_label = Label.new()
	_caption_label.custom_minimum_size.x = 36
	_caption_label.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	add_child(_caption_label)

	_open_btn = Button.new()
	_open_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_open_btn.pressed.connect(_on_open_pressed)
	add_child(_open_btn)

	_popup = PopupPanel.new()
	_popup.hide()
	add_child(_popup)

	_popup_root = MarginContainer.new()
	_popup_root.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_popup_root.add_theme_constant_override("margin_left", 6)
	_popup_root.add_theme_constant_override("margin_right", 6)
	_popup_root.add_theme_constant_override("margin_top", 6)
	_popup_root.add_theme_constant_override("margin_bottom", 6)
	_popup.add_child(_popup_root)

	_popup_vbox = VBoxContainer.new()
	_popup_vbox.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_popup_root.add_child(_popup_vbox)

	var nav := HBoxContainer.new()
	_popup_vbox.add_child(nav)
	_btn_prev = Button.new()
	_btn_prev.text = "<"
	_btn_prev.custom_minimum_size = Vector2(28, 0)
	_btn_prev.pressed.connect(_on_prev_month)
	_apply_font(_btn_prev)
	nav.add_child(_btn_prev)
	_month_label = Label.new()
	_month_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_month_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_apply_font(_month_label)
	nav.add_child(_month_label)
	_btn_next = Button.new()
	_btn_next.text = ">"
	_btn_next.custom_minimum_size = Vector2(28, 0)
	_btn_next.pressed.connect(_on_next_month)
	_apply_font(_btn_next)
	nav.add_child(_btn_next)

	_day_grid = GridContainer.new()
	_day_grid.columns = 7
	_day_grid.add_theme_constant_override("h_separation", 2)
	_day_grid.add_theme_constant_override("v_separation", 2)
	_popup_vbox.add_child(_day_grid)

	var footer := HBoxContainer.new()
	_popup_vbox.add_child(footer)
	_btn_clear = Button.new()
	_btn_clear.text = "Clear"
	_btn_clear.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_btn_clear.pressed.connect(_on_clear_pressed)
	_apply_font(_btn_clear)
	footer.add_child(_btn_clear)

	_reset_view_to_today()
	_rebuild_day_grid()
	_update_open_button_text()


func _body_font_size() -> int:
	if _dock_theme != null:
		return _dock_theme.body_font_size()
	return 13


func _apply_font_size(size: int) -> void:
	for node in [_caption_label, _open_btn, _month_label, _btn_prev, _btn_next, _btn_clear]:
		if node is Control:
			node.add_theme_font_size_override("font_size", size)


func _apply_font(control: Control) -> void:
	if control is Label or control is Button:
		control.add_theme_font_size_override("font_size", _body_font_size())


func _on_open_pressed() -> void:
	if has_date():
		_view_year = _selected_year
		_view_month = _selected_month
	else:
		_reset_view_to_today()
	_rebuild_day_grid()
	call_deferred("_show_popup_at_open_button")


func _show_popup_at_open_button() -> void:
	if not is_instance_valid(_popup) or not is_instance_valid(_open_btn):
		return
	_apply_popup_geometry()
	_popup.popup()


func _on_prev_month() -> void:
	_view_month -= 1
	if _view_month < 1:
		_view_month = 12
		_view_year -= 1
	_rebuild_day_grid()
	_refresh_popup_geometry_if_visible()


func _on_next_month() -> void:
	_view_month += 1
	if _view_month > 12:
		_view_month = 1
		_view_year += 1
	_rebuild_day_grid()
	_refresh_popup_geometry_if_visible()


func _refresh_popup_geometry_if_visible() -> void:
	if is_instance_valid(_popup) and _popup.visible:
		call_deferred("_apply_popup_geometry")


func _apply_popup_geometry() -> void:
	if not is_instance_valid(_popup) or not is_instance_valid(_open_btn) or not is_instance_valid(_popup_root):
		return
	_popup.reset_size()
	var content_min := _popup_root.get_combined_minimum_size()
	var popup_w := maxi(1, int(ceilf(content_min.x)))
	var popup_h := maxi(1, int(ceilf(content_min.y)))
	var popup_size := Vector2i(popup_w, popup_h)
	_popup.size = popup_size
	var btn_pos := _open_btn.get_screen_position()
	var btn_size := _open_btn.size
	var pos_x := int(btn_pos.x + (btn_size.x * 0.5) - (popup_size.x * 0.5))
	var pos_y := int(btn_pos.y + btn_size.y)
	var vr := get_viewport().get_visible_rect()
	pos_x = clampi(pos_x, int(vr.position.x), int(vr.position.x + vr.size.x - popup_size.x))
	pos_y = clampi(pos_y, int(vr.position.y), int(vr.position.y + vr.size.y - popup_size.y))
	_popup.position = Vector2i(pos_x, pos_y)


func _clear_day_grid() -> void:
	if not is_instance_valid(_day_grid):
		return
	for child in _day_grid.get_children():
		_day_grid.remove_child(child)
		child.free()


func _on_clear_pressed() -> void:
	clear()
	_popup.hide()


func _on_day_pressed(day: int) -> void:
	_selected_year = _view_year
	_selected_month = _view_month
	_selected_day = day
	_update_open_button_text()
	_popup.hide()
	date_changed.emit()


func _reset_view_to_today() -> void:
	var today := _utc_date_dict_from_unix(int(Time.get_unix_time_from_system()))
	_view_year = int(today.get("year", 1970))
	_view_month = int(today.get("month", 1))


func _update_open_button_text() -> void:
	if not is_instance_valid(_open_btn):
		return
	var iso := get_date_iso()
	_open_btn.text = iso if not iso.is_empty() else _placeholder


func _rebuild_day_grid() -> void:
	if not is_instance_valid(_day_grid):
		return
	_clear_day_grid()

	_month_label.text = "%s %d" % [_month_names[_view_month - 1], _view_year]

	for label in _weekday_labels:
		var hdr := Label.new()
		hdr.text = label
		hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		hdr.custom_minimum_size = Vector2(28, 20)
		_apply_font(hdr)
		_day_grid.add_child(hdr)

	var first_weekday := _weekday_sunday_first(_view_year, _view_month)
	var days_in_month := _days_in_month(_view_year, _view_month)
	var today := _utc_date_dict_from_unix(int(Time.get_unix_time_from_system()))
	var today_y := int(today.get("year", 0))
	var today_m := int(today.get("month", 0))
	var today_d := int(today.get("day", 0))

	for _i in range(first_weekday):
		var spacer := Control.new()
		spacer.custom_minimum_size = Vector2(28, 24)
		_day_grid.add_child(spacer)

	for day in range(1, days_in_month + 1):
		var btn := Button.new()
		btn.text = str(day)
		btn.custom_minimum_size = Vector2(28, 24)
		_apply_font(btn)
		if _dock_theme != null:
			_dock_theme.apply_flat_icon_button(btn)
		var is_today := _view_year == today_y and _view_month == today_m and day == today_d
		if is_today:
			btn.tooltip_text = "Today"
			if _dock_theme != null:
				btn.add_theme_stylebox_override("normal", _dock_theme.make_today_day_style())
				btn.add_theme_color_override("font_color", _dock_theme.color_editor(&"accent_color"))
		var picked_day := day
		btn.pressed.connect(func() -> void: _on_day_pressed(picked_day))
		_day_grid.add_child(btn)


static func _utc_date_dict_from_unix(unix_time: int) -> Dictionary:
	return Time.get_datetime_dict_from_unix_time(unix_time)


static func _weekday_sunday_first(year: int, month: int) -> int:
	var dict := {
		"year": year,
		"month": month,
		"day": 1,
		"hour": 0,
		"minute": 0,
		"second": 0,
	}
	var unix := int(Time.get_unix_time_from_datetime_dict(dict))
	var wd := int(_utc_date_dict_from_unix(unix).get("weekday", 1))
	# Godot: 1 = Monday … 7 = Sunday → Sunday-first column 0…6
	return 0 if wd == 7 else wd


static func _days_in_month(year: int, month: int) -> int:
	if month < 1 or month > 12:
		return 0
	var next_month := month + 1
	var next_year := year
	if next_month > 12:
		next_month = 1
		next_year += 1
	var start := int(
		Time.get_unix_time_from_datetime_dict(
			{"year": year, "month": month, "day": 1, "hour": 0, "minute": 0, "second": 0}
		)
	)
	var end := int(
		Time.get_unix_time_from_datetime_dict(
			{
				"year": next_year,
				"month": next_month,
				"day": 1,
				"hour": 0,
				"minute": 0,
				"second": 0,
			}
		)
	)
	return maxi(28, int((end - start) / 86400))


static func format_iso(year: int, month: int, day: int) -> String:
	return "%04d-%02d-%02d" % [year, month, day]


static func expected_day_grid_child_count(year: int, month: int) -> int:
	return 7 + _weekday_sunday_first(year, month) + _days_in_month(year, month)
