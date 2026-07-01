class_name HookTimeDisplay
extends RefCounted

const HookCalendarLabelsGd = preload("res://addons/mnemonic/clips/hook_calendar_labels.gd")


static func format_clock_12h(hour: int, minute: int) -> String:
	var period := "AM" if hour < 12 else "PM"
	var h12 := hour % 12
	if h12 == 0:
		h12 = 12
	return "%d:%02d %s" % [h12, minute, period]


static func format_local_time_of_day(unix: int) -> String:
	var d := Time.get_datetime_dict_from_unix_time(unix)
	return format_clock_12h(int(d.get("hour", 0)), int(d.get("minute", 0)))


static func format_local_datetime(unix: int) -> String:
	var d := Time.get_datetime_dict_from_unix_time(unix)
	var month := int(d.get("month", 1))
	var month_name := HookCalendarLabelsGd.MONTH_ABBR[clampi(month - 1, 0, 11)]
	return "%s %d, %d, %s" % [
		month_name,
		int(d.get("day", 1)),
		int(d.get("year", 1970)),
		format_clock_12h(int(d.get("hour", 0)), int(d.get("minute", 0))),
	]


static func format_local_time_range(open_unix: int, close_unix: int) -> String:
	var open_d := Time.get_datetime_dict_from_unix_time(open_unix)
	var close_d := Time.get_datetime_dict_from_unix_time(close_unix)
	var month := int(open_d.get("month", 1))
	var month_name := HookCalendarLabelsGd.MONTH_ABBR[clampi(month - 1, 0, 11)]
	var date_part := "%s %d, %d" % [
		month_name,
		int(open_d.get("day", 1)),
		int(open_d.get("year", 1970)),
	]
	var start := format_clock_12h(int(open_d.get("hour", 0)), int(open_d.get("minute", 0)))
	var end := format_clock_12h(int(close_d.get("hour", 0)), int(close_d.get("minute", 0)))
	return "%s, %s–%s" % [date_part, start, end]
